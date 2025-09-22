using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Infrastructure.Data;
using System.Diagnostics;

namespace NumbatWallet.Infrastructure.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly NumbatWalletDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly IBlobStorageService _blobStorageService;

    public HealthCheckService(
        NumbatWalletDbContext dbContext,
        ICacheService cacheService,
        IBlobStorageService blobStorageService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _blobStorageService = blobStorageService;
    }

    public async Task<HealthStatusDto> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var health = new HealthStatusDto
        {
            Components = new Dictionary<string, ComponentHealthDto>()
        };

        // Check database
        var dbHealth = await CheckDatabaseHealthAsync(cancellationToken);
        health.Components["database"] = dbHealth;

        // Check cache
        var cacheHealth = await CheckCacheHealthAsync(cancellationToken);
        health.Components["cache"] = cacheHealth;

        // Check storage
        var storageHealth = await CheckStorageHealthAsync(cancellationToken);
        health.Components["storage"] = storageHealth;

        // Determine overall status
        var hasUnhealthy = health.Components.Any(c => c.Value.Status == "Unhealthy");
        var hasDegraded = health.Components.Any(c => c.Value.Status == "Degraded");

        if (hasUnhealthy)
        {
            health.Status = "Unhealthy";
        }
        else if (hasDegraded)
        {
            health.Status = "Degraded";
        }
        else
        {
            health.Status = "Healthy";
        }

        return health;
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        var healthStatus = await GetHealthStatusAsync(cancellationToken);

        // Convert HealthStatusDto to SystemHealthDto
        var systemHealth = new SystemHealthDto
        {
            Status = healthStatus.Status,
            Components = healthStatus.Components,
            CheckedAt = DateTime.UtcNow
        };

        // Add system metrics
        var process = Process.GetCurrentProcess();
        systemHealth.Metrics = new SystemMetrics
        {
            MemoryUsed = process.WorkingSet64,
            MemoryTotal = GC.GetTotalMemory(false),
            CpuUsage = 0, // Would need performance counters for actual CPU
            ActiveConnections = 0, // Would need to track this
            RequestsPerSecond = 0, // Would need to track this
            AverageResponseTime = 0,
            TotalRequests = 0,
            FailedRequests = 0,
            DiskUsed = 0,
            DiskTotal = 0
        };

        return systemHealth;
    }

    public async Task<bool> CheckDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CheckCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var testKey = "health_check_" + Guid.NewGuid();
            await _cacheService.SetAsync(testKey, "test", TimeSpan.FromSeconds(10), cancellationToken);
            var value = await _cacheService.GetAsync<string>(testKey, cancellationToken);
            return value == "test";
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CheckStorageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple connectivity check - would list containers in production
            var testKey = "health-check-" + Guid.NewGuid() + ".txt";
            var testData = System.Text.Encoding.UTF8.GetBytes("test");
            using var stream = new MemoryStream(testData);
            await _blobStorageService.UploadAsync(stream, testKey, "health", null, cancellationToken);
            await _blobStorageService.DeleteAsync(testKey, "health", cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<ComponentHealthDto> CheckDatabaseHealthAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var health = new ComponentHealthDto();

        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            stopwatch.Stop();

            if (canConnect)
            {
                // Try a simple query
                var count = await _dbContext.Set<Domain.Aggregates.Person>().CountAsync(cancellationToken);
                health.Status = "Healthy";
                health.Description = $"Connected, {count} persons in database";
            }
            else
            {
                health.Status = "Unhealthy";
                health.Description = "Cannot connect to database";
            }

            health.ResponseTime = stopwatch.Elapsed;
            health.Details = new Dictionary<string, object>
            {
                { "provider", "PostgreSQL" },
                { "responseTimeMs", stopwatch.ElapsedMilliseconds }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            health.Status = "Unhealthy";
            health.Description = $"Database error: {ex.Message}";
            health.ResponseTime = stopwatch.Elapsed;
        }

        return health;
    }

    private async Task<ComponentHealthDto> CheckCacheHealthAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var health = new ComponentHealthDto();

        try
        {
            var testKey = "health_check_" + Guid.NewGuid();
            await _cacheService.SetAsync(testKey, "test", TimeSpan.FromSeconds(10), cancellationToken);
            var value = await _cacheService.GetAsync<string>(testKey, cancellationToken);
            stopwatch.Stop();

            if (value == "test")
            {
                health.Status = "Healthy";
                health.Description = "Cache is responsive";
            }
            else
            {
                health.Status = "Degraded";
                health.Description = "Cache responding but value mismatch";
            }

            health.ResponseTime = stopwatch.Elapsed;
            health.Details = new Dictionary<string, object>
            {
                { "provider", "Redis" },
                { "responseTimeMs", stopwatch.ElapsedMilliseconds }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            health.Status = "Unhealthy";
            health.Description = $"Cache error: {ex.Message}";
            health.ResponseTime = stopwatch.Elapsed;
        }

        return health;
    }

    private async Task<ComponentHealthDto> CheckStorageHealthAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var health = new ComponentHealthDto();

        try
        {
            var testKey = "health-check-" + Guid.NewGuid() + ".txt";
            var testData = System.Text.Encoding.UTF8.GetBytes("test");
            using var stream = new MemoryStream(testData);
            await _blobStorageService.UploadAsync(stream, testKey, "health", null, cancellationToken);
            await _blobStorageService.DeleteAsync(testKey, "health", cancellationToken);
            stopwatch.Stop();

            health.Status = "Healthy";
            health.Description = "Storage is responsive";
            health.ResponseTime = stopwatch.Elapsed;
            health.Details = new Dictionary<string, object>
            {
                { "provider", "Azure Blob Storage" },
                { "responseTimeMs", stopwatch.ElapsedMilliseconds }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            health.Status = "Degraded"; // Storage is less critical than DB
            health.Description = $"Storage error: {ex.Message}";
            health.ResponseTime = stopwatch.Elapsed;
        }

        return health;
    }
}
