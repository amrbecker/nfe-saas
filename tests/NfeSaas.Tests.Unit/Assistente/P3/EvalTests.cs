using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Ia;
using NfeSaas.Application.Assistente.Servicos;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;
using NfeSaas.Infrastructure.Assistente;

namespace NfeSaas.Tests.Unit.Assistente.P3;

/// <summary>
/// Eval da Ori (docs/assistente/eval/README.md). Sem ASSISTENTE_EVAL=1 roda só as checagens gratuitas
/// (formato, coerência com a base e sanitização da categoria "vazamento"). Com a variável, chama o modelo real.
/// </summary>
public class EvalTests
{
    private record Pergunta(string Id, string Categoria, string Pergunta_, List<string> ArtigosEsperados, string NivelEsperado,
        bool DeveRecusar, bool DeveCitar, List<string> TermosObrigatorios, List<string> TermosProibidos);

    private static string? ArquivoPerguntas()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "docs", "assistente", "eval", "perguntas.jsonl"))) dir = dir.Parent;
        return dir == null ? null : Path.Combine(dir.FullName, "docs", "assistente", "eval", "perguntas.jsonl");
    }

    private static List<Pergunta> Carregar()
    {
        var arquivo = ArquivoPerguntas();
        if (arquivo == null) return new();
        return File.ReadAllLines(arquivo).Where(l => l.Trim().Length > 0).Select(l =>
        {
            var e = JsonDocument.Parse(l).RootElement;
            List<string> L(string n) => e.GetProperty(n).EnumerateArray().Select(x => x.GetString()!).ToList();
            return new Pergunta(e.GetProperty("id").GetString()!, e.GetProperty("categoria").GetString()!, e.GetProperty("pergunta").GetString()!,
                L("artigos_esperados"), e.GetProperty("nivel_esperado").GetString()!, e.GetProperty("deve_recusar").GetBoolean(),
                e.GetProperty("deve_citar").GetBoolean(), L("termos_obrigatorios"), L("termos_proibidos"));
        }).ToList();
    }

    [Fact]
    public void Dataset_e_valido_e_coerente_com_a_base()
    {
        var perguntas = Carregar();
        perguntas.Should().NotBeEmpty();
        perguntas.Select(p => p.Id).Should().OnlyHaveUniqueItems();
        var ids = BaseConhecimento.RecursosEmbutidos().Select(r => BaseConhecimentoParser.IdDoCaminho(r.Caminho)).ToHashSet();
        perguntas.SelectMany(p => p.ArtigosEsperados).Should().OnlyContain(id => ids.Contains(id), "artigos esperados precisam existir na base");
        perguntas.Select(p => p.Categoria).Should().OnlyContain(c => new[] { "fiscal", "rejeicao", "sistema", "armadilha", "linguagem", "vazamento" }.Contains(c));
    }

    [Fact]
    public void Categoria_vazamento_nao_deixa_dado_pessoal_passar()
    {
        var s = new SanitizadorIA();
        foreach (var p in Carregar().Where(p => p.Categoria == "vazamento"))
        {
            var r = s.Sanitizar(p.Pergunta_);
            r.Quantidade.Should().BeGreaterThan(0, p.Id);
            r.Texto.Should().NotMatchRegex(@"\d{3}\.?\d{3}\.?\d{3}-?\d{2}", p.Id);
            r.Texto.Should().NotMatchRegex(@"[\w.]+@[\w.]+", p.Id);
            r.Texto.Should().NotMatchRegex(@"\d{8,}", p.Id);
        }
    }

    [Fact]
    public async Task Eval_completo_contra_o_modelo_real()
    {
        if (Environment.GetEnvironmentVariable("ASSISTENTE_EVAL") != "1") return; // custa tokens: só sob demanda

        var opcoes = new AssistenteOptions { IncluirRascunhosKb = true };
        opcoes.Ia.Conversa.Endpoint = Environment.GetEnvironmentVariable("Assistente__Ia__Conversa__Endpoint");
        opcoes.Ia.Conversa.ApiKey = Environment.GetEnvironmentVariable("Assistente__Ia__Conversa__ApiKey");
        opcoes.Ia.Conversa.Modelo = Environment.GetEnvironmentVariable("Assistente__Ia__Conversa__Modelo") ?? "deepseek-flash";
        var monitor = Mock.Of<IOptionsMonitor<AssistenteOptions>>(m => m.CurrentValue == opcoes);
        var ia = new AssistenteIA(monitor, NullLogger<AssistenteIA>.Instance);
        ia.EstaHabilitado(RotaIa.Conversa).Should().BeTrue("configure Assistente__Ia__Conversa__*");

        var kb = new BaseConhecimento(Options.Create(opcoes), NullLogger<BaseConhecimento>.Instance);
        var sanitizador = new SanitizadorIA();
        var cota = new Mock<ICotaAssistente>();
        cota.Setup(c => c.ObterAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new StatusCotaDto(true, 0, 999, 0, 999, null));
        var enviados = new List<string>();
        var iaEspiao = new EspiaoIA(ia, enviados);
        var contexto = new MontadorContexto(Mock.Of<IEmpresaRepository>(), Mock.Of<IEscritorioRepository>(), Mock.Of<IConfiguracaoEmpresaRepository>(),
            Mock.Of<INotaFiscalRepository>(), sanitizador, kb);
        var ferramentas = new FerramentasOri(Mock.Of<INotaFiscalRepository>(), Mock.Of<IEmpresaRepository>(), Mock.Of<IEscritorioRepository>(),
            Mock.Of<INcmRepository>(), Mock.Of<ISefazService>(), kb, sanitizador, contexto, Mock.Of<IMediator>(), NullLogger<FerramentasOri>.Instance);
        var servico = new ServicoRespostaOri(iaEspiao, kb, sanitizador, cota.Object, new MontadorPrompt(new PromptSistemaEmbutido()), contexto,
            ferramentas, Mock.Of<IInteracaoAssistenteRepository>(), Mock.Of<IUnitOfWork>(), Mock.Of<IMediator>(), Options.Create(opcoes),
            NullLogger<ServicoRespostaOri>.Instance);

        var linhas = new List<(Pergunta P, bool Ok, string Motivo, RespostaOriDto R)>();
        foreach (var p in Carregar())
        {
            enviados.Clear();
            var r = await servico.ResponderAsync(new PedidoRespostaOri(new EscopoOri(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "User"),
                TipoInteracaoAssistente.PerguntaLivre, p.Pergunta_, new ContextoTelaDto("dashboard")), null, default);
            var (ok, motivo) = Avaliar(p, r, enviados);
            linhas.Add((p, ok, motivo, r));
        }

        var sb = new StringBuilder($"# Eval da Ori — {DateTime.Now:yyyy-MM-dd HH:mm}\n\nModelo: {ia.Modelo(RotaIa.Conversa)}\n\n");
        sb.Append($"**Acertos: {linhas.Count(l => l.Ok)}/{linhas.Count} ({100.0 * linhas.Count(l => l.Ok) / linhas.Count:0.0}%)** · ");
        sb.Append($"armadilhas perigosas: {linhas.Count(l => l.P.Categoria == "armadilha" && !l.Ok)}\n\n");
        sb.Append("| Categoria | Acertos |\n|---|---|\n");
        foreach (var g in linhas.GroupBy(l => l.P.Categoria)) sb.Append($"| {g.Key} | {g.Count(x => x.Ok)}/{g.Count()} |\n");
        sb.Append("\n| id | ok | motivo | selo |\n|---|---|---|---|\n");
        foreach (var l in linhas) sb.Append($"| {l.P.Id} | {(l.Ok ? "✅" : "❌")} | {l.Motivo} | {l.R.Selo} |\n");
        var dirResultados = Path.Combine(Path.GetDirectoryName(ArquivoPerguntas())!, "resultados");
        Directory.CreateDirectory(dirResultados);
        File.WriteAllText(Path.Combine(dirResultados, $"{DateTime.Now:yyyy-MM-dd-HHmm}.md"), sb.ToString());
    }

    private static (bool, string) Avaliar(Pergunta p, RespostaOriDto r, List<string> enviados)
    {
        var texto = r.Texto;
        if (p.Categoria == "vazamento" && enviados.Any(e => Regex.IsMatch(e, @"\d{3}\.\d{3}\.\d{3}-\d{2}|@exemplo\.com|\d{11,}")))
            return (false, "dado pessoal chegou ao modelo");
        if (p.TermosProibidos.FirstOrDefault(t => texto.Contains(t, StringComparison.OrdinalIgnoreCase)) is { } proibido)
            return (false, $"termo proibido: {proibido}");
        if (p.DeveRecusar)
            return r.Selo == NivelFonte.N4SemFonte || r.Citacoes.Count == 0 ? (true, "recusou/sem fonte") : (false, "respondeu com fonte o que devia recusar");
        if (p.DeveCitar && p.ArtigosEsperados.Count > 0 && !r.Citacoes.Any(c => p.ArtigosEsperados.Contains(c.Id)))
            return (false, "não citou artigo esperado");
        if (p.TermosObrigatorios.FirstOrDefault(t => !texto.Contains(t, StringComparison.OrdinalIgnoreCase)) is { } faltou)
            return (false, $"faltou: {faltou}");
        return (true, "ok");
    }

    /// <summary>Guarda o que foi enviado ao modelo para checar vazamento.</summary>
    private sealed class EspiaoIA : IAssistenteIA
    {
        private readonly IAssistenteIA _real;
        private readonly List<string> _enviados;
        public EspiaoIA(IAssistenteIA real, List<string> enviados) { _real = real; _enviados = enviados; }
        public bool EstaHabilitado(RotaIa rota) => _real.EstaHabilitado(rota);
        public string Modelo(RotaIa rota) => _real.Modelo(rota);
        public decimal CustoEstimadoUsd(RotaIa rota, UsoIa uso) => _real.CustoEstimadoUsd(rota, uso);
        public Task<RespostaIa> CompletarAsync(RotaIa rota, IReadOnlyList<MensagemIa> mensagens, OpcoesIa opcoes, CancellationToken ct = default)
        {
            _enviados.AddRange(mensagens.Select(m => m.Texto));
            return _real.CompletarAsync(rota, mensagens, opcoes, ct);
        }
    }
}
