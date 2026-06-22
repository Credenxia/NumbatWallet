using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Application.BackgroundJobs;

/// <summary>
/// Background job that performs periodic database cleanup (expired tokens, old audit logs, etc.).
/// Runs daily at midnight UTC.
/// </summary>
public class DatabaseCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseCleanupJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DatabaseCleanupJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during database cleanup");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        if (scope.ServiceProvider.GetService<IDatabaseMaintenanceService>() is { } maintenanceService)
        {
            await maintenanceService.CleanupExpiredTokensAsync(cancellationToken);
            await maintenanceService.CleanupOldAuditLogsAsync(DateTime.UtcNow.AddDays(-90), cancellationToken);
            logger.LogInformation("Database cleanup completed");
        }
        else
        {
            logger.LogDebug("IDatabaseMaintenanceService not registered, skipping cleanup");
        }
    }
}

/// <summary>
/// Interface for database maintenance operations.
/// </summary>
public interface IDatabaseMaintenanceService
{
    Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
    Task CleanupOldAuditLogsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
