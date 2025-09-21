using System.Net.Http.Headers;

namespace NumbatWallet.Web.Admin.Services;

/// <summary>
/// REST API client interface for file operations only.
/// All other data operations should use GraphQL.
/// </summary>
public interface IFileApiClient
{
    /// <summary>
    /// Upload a file to the API
    /// </summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a file from the API
    /// </summary>
    Task<(Stream Stream, string ContentType, string FileName)> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from the API
    /// </summary>
    Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export data as a file (CSV, Excel, PDF)
    /// </summary>
    Task<(Stream Stream, string ContentType, string FileName)> ExportDataAsync(string exportType, string entityType, Dictionary<string, string>? filters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Import data from a file
    /// </summary>
    Task<ImportResult> ImportDataAsync(Stream fileStream, string fileName, string entityType, CancellationToken cancellationToken = default);
}

public class ImportResult
{
    public bool Success { get; set; }
    public int TotalRecords { get; set; }
    public int ImportedRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<string> Errors { get; set; } = new();
}