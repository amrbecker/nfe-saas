using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.Application.Assistente.Ia;

public record PedidoRespostaOri(
    EscopoOri Escopo,
    TipoInteracaoAssistente Tipo,
    string Pergunta,
    ContextoTelaDto? Contexto,
    Guid? NotaId = null,
    Conversa? Conversa = null);

/// <summary>
/// Pipeline de uma resposta da Ori: sanitiza → roteia artigos → (sem IA/sem cota: responde só com a base) →
/// modelo com ferramentas → verificador (1 nova tentativa) → grava sanitizado → devolve reidratado.
/// </summary>
public class ServicoRespostaOri
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IAssistenteIA _ia;
    private readonly IBaseConhecimento _base;
    private readonly ISanitizadorIA _sanitizador;
    private readonly ICotaAssistente _cota;
    private readonly MontadorPrompt _prompt;
    private readonly MontadorContexto _contexto;
    private readonly FerramentasOri _ferramentas;
    private readonly IInteracaoAssistenteRepository _interacoes;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;
    private readonly IOptions<AssistenteOptions> _opcoes;
    private readonly ILogger<ServicoRespostaOri> _logger;

    public ServicoRespostaOri(IAssistenteIA ia, IBaseConhecimento baseConhecimento, ISanitizadorIA sanitizador,
        ICotaAssistente cota, MontadorPrompt prompt, MontadorContexto contexto, FerramentasOri ferramentas,
        IInteracaoAssistenteRepository interacoes, IUnitOfWork uow, IMediator mediator,
        IOptions<AssistenteOptions> opcoes, ILogger<ServicoRespostaOri> logger)
    {
        _ia = ia;
        _base = baseConhecimento;
        _sanitizador = sanitizador;
        _cota = cota;
        _prompt = prompt;
        _contexto = contexto;
        _ferramentas = ferramentas;
        _interacoes = interacoes;
        _uow = uow;
        _mediator = mediator;
        _opcoes = opcoes;
        _logger = logger;
    }

    public bool Habilitado(Guid escritorioId) => _opcoes.Value.HabilitadoPara(escritorioId);

    public async Task<RespostaOriDto> ResponderAsync(PedidoRespostaOri p, IProgress<EventoConversaDto>? progresso, CancellationToken ct)
    {
        var mapa = new Dictionary<string, string>();
        var pergunta = _sanitizador.Sanitizar(p.Pergunta, mapa);
        var nota = await _contexto.ObterNotaAsync(p.Escopo, p.NotaId ?? p.Contexto?.NotaId, ct);

        var codigo = _base.ExtrairCodigoRejeicao(nota?.MotivoRejeicao);
        var artigos = RotearArtigos(p, codigo, pergunta.Texto);

        var cota = await _cota.ObterAsync(p.Escopo.UsuarioId, p.Escopo.EscritorioId, ct);
        if (!_ia.EstaHabilitado(RotaIa.Conversa) || !cota.Permitido)
            return await RespostaDaBaseAsync(p, artigos, pergunta, mapa, cota, ct);

        var contexto = await _contexto.MontarAsync(p.Escopo, p.Contexto, nota, mapa, ct);
        var historico = p.Conversa?.Mensagens
            .Where(m => m.Papel != PapelMensagem.Ferramenta)
            .OrderBy(m => m.CriadaEm)
            .Select(m => new MensagemIa(m.Papel == PapelMensagem.Usuario ? PapelIa.Usuario : PapelIa.Assistente, m.ConteudoSanitizado))
            .ToList() ?? new List<MensagemIa>();
        var mensagens = _prompt.Montar(artigos, contexto, historico, p.Conversa?.ResumoAnterior, pergunta.Texto).ToList();

        var acoes = new List<AcaoUiDto>();
        var ferramentas = _ferramentas.Criar(p.Escopo, mapa, acoes);
        var progressoFerramentas = progresso == null ? null : new ProgressoSincrono<ChamadaFerramentaIa>(c =>
            progresso.Report(new EventoConversaDto("ferramenta", DescreverFerramenta(c.Nome), null)));

        progresso?.Report(new EventoConversaDto("status", "Pensando…", null));
        RespostaIa resposta;
        try
        {
            resposta = await _ia.CompletarAsync(RotaIa.Conversa, mensagens,
                new OpcoesIa(MaxTokens: p.Tipo == TipoInteracaoAssistente.ExplicarRejeicao ? 900 : 1200, Raciocinio: true,
                    Ferramentas: ferramentas, MaxIteracoesFerramentas: 4, Progresso: progressoFerramentas), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falha ao consultar o modelo da Ori; respondendo só com a base.");
            return await RespostaDaBaseAsync(p, artigos, pergunta, mapa, cota, ct);
        }

        var permitidos = new List<string> { pergunta.Texto, JsonSerializer.Serialize(contexto, Json), MontadorPrompt.BlocoArtigos(artigos) };
        permitidos.AddRange(resposta.Ferramentas.Select(f => f.Resultado));

        var uso = resposta.Uso;
        var texto = resposta.Texto;
        var verificacao = VerificadorResposta.Verificar(texto, _base, permitidos);
        if (!verificacao.Aprovada)
        {
            progresso?.Report(new EventoConversaDto("status", "Conferindo as fontes…", null));
            try
            {
                var correcao = new List<MensagemIa>(mensagens)
                {
                    new(PapelIa.Assistente, texto),
                    new(PapelIa.Usuario, VerificadorResposta.InstrucaoCorrecao(verificacao))
                };
                var segunda = await _ia.CompletarAsync(RotaIa.Conversa, correcao, new OpcoesIa(1200, true), ct);
                uso = uso.Somar(segunda.Uso);
                texto = segunda.Texto;
                verificacao = VerificadorResposta.Verificar(texto, _base, permitidos);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Falha na segunda tentativa da Ori.");
            }
        }

        var falhou = !verificacao.Aprovada;
        if (falhou)
        {
            texto = VerificadorResposta.RespostaSemFonte;
            await RegistrarLacunaAsync(p, pergunta.Texto, ct);
        }
        else if (verificacao.ArtigosCitados.Count == 0 && artigos.Count == 0)
        {
            await RegistrarLacunaAsync(p, pergunta.Texto, ct);
        }

        var custo = _ia.CustoEstimadoUsd(RotaIa.Conversa, uso);
        var selo = falhou ? NivelFonte.N4SemFonte : verificacao.Selo;
        var interacao = InteracaoAssistente.Criar(p.Escopo.EscritorioId, p.Escopo.EmpresaId, p.Escopo.UsuarioId, p.Tipo,
            pergunta.Texto, texto, verificacao.ArtigosCitados.Select(a => a.Id), selo, resposta.Modelo,
            uso.TokensEntrada, uso.TokensCache, uso.TokensSaida, custo, falhou, p.Conversa?.Id, nota?.Id);
        await _interacoes.AddAsync(interacao, ct);

        if (p.Conversa != null)
        {
            p.Conversa.AdicionarMensagem(PapelMensagem.Usuario, pergunta.Texto);
            p.Conversa.AdicionarMensagem(PapelMensagem.Assistente, texto,
                resposta.Ferramentas.Count == 0 ? null : JsonSerializer.Serialize(resposta.Ferramentas.Select(f => new { f.Nome, f.ArgumentosJson }), Json));
            AtualizarResumo(p.Conversa);
        }
        await _uow.SaveChangesAsync(ct);
        await _cota.RegistrarAsync(p.Escopo.UsuarioId, p.Escopo.EscritorioId, uso, custo, ct);
        var cotaDepois = await _cota.ObterAsync(p.Escopo.UsuarioId, p.Escopo.EscritorioId, ct);

        return new RespostaOriDto(
            interacao.Id,
            _sanitizador.Reidratar(texto, mapa),
            selo,
            falhou ? new List<CitacaoDto>() : verificacao.ArtigosCitados.Select(Citacao).ToList(),
            falhou,
            RespondidaSemModelo: false,
            pergunta.Quantidade,
            acoes.Select(a => Reidratar(a, mapa)).ToList(),
            cotaDepois);
    }

    /// <summary>
    /// Explicar rejeição: código de rejeição manda. Pergunta livre: o texto da pergunta vem antes do campo em foco
    /// (quem está no CFOP pode perguntar sobre cancelamento); campo e tela completam até 3 artigos.
    /// </summary>
    private IReadOnlyList<ArtigoKb> RotearArtigos(PedidoRespostaOri p, string? codigo, string perguntaSanitizada)
    {
        var codigos = codigo == null ? null : new[] { codigo };
        if (p.Tipo == TipoInteracaoAssistente.ExplicarRejeicao)
            return _base.Rotear(new CriterioRoteamentoKb(codigos, p.Contexto?.Foco?.Campo, p.Contexto?.Tela, perguntaSanitizada), 3);

        var lista = _base.Rotear(new CriterioRoteamentoKb(codigos, Termo: perguntaSanitizada), 3).ToList();
        foreach (var a in _base.Rotear(new CriterioRoteamentoKb(null, p.Contexto?.Foco?.Campo, p.Contexto?.Tela), 3))
            if (lista.Count < 3 && !lista.Contains(a)) lista.Add(a);
        return lista;
    }

    /// <summary>Resposta sem modelo (IA desligada, cota esgotada ou falha): resumo do artigo mais relevante.</summary>
    private async Task<RespostaOriDto> RespostaDaBaseAsync(PedidoRespostaOri p, IReadOnlyList<ArtigoKb> artigos,
        ResultadoSanitizacao pergunta, Dictionary<string, string> mapa, StatusCotaDto cota, CancellationToken ct)
    {
        if (artigos.Count == 0)
        {
            await RegistrarLacunaAsync(p, pergunta.Texto, ct);
            return new RespostaOriDto(null, VerificadorResposta.RespostaSemFonte, NivelFonte.N4SemFonte, new(), false, true,
                pergunta.Quantidade, new(), cota);
        }

        var principal = artigos[0];
        var texto = $"**{principal.Titulo}**\n\n{principal.ResumoCurto}\n\n[fonte: {principal.Id}]";
        if (artigos.Count > 1)
            texto += "\n\nVeja também: " + string.Join(", ", artigos.Skip(1).Select(a => a.Titulo)) + ".";
        return new RespostaOriDto(null, texto, VerificadorResposta.CalcularSelo(new[] { principal }),
            artigos.Select(Citacao).ToList(), false, true, pergunta.Quantidade, new(), cota);
    }

    private async Task RegistrarLacunaAsync(PedidoRespostaOri p, string perguntaSanitizada, CancellationToken ct)
    {
        try
        {
            var tema = p.Contexto?.Foco?.Campo ?? p.Contexto?.Tela;
            var resumo = perguntaSanitizada.Length > 500 ? perguntaSanitizada[..500] : perguntaSanitizada;
            await _mediator.Send(new RegistrarSinalCommand(p.Escopo.EscritorioId, p.Escopo.EmpresaId, p.Escopo.UsuarioId,
                TipoSinalProduto.LacunaConhecimento, resumo, p.Contexto?.Tela, tema), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Não foi possível registrar a lacuna de conhecimento.");
        }
    }

    /// <summary>Resumo determinístico dos turnos fora da janela (E6) — sem gastar uma chamada ao modelo.</summary>
    private static void AtualizarResumo(Conversa conversa)
    {
        var perguntas = conversa.Mensagens.Where(m => m.Papel == PapelMensagem.Usuario).OrderBy(m => m.CriadaEm).ToList();
        var fora = perguntas.Count - MontadorPrompt.JanelaTurnos;
        if (fora <= 0) return;
        var resumo = "Perguntas anteriores do usuário: " + string.Join(" | ",
            perguntas.Take(fora).Select(m => m.ConteudoSanitizado.Length > 160 ? m.ConteudoSanitizado[..160] + "…" : m.ConteudoSanitizado));
        conversa.AtualizarResumo(resumo.Length > 3900 ? resumo[..3900] : resumo);
    }

    private static CitacaoDto Citacao(ArtigoKb a) => new(a.Id, a.Titulo, a.NivelFonte, a.VerificadoEm, a.Status);

    private AcaoUiDto Reidratar(AcaoUiDto a, IReadOnlyDictionary<string, string> mapa) =>
        a with
        {
            Resumo = _sanitizador.Reidratar(a.Resumo, mapa),
            Parametros = a.Parametros.ToDictionary(kv => kv.Key, kv => _sanitizador.Reidratar(kv.Value, mapa))
        };

    public static string DescreverFerramenta(string nome) => nome switch
    {
        "consultar_nota" => "Consultando a nota…",
        "listar_rejeicoes_recentes" => "Olhando as rejeições recentes…",
        "consultar_empresa" => "Conferindo os dados da empresa…",
        "consultar_certificado" => "Verificando o certificado…",
        "consultar_status_sefaz" => "Consultando a SEFAZ…",
        "buscar_ncm" => "Buscando na tabela NCM…",
        "consultar_assinatura" => "Conferindo o plano…",
        "consultar_padroes" => "Olhando suas notas anteriores…",
        "buscar_artigo" => "Procurando na base de conhecimento…",
        "preparar_nota" or "preparar_cadastro" => "Preparando o formulário…",
        _ => "Trabalhando nisso…"
    };

    /// <summary>IProgress que chama o callback na hora (Progress&lt;T&gt; posta em outra thread e perde a ordem).</summary>
    private sealed class ProgressoSincrono<T> : IProgress<T>
    {
        private readonly Action<T> _acao;
        public ProgressoSincrono(Action<T> acao) => _acao = acao;
        public void Report(T value) => _acao(value);
    }
}
