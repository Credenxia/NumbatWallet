using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Infrastructure.Azure;

/// <summary>
/// Azure Blob Storage service implementation
/// POA: Phase 3 - Storage integration
/// </summary>
public class AzureStorageService : IAzureStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureStorageService> _logger;

    public AzureStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<AzureStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadBlobAsync(
        string containerName,
        string blobName,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync(
                PublicAccessType.None,
                cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = new MemoryStream(data);
            var response = await blobClient.UploadAsync(
                stream,
                overwrite: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Successfully uploaded blob {BlobName} to container {ContainerName}",
                blobName, containerName);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to upload blob {BlobName} to container {ContainerName}",
                blobName, containerName);
            throw;
        }
    }

    public async Task<byte[]> DownloadBlobAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadContentAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully downloaded blob {BlobName} from container {ContainerName}",
                blobName, containerName);

            return response.Value.Content.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to download blob {BlobName} from container {ContainerName}",
                blobName, containerName);
            throw;
        }
    }

    public async Task<bool> DeleteBlobAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                cancellationToken: cancellationToken);

            if (response.Value)
            {
                _logger.LogInformation(
                    "Successfully deleted blob {BlobName} from container {ContainerName}",
                    blobName, containerName);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete blob {BlobName} from container {ContainerName}",
                blobName, containerName);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListBlobsAsync(
        string containerName,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobs = new List<string>();

            await foreach (var blobItem in containerClient.GetBlobsAsync(
                prefix: prefix,
                cancellationToken: cancellationToken))
            {
                blobs.Add(blobItem.Name);
            }

            _logger.LogInformation(
                "Listed {Count} blobs in container {ContainerName} with prefix {Prefix}",
                blobs.Count, containerName, prefix ?? "none");

            return blobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to list blobs in container {ContainerName}",
                containerName);
            throw;
        }
    }
}

/// <summary>
/// Mock implementation for development/testing
/// </summary>
public class MockAzureStorageService : IAzureStorageService
{
    private readonly Dictionary<string, Dictionary<string, byte[]>> _storage = new();
    private readonly ILogger<MockAzureStorageService> _logger;

    public MockAzureStorageService(ILogger<MockAzureStorageService> logger)
    {
        _logger = logger;
    }

    public Task<string> UploadBlobAsync(
        string containerName,
        string blobName,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        if (!_storage.ContainsKey(containerName))
        {
            _storage[containerName] = new Dictionary<string, byte[]>();
        }

        _storage[containerName][blobName] = data;
        var uri = $"https://mock.blob.core.windows.net/{containerName}/{blobName}";

        _logger.LogInformation("Mock uploaded {Bytes} bytes to {Uri}", data.Length, uri);
        return Task.FromResult(uri);
    }

    public Task<byte[]> DownloadBlobAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        if (_storage.TryGetValue(containerName, out var container))
        {
            if (container.TryGetValue(blobName, out var data))
            {
                return Task.FromResult(data);
            }
        }

        throw new FileNotFoundException($"Blob {blobName} not found in container {containerName}");
    }

    public Task<bool> DeleteBlobAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        if (_storage.TryGetValue(containerName, out var container))
        {
            return Task.FromResult(container.Remove(blobName));
        }

        return Task.FromResult(false);
    }

    public Task<IEnumerable<string>> ListBlobsAsync(
        string containerName,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        if (_storage.TryGetValue(containerName, out var container))
        {
            var blobs = container.Keys.AsEnumerable();

            if (!string.IsNullOrEmpty(prefix))
            {
                blobs = blobs.Where(b => b.StartsWith(prefix));
            }

            return Task.FromResult(blobs);
        }

        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }
}