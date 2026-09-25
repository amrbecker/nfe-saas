using System.Net;
using Microsoft.Extensions.Options;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Cs;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.API.Workers.Assistente;

/// <summary>
/// Customer Success proativo da Ori (a cada hora): avalia as regras determinísticas, grava alertas deduplicados pela chave,
/// dispara lembretes devidos e envia e-mail de certificado vencendo/trial acabando aos admins do escritório.
/// </summary>
public class SaudeContaWorker : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);
    private static readonly TipoAlertaCs[] TiposComEmail = { TipoAlertaCs.CertificadoVencendo, TipoAlertaCs.TrialAcabando };

    private readonly IServiceScopeFactory _scopes;
    private readonly IHostEnvironment _env;
    private readonly ILogger<SaudeContaWorker> _logger;

    public SaudeContaWorker(IServiceScopeFactory scopes, IHostEnvironment env, ILogger<SaudeContaWorker> logger)
    {
        _scopes = scopes;
        _env = env;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_env.IsEnvironment("Testing")) return;
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                if (scope.ServiceProvider.GetRequiredService<IOptions<AssistenteOptions>>().Value.Habilitado)
                    await ExecutarAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha no ciclo do SaudeContaWorker.");
            }
            await Task.Delay(Intervalo, stoppingToken);
        }
    }

    public static async Task ExecutarAsync(IServiceProvider sp, CancellationToken ct)
    {
        var consultas = sp.GetRequiredService<IConsultasAssistenteRepository>();
        var alertas = sp.GetRequiredService<IAlertaCsRepository>();
        var lembretes = sp.GetRequiredService<ILembreteEmissaoRepository>();
        var kb = sp.GetRequiredService<IBaseConhecimento>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var agora = DateTime.UtcNow;
        var propostos = new List<AlertaProposto>();

        foreach (var esc in await consultas.EscritoriosAtivosAsync(ct))
            propostos.AddRange(RegrasSaudeConta.AvaliarEscritorio(esc, agora));

        foreach (var e in await consultas.EmpresasAtivasAsync(ct))
        {
            var notas = await consultas.NotasAsync(e.EmpresaId, agora.AddDays(-100), null, 3000, ct);
            var foto = new FotoEmpresa(e, notas,
                await consultas.TeveNotaAutorizadaAsync(e.EmpresaId, null, ct),
                await consultas.TeveNotaAutorizadaAsync(e.EmpresaId, AmbienteSefaz.Homologacao, ct),
                await consultas.TeveNotaAutorizadaAsync(e.EmpresaId, AmbienteSefaz.Producao, ct));
            foreach (var p in RegrasSaudeConta.AvaliarEmpresa(foto, agora, kb.ExtrairCodigoRejeicao))
            {
                // Recorrência já coberta por um lembrete não vira alerta.
                if (p.Tipo == TipoAlertaCs.NotaRecorrente && Guid.TryParse(p.Link?.Split('=').Last(), out var notaModelo)
                    && await lembretes.ExisteParaNotaModeloAsync(e.EmpresaId, notaModelo, ct))
                    continue;
                propostos.Add(p);
            }
        }

        // Lembretes devidos (A7): avisa e agenda o próximo.
        var empresasPorId = (await consultas.EmpresasAtivasAsync(ct)).ToDictionary(e => e.EmpresaId);
        foreach (var l in await lembretes.ListarDevidosAsync(agora, ct))
        {
            if (!empresasPorId.TryGetValue(l.EmpresaId, out var empresa)) continue;
            propostos.Add(new AlertaProposto(empresa.EscritorioId, l.EmpresaId, TipoAlertaCs.LembreteEmissao,
                $"lembrete:{l.Id}:{l.ProximaEm:yyyyMMdd}", $"Lembrete: {l.Descricao} ({empresa.Nome}). O formulário abre preparado para você conferir e emitir.",
                $"/emitir?origem={l.NotaModeloId}"));
            l.RegistrarAviso(agora);
        }

        var novos = 0;
        foreach (var p in propostos.DistinctBy(p => p.Chave))
        {
            if (await alertas.ExisteChaveAsync(p.Chave, ct)) continue;
            await alertas.AddAsync(AlertaCs.Criar(p.EscritorioId, p.EmpresaId, p.Tipo, p.Chave, p.Mensagem, p.Link, p.Interno), ct);
            novos++;
        }
        await uow.SaveChangesAsync(ct);
        if (novos > 0) sp.GetRequiredService<ILogger<SaudeContaWorker>>().LogInformation("Ori: {Novos} alertas de CS criados.", novos);

        await EnviarEmailsAsync(sp, alertas, consultas, uow, ct);
    }

    private static async Task EnviarEmailsAsync(IServiceProvider sp, IAlertaCsRepository alertas, IConsultasAssistenteRepository consultas,
        IUnitOfWork uow, CancellationToken ct)
    {
        var email = sp.GetRequiredService<IEmailService>();
        var opcoes = sp.GetRequiredService<IOptions<AssistenteOptions>>().Value;
        var baseUrl = (opcoes.WebUiBaseUrl ?? sp.GetRequiredService<IConfiguration>()["WebUI:BaseUrl"] ?? "").TrimEnd('/');

        foreach (var a in await alertas.ListarPendentesDeEmailAsync(TiposComEmail, ct))
        {
            var destinatarios = await consultas.EmailsAdminsAsync(a.EscritorioId, ct);
            var enviado = false;
            foreach (var d in destinatarios)
            {
                var link = string.IsNullOrEmpty(baseUrl) ? "" : $"<p><a href=\"{baseUrl}{a.LinkAcao}\">Abrir o NFeFlow</a></p>";
                enviado |= await email.EnviarAsync(d, "NFeFlow — " + (a.Tipo == TipoAlertaCs.TrialAcabando ? "seu período de teste" : "certificado digital"),
                    $"<p>{WebUtility.HtmlEncode(a.Mensagem)}</p>{link}<p>— Ori, a assistente do NFeFlow</p>", ct);
            }
            // Sem destinatário ou provedor configurado: marca mesmo assim para não tentar para sempre.
            if (enviado || destinatarios.Count == 0) a.MarcarEmailEnviado();
        }
        await uow.SaveChangesAsync(ct);
    }
}
