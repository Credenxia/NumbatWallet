using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services.Mocks;

/// <summary>
/// Mock blob storage service for development/testing when Azure Storage is not configured
/// POA: Provides null implementation to satisfy DI requirements
/// </summary>
public class MockBlobStorageService : IBlobStorageService
{
    private readonly ILogger<MockBlobStorageService> _logger;
    private readonly Dictionary<string, (byte[] Data, Dictionary<string, string> Metadata)> _inMemoryStorage = new();
    private const string DefaultContainer = "numbat-storage";

    public MockBlobStorageService(ILogger<MockBlobStorageService> logger)
    {
        _logger = logger;
        _logger.LogWarning("Using MockBlobStorageService - files are stored in memory only and will be lost on restart");
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string? containerName = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= DefaultContainer;
        _logger.LogDebug("Mock upload to {Container}/{FileName}", containerName, fileName);

        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);

        var key = $"{containerName}/{fileName}";
        _inMemoryStorage[key] = (memoryStream.ToArray(), metadata ?? new Dictionary<string, string>());

        return $"mock://storage/{key}";
    }

    public async Task<Stream> DownloadAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= DefaultContainer;
        _logger.LogDebug("Mock download from {Container}/{BlobName}", containerName, blobName);

        var key = $"{containerName}/{blobName}";
        if (_inMemoryStorage.TryGetValue(key, out var entry))
        {
            return new MemoryStream(entry.Data);
        }

        throw new FileNotFoundException($"Blob not found: {key}");
    }

    public async Task<bool> DeleteAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= DefaultContainer;
        _logger.LogDebug("Mock delete from {Container}/{BlobName}", containerName, blobName);

        var key = $"{containerName}/{blobName}";
        return await Task.FromResult(_inMemoryStorage.Remove(key));
    }

    public async Task<bool> ExistsAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= DefaultContainer;
        var key = $"{containerName}/{blobName}";
        return await Task.FromResult(_inMemoryStorage.ContainsKey(key));
    }

    public async Task<string> GetBlobUrlAsync(
        string blobName,
        string? containerName = null,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= DefaultContainer;
        var key = $"{containerName}/{blobName}";

        if (!_inMemoryStorage.ContainsKey(key))
        {
            throw new FileNotFoundException($"Blob not found: {key}");
        }

        var expirySeconds = (expiry ?? TimeSpan.FromHours(1)).TotalSeconds;
        return await Task.FromResult($"mock://storage/{key}?expires={expirySeconds}");
    }

    public async Task<IEnumerable<string>> ListBlobsAsync(
        string? prefix = null,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= DefaultContainer;
        _logger.LogDebug("Mock list blobs in {Container} with prefix {Prefix}", containerName, prefix);

        var containerPrefix = $"{containerName}/";
        var fullPrefix = string.IsNullOrEmpty(prefix) ? containerPrefix : $"{containerPrefix}{prefix}";

        var blobs = _inMemoryStorage.Keys
            .Where(k => k.StartsWith(fullPrefix))
            .Select(k => k.Substring(containerPrefix.Length))
            .ToList();

        return await Task.FromResult(blobs);
    }

    public async Task<Dictionary<string, string>?> GetMetadataAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= DefaultContainer;
        var key = $"{containerName}/{blobName}";

        if (_inMemoryStorage.TryGetValue(key, out var entry))
        {
            return await Task.FromResult(new Dictionary<string, string>(entry.Metadata));
        }

        return await Task.FromResult<Dictionary<string, string>?>(null);
    }

    public async Task<bool> SetMetadataAsync(
        string blobName,
        Dictionary<string, string> metadata,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        containerName ??= DefaultContainer;
        var key = $"{containerName}/{blobName}";

        if (_inMemoryStorage.TryGetValue(key, out var entry))
        {
            _inMemoryStorage[key] = (entry.Data, metadata);
            return await Task.FromResult(true);
        }

        return await Task.FromResult(false);
    }
}