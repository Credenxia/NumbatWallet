using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace NumbatWallet.Web.Api.Health;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(
        IConnectionMultiplexer redis,
        ILogger<RedisHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);

        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("Redis health check failed: Not connected");
                return HealthCheckResult.Unhealthy("Redis is not connected");
            }

            var database = _redis.GetDatabase();
            var testKey = $"health-check-{Guid.NewGuid():N}";
            var testValue = DateTime.UtcNow.ToString("O");

            // Write test value
            await database.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(10));

            // Read test value
            var retrievedValue = await database.StringGetAsync(testKey);

            // Delete test key
            await database.KeyDeleteAsync(testKey);

            if (retrievedValue != testValue)
            {
                _logger.LogWarning("Redis health check failed: Value mismatch");
                return HealthCheckResult.Degraded("Redis is connected but not functioning properly");
            }

            _logger.LogDebug("Redis health check passed");

            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints[0]);

            return HealthCheckResult.Healthy("Redis is healthy", new Dictionary<string, object>
            {
                ["endpoints"] = string.Join(", ", endpoints.Select(e => e.ToString())),
                ["connected_clients"] = server.IsConnected ? "available" : "unavailable",
                ["version"] = server.IsConnected ? server.Version.ToString() : "unknown"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed with exception");

            return HealthCheckResult.Unhealthy(
                "Redis check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
        }
    }
}