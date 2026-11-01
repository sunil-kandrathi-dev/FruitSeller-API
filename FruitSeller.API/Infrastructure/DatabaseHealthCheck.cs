using FruitSeller.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FruitSeller.API.Infrastructure;

public sealed class DatabaseHealthCheck(FruitSellerDbContext db) : IHealthCheck
{
    // Used by /health/ready to verify the database is truly available before traffic is routed.
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await db.Database.CanConnectAsync(cancellationToken);
            return isHealthy
                ? HealthCheckResult.Healthy("Database is reachable.")
                : HealthCheckResult.Unhealthy("Database is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connectivity check failed.", ex);
        }
    }
}
