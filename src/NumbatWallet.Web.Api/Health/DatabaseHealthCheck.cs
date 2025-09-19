using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NumbatWallet.Infrastructure.Data;

namespace NumbatWallet.Web.Api.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly NumbatWalletDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        NumbatWalletDbContext dbContext,
        ILogger<DatabaseHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(logger);

        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test database connectivity
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                _logger.LogWarning("Database health check failed: Unable to connect");
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Execute a simple query to ensure the database is responding
            _ = await _dbContext.Database
                .SqlQuery<int>($"SELECT 1")
                .FirstOrDefaultAsync(cancellationToken);

            _logger.LogDebug("Database health check passed");

            return HealthCheckResult.Healthy("Database is responding", new Dictionary<string, object>
            {
                ["provider"] = "PostgreSQL",
                ["database"] = _dbContext.Database.GetConnectionString() ?? "unknown"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed with exception");

            return HealthCheckResult.Unhealthy(
                "Database check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
        }
    }
}