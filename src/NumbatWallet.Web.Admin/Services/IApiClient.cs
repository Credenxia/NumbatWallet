using System.Net.Http;

namespace NumbatWallet.Web.Admin.Services;

public interface IApiClient
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<T?> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default);
    Task<T?> PutAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
    Task<PagedResult<T>> GetPagedAsync<T>(string endpoint, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class ApiException : Exception
{
    public int StatusCode { get; set; }
    public string? ResponseContent { get; set; }

    public ApiException(string message, int statusCode = 0, string? responseContent = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }

    public ApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}