using NfeSaas.Application.Assistente.Insights;

namespace NfeSaas.API.Workers.Assistente;

/// <summary>Relatórios semanais da Ori (insights ao PO e fila ao curador): segunda-feira, 12:00 UTC (09:00 em Brasília).</summary>
public class InsightsWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IHostEnvironment _env;
    private readonly ILogger<InsightsWorker> _logger;

    public InsightsWorker(IServiceScopeFactory scopes, IHostEnvironment env, ILogger<InsightsWorker> logger)
    {
        _scopes = scopes;
        _env = env;
        _logger = logger;
    }

    public static DateTime ProximaExecucao(DateTime agora)
    {
        var alvo = agora.Date.AddHours(12);
        while (alvo.DayOfWeek != DayOfWeek.Monday || alvo <= agora) alvo = alvo.AddDays(1);
        return alvo;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_env.IsEnvironment("Testing")) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            var espera = ProximaExecucao(DateTime.UtcNow) - DateTime.UtcNow;
            await Task.Delay(espera, stoppingToken);
            try
            {
                using var scope = _scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<RelatoriosSemanais>().EnviarAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha ao enviar os relatórios semanais da Ori.");
            }
        }
    }
}
