using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.Application.Assistente.Insights;

/// <summary>
/// Relatórios semanais da Ori: insights de produto para o PO e fila de curadoria para o escritório parceiro.
/// Só números agregados e textos já sanitizados — nenhum escritório, empresa, usuário ou dado de cliente.
/// </summary>
public class RelatoriosSemanais
{
    private readonly IInteracaoAssistenteRepository _interacoes;
    private readonly ISinalProdutoRepository _sinais;
    private readonly IChamadoRepository _chamados;
    private readonly IPublicacaoDetectadaRepository _publicacoes;
    private readonly IFonteMonitoradaRepository _fontes;
    private readonly IBaseConhecimento _base;
    private readonly IAssistenteIA _ia;
    private readonly IEmailService _email;
    private readonly IOptions<AssistenteOptions> _opcoes;
    private readonly ILogger<RelatoriosSemanais> _logger;

    public RelatoriosSemanais(IInteracaoAssistenteRepository interacoes, ISinalProdutoRepository sinais, IChamadoRepository chamados,
        IPublicacaoDetectadaRepository publicacoes, IFonteMonitoradaRepository fontes, IBaseConhecimento baseConhecimento,
        IAssistenteIA ia, IEmailService email, IOptions<AssistenteOptions> opcoes, ILogger<RelatoriosSemanais> logger)
    {
        _interacoes = interacoes;
        _sinais = sinais;
        _chamados = chamados;
        _publicacoes = publicacoes;
        _fontes = fontes;
        _base = baseConhecimento;
        _ia = ia;
        _email = email;
        _opcoes = opcoes;
        _logger = logger;
    }

    public async Task<string> MontarInsightsAsync(DateTime agora, CancellationToken ct)
    {
        var desde = agora.AddDays(-7);
        var interacoes = await _interacoes.ListarDesdeAsync(desde, 5000, ct);
        var sinais = await _sinais.ListarAsync(null, desde, 2000, ct);
        var chamados = (await _chamados.ListarAsync(StatusChamado.Aberto, 1, 200, ct)).Where(c => c.CreatedAt >= desde).ToList();

        var avaliadas = interacoes.Where(i => i.Avaliacao != null).ToList();
        var citados = interacoes.SelectMany(i => (i.ArtigosCitados ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(a => a).OrderByDescending(g => g.Count()).Take(10).Select(g => $"{g.Key} ({g.Count()})");

        var sb = new StringBuilder();
        sb.Append("<h2>Ori — insights da semana</h2><ul>");
        sb.Append($"<li>Respostas com o modelo: {interacoes.Count} · custo estimado US$ {interacoes.Sum(i => i.CustoEstimadoUsd):0.0000}</li>");
        sb.Append($"<li>Avaliações: {avaliadas.Count(i => i.Avaliacao > 0)} 👍 / {avaliadas.Count(i => i.Avaliacao < 0)} 👎</li>");
        sb.Append($"<li>Respostas barradas pelo verificador de fontes: {interacoes.Count(i => i.VerificacaoFalhou)}</li>");
        sb.Append($"<li>Chamados abertos na semana: {string.Join(", ", chamados.GroupBy(c => c.Severidade).Select(g => $"{g.Key}: {g.Count()}"))}</li>");
        sb.Append($"<li>Artigos mais citados: {string.Join(", ", citados)}</li></ul>");

        sb.Append("<h3>Sinais de produto</h3>");
        foreach (var g in sinais.GroupBy(s => s.Tipo).OrderByDescending(g => g.Count()))
        {
            sb.Append($"<p><b>{g.Key}</b> ({g.Count()})</p><ul>");
            foreach (var s in g.Take(5)) sb.Append($"<li>{WebUtility.HtmlEncode(s.ResumoSanitizado)}</li>");
            sb.Append("</ul>");
        }

        if (_ia.EstaHabilitado(RotaIa.FontesPublicas) && sinais.Count > 0)
        {
            try
            {
                var texto = string.Join("\n", sinais.Take(150).Select(s => $"[{s.Tipo}] {s.ResumoSanitizado}"));
                var r = await _ia.CompletarAsync(RotaIa.FontesPublicas, new[]
                {
                    new MensagemIa(PapelIa.Sistema, "Você resume sinais de usuários de um emissor de NF-e para o Product Owner. Em até 8 tópicos curtos em português: principais dores, pedidos recorrentes, fricções de uso e oportunidades. Não invente números."),
                    new MensagemIa(PapelIa.Usuario, texto)
                }, new OpcoesIa(800, Raciocinio: false), ct);
                sb.Append("<h3>Resumo executivo (IA)</h3><p>").Append(WebUtility.HtmlEncode(r.Texto).Replace("\n", "<br>")).Append("</p>");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Resumo executivo de insights indisponível.");
            }
        }
        return sb.ToString();
    }

    public async Task<string> MontarCuradoriaAsync(DateTime agora, CancellationToken ct)
    {
        var novas = (await _publicacoes.ListarAsync(StatusPublicacao.Nova, 1, 100, ct)).Where(p => p.Relevante != false).ToList();
        var hoje = DateOnly.FromDateTime(agora);
        var vencidos = _base.Todos().Where(a => a.RevisarAte is { } d && d < hoje).ToList();
        var rascunhos = _base.Todos().Count(a => a.Status == "rascunho");
        var cegas = (await _fontes.ListarAsync(true, ct)).Where(f => f.Cega).ToList();

        var sb = new StringBuilder("<h2>Ori — fila de curadoria</h2>");
        sb.Append($"<p>{novas.Count} publicações novas · {vencidos.Count} artigos com revisão vencida · {rascunhos} rascunhos aguardando revisão.</p><ul>");
        foreach (var p in novas.Take(30))
            sb.Append($"<li>[{WebUtility.HtmlEncode(p.Fonte.Nome)}] {WebUtility.HtmlEncode(p.Titulo)}{(p.ImpactoTecnico ? " — <b>impacto técnico</b>" : "")}</li>");
        sb.Append("</ul>");
        if (vencidos.Count > 0) sb.Append("<p>Revisão vencida: ").Append(string.Join(", ", vencidos.Select(a => a.Id))).Append("</p>");
        if (cegas.Count > 0) sb.Append("<p>Fontes sem leitura há 3+ tentativas: ").Append(string.Join(", ", cegas.Select(f => WebUtility.HtmlEncode(f.Nome)))).Append("</p>");
        return sb.ToString();
    }

    public async Task EnviarAsync(DateTime agora, CancellationToken ct)
    {
        var o = _opcoes.Value;
        if (!string.IsNullOrWhiteSpace(o.EmailPo))
            await _email.EnviarAsync(o.EmailPo, $"Ori — insights da semana ({agora:dd/MM})", await MontarInsightsAsync(agora, ct), ct);
        if (!string.IsNullOrWhiteSpace(o.EmailCurador))
            await _email.EnviarAsync(o.EmailCurador, $"Ori — fila de curadoria ({agora:dd/MM})", await MontarCuradoriaAsync(agora, ct), ct);
    }
}
