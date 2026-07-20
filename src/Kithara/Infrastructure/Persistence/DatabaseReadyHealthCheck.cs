using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kithara.Infrastructure.Persistence;

public sealed class DatabaseReadyHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<KitharaDbContext> _dbContextFactory;

    public DatabaseReadyHealthCheck(IDbContextFactory<KitharaDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var canConnect = await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
        return canConnect
            ? HealthCheckResult.Healthy("Database reachable.")
            : HealthCheckResult.Unhealthy("Database unreachable.");
    }
}
