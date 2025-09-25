using Microsoft.AspNetCore.OutputCaching;

namespace NumbatWallet.Web.Api.Caching;

/// <summary>
/// Service for managing cache invalidation
/// </summary>
public interface ICacheInvalidationService
{
    Task InvalidateCredentialCacheAsync(string credentialId, CancellationToken cancellationToken = default);
    Task InvalidateWalletCacheAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task InvalidateIssuanceCacheAsync(Guid issuanceId, CancellationToken cancellationToken = default);
    Task InvalidateUserCacheAsync(string userId, CancellationToken cancellationToken = default);
    Task InvalidateAllCacheAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of cache invalidation service
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        IOutputCacheStore outputCacheStore,
        ICacheService cacheService,
        ILogger<CacheInvalidationService> logger)
    {
        _outputCacheStore = outputCacheStore;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task InvalidateCredentialCacheAsync(string credentialId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Invalidating cache for credential {CredentialId}", credentialId);

        // Invalidate output cache
        await _outputCacheStore.EvictByTagAsync(CacheTags.Credentials, cancellationToken);
        await _outputCacheStore.EvictByTagAsync(CacheTags.ForCredential(credentialId), cancellationToken);

        // Invalidate application cache
        await _cacheService.RemoveAsync($"credential:{credentialId}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync($"credentials:*", cancellationToken);
    }

    public async Task InvalidateWalletCacheAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Invalidating cache for wallet {WalletId}", walletId);

        // Invalidate output cache
        await _outputCacheStore.EvictByTagAsync(CacheTags.Wallets, cancellationToken);
        await _outputCacheStore.EvictByTagAsync(CacheTags.ForWallet(walletId), cancellationToken);

        // Invalidate application cache
        await _cacheService.RemoveAsync($"wallet:{walletId}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync($"wallet:{walletId}:*", cancellationToken);
    }

    public async Task InvalidateIssuanceCacheAsync(Guid issuanceId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Invalidating cache for issuance {IssuanceId}", issuanceId);

        // Invalidate output cache
        await _outputCacheStore.EvictByTagAsync(CacheTags.Issuances, cancellationToken);

        // Invalidate application cache
        await _cacheService.RemoveAsync($"issuance:{issuanceId}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync($"issuances:*", cancellationToken);
    }

    public async Task InvalidateUserCacheAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Invalidating cache for user {UserId}", userId);

        // Invalidate output cache
        await _outputCacheStore.EvictByTagAsync(CacheTags.Users, cancellationToken);
        await _outputCacheStore.EvictByTagAsync(CacheTags.ForUser(userId), cancellationToken);

        // Invalidate application cache
        await _cacheService.RemoveAsync($"user:{userId}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync($"user:{userId}:*", cancellationToken);
    }

    public async Task InvalidateAllCacheAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Invalidating all cache");

        // Invalidate all output cache tags
        await _outputCacheStore.EvictByTagAsync(CacheTags.Credentials, cancellationToken);
        await _outputCacheStore.EvictByTagAsync(CacheTags.Wallets, cancellationToken);
        await _outputCacheStore.EvictByTagAsync(CacheTags.Issuances, cancellationToken);
        await _outputCacheStore.EvictByTagAsync(CacheTags.Organizations, cancellationToken);
        await _outputCacheStore.EvictByTagAsync(CacheTags.Users, cancellationToken);

        // Clear all application cache
        await _cacheService.RemoveByPrefixAsync("*", cancellationToken);
    }
}

/// <summary>
/// Background service for cache maintenance
/// </summary>
public class CacheMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheMaintenanceService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public CacheMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<CacheMaintenanceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                await PerformCacheMaintenanceAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache maintenance");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task PerformCacheMaintenanceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        _logger.LogInformation("Starting cache maintenance");

        try
        {
            // Perform basic cache maintenance
            // Since ICacheService doesn't have GetStatisticsAsync, we'll do basic cleanup
            // This could be extended with specific cache backend implementations

            _logger.LogInformation("Cache maintenance completed");

            // Example: Clear very old cache entries based on patterns
            // This is a placeholder - actual implementation depends on business rules
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform cache maintenance");
        }
    }
}

/// <summary>
/// Cache warmup service for preloading common data
/// </summary>
public interface ICacheWarmupService
{
    Task WarmupAsync(CancellationToken cancellationToken = default);
}

public class CacheWarmupService : ICacheWarmupService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheWarmupService> _logger;

    public CacheWarmupService(
        ICacheService cacheService,
        ILogger<CacheWarmupService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task WarmupAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cache warmup");

        try
        {
            // Preload configuration data
            await WarmupConfigurationAsync(cancellationToken);

            // Preload frequently accessed reference data
            await WarmupReferenceDataAsync(cancellationToken);

            _logger.LogInformation("Cache warmup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warmup failed");
            // Don't throw - cache warmup failure shouldn't prevent app startup
        }
    }

    private async Task WarmupConfigurationAsync(CancellationToken cancellationToken)
    {
        // Cache application configuration
        var config = new Dictionary<string, string>
        {
            ["app:version"] = "1.0.0",
            ["app:environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            ["features:caching"] = "true",
            ["features:webhooks"] = "false"
        };

        foreach (var kvp in config)
        {
            await _cacheService.SetAsync(
                $"config:{kvp.Key}",
                kvp.Value,
                TimeSpan.FromHours(24),
                cancellationToken);
        }
    }

    private async Task WarmupReferenceDataAsync(CancellationToken cancellationToken)
    {
        // Cache credential types
        var credentialTypes = new[]
        {
            "DriverLicense",
            "ProofOfAge",
            "ProofOfIdentity",
            "WorkingWithChildren",
            "ProofOfAddress"
        };

        await _cacheService.SetAsync(
            "reference:credentialTypes",
            credentialTypes,
            TimeSpan.FromHours(24),
            cancellationToken);

        // Cache issuance statuses
        var issuanceStatuses = new[]
        {
            "Pending",
            "Approved",
            "Rejected",
            "Completed",
            "Cancelled"
        };

        await _cacheService.SetAsync(
            "reference:issuanceStatuses",
            issuanceStatuses,
            TimeSpan.FromHours(24),
            cancellationToken);
    }
}