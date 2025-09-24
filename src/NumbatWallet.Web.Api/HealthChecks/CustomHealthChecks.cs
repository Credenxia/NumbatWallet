using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthCheckResult = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult;
using Path = System.IO.Path;
using Microsoft.EntityFrameworkCore;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Application.Interfaces;
using System.Net.Http;
using StackExchange.Redis;

namespace NumbatWallet.Web.Api.HealthChecks;

/// <summary>
/// Database connectivity health check
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly NumbatWalletDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(NumbatWalletDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test database connectivity with a simple query
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Test a simple query
            var testQuery = await _dbContext.Database.ExecuteSqlRawAsync(
                "SELECT 1", cancellationToken);

            // Get database statistics
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            var hasPendingMigrations = pendingMigrations.Any();

            var data = new Dictionary<string, object>
            {
                ["database"] = _dbContext.Database.GetDbConnection().Database ?? "unknown",
                ["provider"] = _dbContext.Database.ProviderName ?? "unknown",
                ["pendingMigrations"] = hasPendingMigrations
            };

            if (hasPendingMigrations)
            {
                return HealthCheckResult.Degraded(
                    "Database has pending migrations",
                    data: data);
            }

            return HealthCheckResult.Healthy("Database is responsive", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy(
                "Database health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
        }
    }
}

/// <summary>
/// Redis cache connectivity health check
/// </summary>
public class RedisCacheHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheHealthCheck> _logger;

    public RedisCacheHealthCheck(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCacheHealthCheck> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _connectionMultiplexer.GetDatabase();

            // Perform a simple ping
            var latency = await database.PingAsync();

            // Check connection status
            if (!_connectionMultiplexer.IsConnected)
            {
                return HealthCheckResult.Unhealthy("Redis is not connected");
            }

            var endpoints = _connectionMultiplexer.GetEndPoints();
            var server = _connectionMultiplexer.GetServer(endpoints.First());
            var info = await server.InfoAsync();

            var data = new Dictionary<string, object>
            {
                ["latencyMs"] = latency.TotalMilliseconds,
                ["connected"] = _connectionMultiplexer.IsConnected,
                ["endpoints"] = endpoints.Length,
                ["configuration"] = _connectionMultiplexer.Configuration ?? "unknown"
            };

            if (latency.TotalMilliseconds > 100)
            {
                return HealthCheckResult.Degraded(
                    $"Redis latency is high: {latency.TotalMilliseconds}ms",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Redis is responsive (latency: {latency.TotalMilliseconds:F2}ms)",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy(
                "Redis health check failed",
                exception: ex);
        }
    }
}


/// <summary>
/// External API health check for ServiceWA integration
/// </summary>
public class ServiceWAHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceWAHealthCheck> _logger;
    private readonly string _healthEndpoint;

    public ServiceWAHealthCheck(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ServiceWAHealthCheck> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _healthEndpoint = configuration["ServiceWA:HealthEndpoint"] ?? "https://api.servicewa.gov.au/health";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync(_healthEndpoint, cancellationToken);
            var responseTime = DateTime.UtcNow - startTime;

            var data = new Dictionary<string, object>
            {
                ["statusCode"] = (int)response.StatusCode,
                ["responseTimeMs"] = responseTime.TotalMilliseconds,
                ["endpoint"] = _healthEndpoint
            };

            if (!response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Unhealthy(
                    $"ServiceWA API returned {response.StatusCode}",
                    data: data);
            }

            if (responseTime.TotalMilliseconds > 2000)
            {
                return HealthCheckResult.Degraded(
                    $"ServiceWA API is slow: {responseTime.TotalMilliseconds:F2}ms",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "ServiceWA API is accessible",
                data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "ServiceWA health check failed - network error");
            return HealthCheckResult.Unhealthy(
                "Cannot reach ServiceWA API",
                exception: ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "ServiceWA health check timed out");
            return HealthCheckResult.Unhealthy(
                "ServiceWA API request timed out",
                exception: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ServiceWA health check failed");
            return HealthCheckResult.Unhealthy(
                "ServiceWA health check failed",
                exception: ex);
        }
    }
}

/// <summary>
/// Memory usage health check
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly ILogger<MemoryHealthCheck> _logger;
    private readonly long _maxMemoryMb;

    public MemoryHealthCheck(ILogger<MemoryHealthCheck> logger, IConfiguration configuration)
    {
        _logger = logger;
        _maxMemoryMb = configuration.GetValue<long>("HealthChecks:MaxMemoryMb", 1024); // Default 1GB
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            var totalMemory = GC.GetTotalMemory(false);
            var memoryMb = totalMemory / (1024 * 1024);
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);

            var data = new Dictionary<string, object>
            {
                ["allocatedMb"] = memoryMb,
                ["gen0Collections"] = gen0,
                ["gen1Collections"] = gen1,
                ["gen2Collections"] = gen2,
                ["totalAvailableMemoryMb"] = gcInfo.TotalAvailableMemoryBytes / (1024 * 1024),
                ["highMemoryLoadThresholdMb"] = gcInfo.HighMemoryLoadThresholdBytes / (1024 * 1024)
            };

            if (memoryMb > _maxMemoryMb)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Memory usage is too high: {memoryMb}MB > {_maxMemoryMb}MB",
                    data: data));
            }

            if (memoryMb > _maxMemoryMb * 0.8)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Memory usage is high: {memoryMb}MB",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Memory usage is normal: {memoryMb}MB",
                data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Memory health check failed",
                exception: ex));
        }
    }
}

/// <summary>
/// Disk space health check
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly ILogger<DiskSpaceHealthCheck> _logger;
    private readonly long _minFreeMb;
    private readonly string _path;

    public DiskSpaceHealthCheck(ILogger<DiskSpaceHealthCheck> logger, IConfiguration configuration)
    {
        _logger = logger;
        _minFreeMb = configuration.GetValue<long>("HealthChecks:MinFreeDiskMb", 500); // Default 500MB
        _path = configuration["HealthChecks:DiskPath"] ?? Path.GetTempPath();
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(_path) ?? "C:\\");

            if (!driveInfo.IsReady)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Drive {driveInfo.Name} is not ready"));
            }

            var freeSpaceMb = driveInfo.AvailableFreeSpace / (1024 * 1024);
            var totalSpaceMb = driveInfo.TotalSize / (1024 * 1024);
            var usedSpaceMb = totalSpaceMb - freeSpaceMb;
            var percentUsed = (double)usedSpaceMb / totalSpaceMb * 100;

            var data = new Dictionary<string, object>
            {
                ["drive"] = driveInfo.Name,
                ["freeSpaceMb"] = freeSpaceMb,
                ["totalSpaceMb"] = totalSpaceMb,
                ["percentUsed"] = percentUsed,
                ["fileSystem"] = driveInfo.DriveFormat
            };

            if (freeSpaceMb < _minFreeMb)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Insufficient disk space: {freeSpaceMb}MB < {_minFreeMb}MB",
                    data: data));
            }

            if (percentUsed > 90)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Disk usage is high: {percentUsed:F1}%",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Disk space is sufficient: {freeSpaceMb}MB free ({percentUsed:F1}% used)",
                data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disk space health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Disk space health check failed",
                exception: ex));
        }
    }
}

