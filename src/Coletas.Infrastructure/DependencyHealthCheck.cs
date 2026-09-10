using Coletas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace Coletas.Infrastructure;
/// <summary>Readiness exige esquema migrado, PostGIS e Redis acessíveis.</summary>
public sealed class DependencyHealthCheck(ColetasDbContext database, IDistributedCache cache) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Motivo: SELECT 1 sozinho daria pronto antes da migration e da extensão espacial.
            // Mudança: docs/mudancas/2026-09-09-02-fase-zero.md
            _ = await database.SystemSettings.AnyAsync(cancellationToken);
            _ = await database.Database.SqlQueryRaw<string>("SELECT postgis_version() AS \"Value\"").SingleAsync(cancellationToken);
            _ = await cache.GetAsync("readiness", cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Dependências indisponíveis.");
        }
    }
}
