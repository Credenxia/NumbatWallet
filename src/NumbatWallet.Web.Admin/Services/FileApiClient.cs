using System.Net.Http.Headers;
using System.Text.Json;

namespace NumbatWallet.Web.Admin.Services;

/// <summary>
/// REST API client implementation for file operations only.
/// All other data operations should use GraphQL.
/// </summary>
public class FileApiClient : IFileApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FileApiClient(HttpClient httpClient, ILogger<FileApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync("/api/files/upload", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync(cancellationToken);
            var fileId = JsonDocument.Parse(result).RootElement.GetProperty("fileId").GetString();

            return fileId ?? throw new InvalidOperationException("File ID not returned from upload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", fileName);
            throw;
        }
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/files/{fileId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"file_{fileId}";

            return (stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", fileId);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/files/{fileId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", fileId);
            return false;
        }
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> ExportDataAsync(
        string exportType,
        string entityType,
        Dictionary<string, string>? filters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryString = filters != null
                ? "?" + string.Join("&", filters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"))
                : "";

            var response = await _httpClient.GetAsync(
                $"/api/export/{entityType}/{exportType}{queryString}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? GetContentTypeForExport(exportType);
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? $"{entityType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{exportType}";

            return (stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting {EntityType} as {ExportType}", entityType, exportType);
            throw;
        }
    }

    public async Task<ImportResult> ImportDataAsync(
        Stream fileStream,
        string fileName,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);

            var contentType = GetContentTypeFromFileName(fileName);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync($"/api/import/{entityType}", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ImportResult>(json, JsonOptions);

            return result ?? new ImportResult { Success = false, Errors = new List<string> { "Unknown error" } };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing {EntityType} from {FileName}", entityType, fileName);
            return new ImportResult
            {
                Success = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private static string GetContentTypeForExport(string exportType)
    {
        return exportType.ToLowerInvariant() switch
        {
            "csv" => "text/csv",
            "excel" or "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "pdf" => "application/pdf",
            "json" => "application/json",
            _ => "application/octet-stream"
        };
    }

    private static string GetContentTypeFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".csv" => "text/csv",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".json" => "application/json",
            ".xml" => "application/xml",
            _ => "application/octet-stream"
        };
    }
}
