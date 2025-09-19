using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NumbatWallet.Web.Api.Health;

public class StorageHealthCheck : IHealthCheck
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<StorageHealthCheck> _logger;

    public StorageHealthCheck(
        BlobServiceClient blobServiceClient,
        ILogger<StorageHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(blobServiceClient);
        ArgumentNullException.ThrowIfNull(logger);

        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get account info to verify connectivity
            var accountInfo = await _blobServiceClient.GetAccountInfoAsync(cancellationToken);

            if (accountInfo == null)
            {
                _logger.LogWarning("Storage health check failed: Unable to get account info");
                return HealthCheckResult.Unhealthy("Cannot connect to storage account");
            }

            // Try to list containers (limit to 1 for performance)
            var containers = _blobServiceClient.GetBlobContainers(cancellationToken: cancellationToken);
            var containerExists = await Task.Run(() => containers.Any(), cancellationToken);

            _logger.LogDebug("Storage health check passed");

            return HealthCheckResult.Healthy("Storage account is accessible", new Dictionary<string, object>
            {
                ["account_kind"] = accountInfo.Value.AccountKind.ToString(),
                ["sku_name"] = accountInfo.Value.SkuName.ToString(),
                ["is_hns_enabled"] = accountInfo.Value.IsHierarchicalNamespaceEnabled
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