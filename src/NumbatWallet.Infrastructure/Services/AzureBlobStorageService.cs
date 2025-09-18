using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly string _defaultContainerName;

    public AzureBlobStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;

        var storageAccountName = configuration["Azure:Storage:AccountName"];
        var connectionString = configuration["Azure:Storage:ConnectionString"];
        _defaultContainerName = configuration["Azure:Storage:DefaultContainer"] ?? "documents";

        if (!string.IsNullOrEmpty(connectionString))
        {
            // Use connection string if provided (for development)
            _blobServiceClient = new BlobServiceClient(connectionString);
            _logger.LogInformation("Azure Blob Storage client initialized with connection string");
        }
        else if (!string.IsNullOrEmpty(storageAccountName))
        {
            // Use managed identity for production
            var blobUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
            _blobServiceClient = new BlobServiceClient(blobUri, new DefaultAzureCredential());
            _logger.LogInformation("Azure Blob Storage client initialized with managed identity for account: {AccountName}", storageAccountName);
        }
        else
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured. Provide either ConnectionString or AccountName.");
        }
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string? containerName = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= _defaultContainerName;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            var blobName = GenerateBlobName(fileName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var blobUploadOptions = new BlobUploadOptions
            {
                Metadata = metadata,
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = GetContentType(fileName)
                }
            };

            await blobClient.UploadAsync(fileStream, blobUploadOptions, cancellationToken);

            _logger.LogInformation("Uploaded blob '{BlobName}' to container '{ContainerName}'", blobName, containerName);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading blob '{FileName}' to container '{ContainerName}'", fileName, containerName);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= _defaultContainerName;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Downloaded blob '{BlobName}' from container '{ContainerName}'", blobName, containerName);
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading blob '{BlobName}' from container '{ContainerName}'", blobName, containerName);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= _defaultContainerName;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);

            if (response.Value)
            {
                _logger.LogInformation("Deleted blob '{BlobName}' from container '{ContainerName}'", blobName, containerName);
            }
            else
            {
                _logger.LogWarning("Blob '{BlobName}' not found in container '{ContainerName}'", blobName, containerName);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blob '{BlobName}' from container '{ContainerName}'", blobName, containerName);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= _defaultContainerName;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.ExistsAsync(cancellationToken);
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if blob '{BlobName}' exists in container '{ContainerName}'", blobName, containerName);
            return false;
        }
    }

    public async Task<string> GetBlobUrlAsync(
        string blobName,
        string? containerName = null,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= _defaultContainerName;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (expiry.HasValue)
            {
                // Generate SAS URL for temporary access
                if (blobClient.CanGenerateSasUri)
                {
                    var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
                    {
                        BlobContainerName = containerName,
                        BlobName = blobName,
                        Resource = "b",
                        ExpiresOn = DateTimeOffset.UtcNow.Add(expiry.Value)
                    };

                    sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);

                    return blobClient.GenerateSasUri(sasBuilder).ToString();
                }
            }

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting URL for blob '{BlobName}' in container '{ContainerName}'", blobName, containerName);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListBlobsAsync(
        string? prefix = null,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= _defaultContainerName;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobs = new List<string>();

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                blobs.Add(blobItem.Name);
            }

            _logger.LogInformation("Listed {Count} blobs in container '{ContainerName}' with prefix '{Prefix}'", blobs.Count, containerName, prefix);
            return blobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing blobs in container '{ContainerName}' with prefix '{Prefix}'", containerName, prefix);
            throw;
        }
    }

    public async Task<Dictionary<string, string>?> GetMetadataAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= _defaultContainerName;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return new Dictionary<string, string>(properties.Value.Metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for blob '{BlobName}' in container '{ContainerName}'", blobName, containerName);
            return null;
        }
    }

    public async Task<bool> SetMetadataAsync(
        string blobName,
        Dictionary<string, string> metadata,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= _defaultContainerName;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.SetMetadataAsync(metadata, cancellationToken: cancellationToken);

            _logger.LogInformation("Set metadata for blob '{BlobName}' in container '{ContainerName}'", blobName, containerName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting metadata for blob '{BlobName}' in container '{ContainerName}'", blobName, containerName);
            return false;
        }
    }

    private string GenerateBlobName(string fileName)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N");
        var extension = Path.GetExtension(fileName);
        return $"{timestamp}/{guid}{extension}";
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".txt" => "text/plain",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            _ => "application/octet-stream"
        };
    }
}