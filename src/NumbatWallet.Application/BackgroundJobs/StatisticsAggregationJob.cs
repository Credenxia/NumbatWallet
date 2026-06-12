using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Application.BackgroundJobs;

/// <summary>
/// Background job that periodically aggregates statistics for dashboards.
/// Runs every 15 minutes.
/// </summary>
public class StatisticsAggregationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<StatisticsAggregationJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StatisticsAggregationJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AggregateStatisticsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error aggregating statistics");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task AggregateStatisticsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var statisticsService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();

        var snapshot = await statisticsService.GetMetricsSnapshotAsync(
            DateTime.UtcNow.AddMinutes(-15),
            DateTime.UtcNow,
            cancellationToken);

        logger.LogDebug("Statistics snapshot aggregated: {SnapshotTime}", DateTime.UtcNow);
    }
}
