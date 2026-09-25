using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.API.Workers.Assistente;

/// <summary>Retenção das conversas da Ori: apaga as sem atividade há mais de 180 dias (política LGPD, ESTRATEGIA.md R4).</summary>
public class ExpurgoConversasWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ExpurgoConversasWorker> _logger;

    public ExpurgoConversasWorker(IServiceScopeFactory scopes, IHostEnvironment env, ILogger<ExpurgoConversasWorker> logger)
    {
        _scopes = scopes;
        _env = env;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_env.IsEnvironment("Testing")) return;
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IConversaRepository>();
                var removidas = await repo.ExpurgarAnterioresAsync(DateTime.UtcNow.AddDays(-Conversa.DiasRetencao), stoppingToken);
                if (removidas > 0) _logger.LogInformation("Expurgo da Ori: {Quantidade} conversas removidas.", removidas);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha no expurgo de conversas da Ori.");
            }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
