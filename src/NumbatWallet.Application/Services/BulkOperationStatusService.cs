using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.DTOs;
using System.Collections.Concurrent;

namespace NumbatWallet.Application.Services;

/// <summary>
/// Service for tracking and managing bulk operation status
/// POA: Issue #187 - Bulk operation status tracking
/// </summary>
public interface IBulkOperationStatusService
{
    Task<OperationStatusDto?> GetOperationStatusAsync(string operationId, CancellationToken cancellationToken = default);
    Task<OperationResultsDto?> GetOperationResultsAsync(string operationId, CancellationToken cancellationToken = default);
    Task<bool> CancelOperationAsync(string operationId, CancellationToken cancellationToken = default);
    Task UpdateOperationStatusAsync(string operationId, OperationStatusDto status, CancellationToken cancellationToken = default);
    Task UpdateOperationResultsAsync(string operationId, OperationResultsDto results, CancellationToken cancellationToken = default);
}

public class BulkOperationStatusService : IBulkOperationStatusService
{
    private readonly ILogger<BulkOperationStatusService> _logger;

    // In-memory cache for development - replace with distributed cache for production
    private static readonly ConcurrentDictionary<string, OperationStatusDto> _statusCache = new();
    private static readonly ConcurrentDictionary<string, OperationResultsDto> _resultsCache = new();
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();
    private static readonly ConcurrentDictionary<string, DateTime> _cacheExpiry = new();
    private const int CacheExpirationMinutes = 60;

    public BulkOperationStatusService(ILogger<BulkOperationStatusService> logger)
    {
        _logger = logger;
    }

    public Task<OperationStatusDto?> GetOperationStatusAsync(
        string operationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CleanExpiredCache();

            if (_statusCache.TryGetValue(operationId, out var status))
            {
                return Task.FromResult<OperationStatusDto?>(status);
            }

            return Task.FromResult<OperationStatusDto?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving operation status for {OperationId}", operationId);
            return Task.FromResult<OperationStatusDto?>(null);
        }
    }

    public Task<OperationResultsDto?> GetOperationResultsAsync(
        string operationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CleanExpiredCache();

            if (_resultsCache.TryGetValue(operationId, out var results))
            {
                return Task.FromResult<OperationResultsDto?>(results);
            }

            return Task.FromResult<OperationResultsDto?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving operation results for {OperationId}", operationId);
            return Task.FromResult<OperationResultsDto?>(null);
        }
    }

    public async Task<bool> CancelOperationAsync(
        string operationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get the cancellation token for this operation
            if (_cancellationTokens.TryRemove(operationId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();

                // Update status in cache
                if (_statusCache.TryGetValue(operationId, out var status))
                {
                    if (status.Status == "Processing")
                    {
                        status.Status = "Cancelled";
                        status.CompletedAt = DateTime.UtcNow;
                        status.Duration = status.CompletedAt.Value - status.StartedAt;

                        await UpdateOperationStatusAsync(operationId, status, cancellationToken);
                    }
                }

                _logger.LogInformation("Operation {OperationId} cancelled", operationId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling operation {OperationId}", operationId);
            return false;
        }
    }

    public Task UpdateOperationStatusAsync(
        string operationId,
        OperationStatusDto status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _statusCache.AddOrUpdate(operationId, status, (key, old) => status);
            _cacheExpiry.AddOrUpdate(operationId,
                DateTime.UtcNow.AddMinutes(CacheExpirationMinutes),
                (key, old) => DateTime.UtcNow.AddMinutes(CacheExpirationMinutes));

            _logger.LogDebug("Updated status for operation {OperationId}: {Status}",
                operationId, status.Status);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operation status for {OperationId}", operationId);
            return Task.CompletedTask;
        }
    }

    public Task UpdateOperationResultsAsync(
        string operationId,
        OperationResultsDto results,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _resultsCache.AddOrUpdate(operationId, results, (key, old) => results);
            _cacheExpiry.AddOrUpdate(operationId,
                DateTime.UtcNow.AddMinutes(CacheExpirationMinutes),
                (key, old) => DateTime.UtcNow.AddMinutes(CacheExpirationMinutes));

            // Clean up cancellation token if operation is complete
            if (results.Status == "Completed" || results.Status == "Failed" || results.Status == "Cancelled")
            {
                if (_cancellationTokens.TryRemove(operationId, out var cts))
                {
                    cts.Dispose();
                }
            }

            _logger.LogDebug("Updated results for operation {OperationId}: {SuccessCount}/{TotalCount} succeeded",
                operationId, results.SuccessCount, results.TotalCount);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operation results for {OperationId}", operationId);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Register a cancellation token for an operation (internal use)
    /// </summary>
    public static void RegisterOperationCancellation(string operationId, CancellationTokenSource cts)
    {
        _cancellationTokens.TryAdd(operationId, cts);
    }

    /// <summary>
    /// Clean up expired cache entries
    /// </summary>
    internal static void CleanExpiredCache()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cacheExpiry
            .Where(kvp => kvp.Value < now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cacheExpiry.TryRemove(key, out _);
            _statusCache.TryRemove(key, out _);
            _resultsCache.TryRemove(key, out _);

            if (_cancellationTokens.TryRemove(key, out var cts))
            {
                cts.Dispose();
            }
        }
    }
}

/// <summary>
/// Background service for cleaning up completed operations
/// </summary>
public class BulkOperationCleanupService : BackgroundService
{
    private readonly ILogger<BulkOperationCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public BulkOperationCleanupService(ILogger<BulkOperationCleanupService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                // Trigger cleanup of expired cache entries by calling static method
                BulkOperationStatusService.CleanExpiredCache();

                _logger.LogDebug("Cleaned up expired bulk operations");
            }
            catch (TaskCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk operation cleanup");
            }
        }
    }
}