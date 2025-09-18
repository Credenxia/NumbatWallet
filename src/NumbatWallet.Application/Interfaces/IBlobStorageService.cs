namespace NumbatWallet.Application.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string? containerName = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default);

    Task<string> GetBlobUrlAsync(
        string blobName,
        string? containerName = null,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> ListBlobsAsync(
        string? prefix = null,
        string? containerName = null,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, string>?> GetMetadataAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default);

    Task<bool> SetMetadataAsync(
        string blobName,
        Dictionary<string, string> metadata,
        string? containerName = null,
        CancellationToken cancellationToken = default);
}