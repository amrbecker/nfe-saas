using System.IO.Compression;
using System.Net;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Monitoramento;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.API.Workers.Assistente;

/// <summary>
/// Vigia as fontes oficiais (MONITORAMENTO_FONTES.md): detecta itens novos, cria publicações para a fila de curadoria e,
/// se a rota FontesPublicas estiver configurada, pede à IA uma classificação. Publicar na base é sempre ato humano.
/// Primeira leitura de uma fonte = linha de base (itens registrados como descartados, sem inundar a fila).
/// </summary>
public class MonitorFontesWorker : BackgroundService
{
    private const string UserAgent = "NFeFlow-Monitor/1.0 (+https://nfe.sideral.app.br)";
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopes;
    private readonly IHostEnvironment _env;
    private readonly ILogger<MonitorFontesWorker> _logger;

    public MonitorFontesWorker(IServiceScopeFactory scopes, IHostEnvironment env, ILogger<MonitorFontesWorker> logger)
    {
        _scopes = scopes;
        _env = env;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_env.IsEnvironment("Testing")) return;
        await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                await ExecutarAsync(scope.ServiceProvider, _logger, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha no ciclo do MonitorFontesWorker.");
            }
            await Task.Delay(Intervalo, stoppingToken);
        }
    }

    public static async Task ExecutarAsync(IServiceProvider sp, ILogger logger, CancellationToken ct)
    {
        var fontes = sp.GetRequiredService<IFonteMonitoradaRepository>();
        var publicacoes = sp.GetRequiredService<IPublicacaoDetectadaRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var opcoes = sp.GetRequiredService<IOptions<AssistenteOptions>>().Value;

        if (await fontes.CountAsync(ct) == 0)
        {
            foreach (var f in FontesPadrao.Criar()) await fontes.AddAsync(f, ct);
            await uow.SaveChangesAsync(ct);
        }

        var agora = DateTime.UtcNow;
        using var http = CriarHttpClient();
        foreach (var fonte in (await fontes.ListarAsync(apenasAtivas: true, ct)).Where(f => f.DevidaEm(agora)))
        {
            try
            {
                var itens = fonte.Mecanismo == MecanismoMonitoramento.Inlabs
                    ? await LerInlabsAsync(http, fonte, opcoes.Inlabs, agora, ct)
                    : ExtratorPublicacoes.Extrair(await http.GetStringAsync(fonte.Url, ct));
                if (itens == null) continue; // INLABS sem credenciais

                var primeiraLeitura = fonte.UltimoHash == null;
                fonte.RegistrarLeitura(ExtratorPublicacoes.HashConjunto(itens), agora);
                foreach (var item in itens)
                {
                    if (await publicacoes.ExisteHashAsync(fonte.Id, item.Hash, ct)) continue;
                    var p = PublicacaoDetectada.Criar(fonte.Id, item.Titulo, fonte.Url, item.Hash, agora);
                    if (primeiraLeitura) p.AtualizarStatus(StatusPublicacao.Descartada, "linha de base (primeira leitura da fonte)");
                    await publicacoes.AddAsync(p, ct);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Fonte {Fonte} não pôde ser lida.", fonte.Nome);
                fonte.RegistrarFalha(agora);
            }
            await uow.SaveChangesAsync(ct);
        }

        var classificador = sp.GetRequiredService<ClassificadorPublicacoes>();
        if (!classificador.Disponivel) return;
        foreach (var p in await publicacoes.ListarNaoClassificadasAsync(20, ct))
        {
            if (p.Status == StatusPublicacao.Descartada) { p.Classificar(false, null, null, Array.Empty<string>(), false); continue; }
            try
            {
                if (await classificador.ClassificarAsync(p, ct) is { } c)
                    p.Classificar(c.Relevante, c.Resumo, c.VigenciaInicio, c.ArtigosAfetados, c.ImpactoTecnico);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Classificação da publicação {Id} falhou.", p.Id);
            }
        }
        await uow.SaveChangesAsync(ct);
    }

    private static HttpClient CriarHttpClient()
    {
        // Cookies: o Portal da NF-e (ASP.NET) redireciona com AspxAutoDetectCookieSupport.
        var handler = new HttpClientHandler { CookieContainer = new CookieContainer(), AllowAutoRedirect = true, AutomaticDecompression = DecompressionMethods.All };
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(40) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return http;
    }

    /// <summary>
    /// DOU seção 1 do dia via INLABS (Imprensa Nacional) — fluxo do exemplo oficial github.com/Imprensa-Nacional/inlabs:
    /// login em logar.php, cookie inlabs_session_cookie, download do ZIP "AAAA-MM-DD-DO1.zip".
    /// </summary>
    private static async Task<List<ItemExtraido>?> LerInlabsAsync(HttpClient http, FonteMonitorada fonte, InlabsOptions cred,
        DateTime agora, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cred.Email) || string.IsNullOrWhiteSpace(cred.Senha)) return null;

        using var login = await http.PostAsync("https://inlabs.in.gov.br/logar.php",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["email"] = cred.Email, ["password"] = cred.Senha }), ct);
        login.EnsureSuccessStatusCode();

        var data = agora.AddHours(-3).ToString("yyyy-MM-dd"); // data de Brasília
        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://inlabs.in.gov.br/index.php?p={data}&dl={data}-DO1.zip");
        req.Headers.Add("origem", "736372697074");
        using var resp = await http.SendAsync(req, ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return new List<ItemExtraido>(); // edição ainda não publicada
        resp.EnsureSuccessStatusCode();

        var termos = (fonte.TermosFiltro ?? FontesPadrao.TermosDou).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var itens = new List<ItemExtraido>();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        foreach (var entrada in zip.Entries.Where(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
        {
            await using var s = entrada.Open();
            var xml = XDocument.Load(s);
            var artigo = xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "article");
            if (artigo == null) continue;
            var orgao = artigo.Attribute("artCategory")?.Value ?? "";
            var titulo = artigo.Descendants().FirstOrDefault(e => e.Name.LocalName == "Identifica")?.Value.Trim() ?? "";
            var texto = artigo.Descendants().FirstOrDefault(e => e.Name.LocalName == "Texto")?.Value ?? "";
            var orgaoRelevante = orgao.Contains("Política Fazendária", StringComparison.OrdinalIgnoreCase)
                || orgao.Contains("Receita Federal", StringComparison.OrdinalIgnoreCase)
                || orgao.Contains("Comitê Gestor", StringComparison.OrdinalIgnoreCase);
            if (!orgaoRelevante || !termos.Any(t => texto.Contains(t, StringComparison.OrdinalIgnoreCase) || titulo.Contains(t, StringComparison.OrdinalIgnoreCase)))
                continue;
            var linha = $"DOU {data} — {titulo} ({orgao.Split('/').LastOrDefault()})";
            itens.Add(new ItemExtraido(linha.Length > 480 ? linha[..480] : linha, ExtratorPublicacoes.Hash(linha)));
        }
        return itens;
    }
}
