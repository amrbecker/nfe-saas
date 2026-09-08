using Microsoft.Extensions.Diagnostics.HealthChecks;
using NfeSaas.Infrastructure.Data;

namespace NfeSaas.API.HealthChecks;

/// <summary>
/// Checa conexão real com o Postgres — o `/health` sem isso só confirma que o processo está de pé,
/// não que a aplicação de fato consegue servir requisições (ex.: Neon em autosuspend/indisponível).
/// </summary>
public class PostgresHealthCheck : IHealthCheck
{
    private readonly NfeDbContext _db;

    public PostgresHealthCheck(NfeDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var conectou = await _db.Database.CanConnectAsync(cancellationToken);
            return conectou
                ? HealthCheckResult.Healthy("Conexão com o Postgres OK.")
                : HealthCheckResult.Unhealthy("Não foi possível conectar ao Postgres.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha ao verificar conexão com o Postgres.", ex);
        }
    }
}
