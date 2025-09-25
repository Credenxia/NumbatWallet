using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthCheckResult = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult;

namespace NumbatWallet.Web.Api.Health;

public class StorageHealthCheck : IHealthCheck
{
    private readonly IBlobStorageService? _blobStorageService;
    private readonly ILogger<StorageHealthCheck> _logger;

    public StorageHealthCheck(
        IBlobStorageService? blobStorageService,
        ILogger<StorageHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // If blob storage service is not configured, return healthy (optional dependency)
            if (_blobStorageService == null)
            {
                _logger.LogDebug("Storage health check skipped: Blob storage service not configured");
                return HealthCheckResult.Healthy("Storage service not configured (optional)",
                    new Dictionary<string, object>
                    {
                        ["status"] = "not_configured",
                        ["optional"] = true
                    });
            }

            // Try to check if service is available by attempting to list blobs
            // This is a basic check since IBlobStorageService doesn't expose connection details
            var exists = await _blobStorageService.ExistsAsync("health-check-test.txt", cancellationToken: cancellationToken);

            _logger.LogDebug("Storage health check passed");

            return HealthCheckResult.Healthy("Storage service is accessible", new Dictionary<string, object>
            {
                ["status"] = "connected",
                ["type"] = _blobStorageService.GetType().Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage health check failed with exception");

            return HealthCheckResult.Unhealthy(
                "Storage check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
        }
    }
}
