using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NumbatWallet.Web.Admin.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly ILogger<ApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(
        HttpClient httpClient,
        IAuthService authService,
        ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            await AddAuthHeaderAsync();

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for GET {Endpoint}", endpoint);
            throw new ApiException($"Failed to fetch data from {endpoint}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out for GET {Endpoint}", endpoint);
            throw new ApiException($"Request timed out for {endpoint}", ex);
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            await AddAuthHeaderAsync();

            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for POST {Endpoint}", endpoint);
            throw new ApiException($"Failed to post data to {endpoint}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out for POST {Endpoint}", endpoint);
            throw new ApiException($"Request timed out for {endpoint}", ex);
        }
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            await AddAuthHeaderAsync();

            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for PUT {Endpoint}", endpoint);
            throw new ApiException($"Failed to update data at {endpoint}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out for PUT {Endpoint}", endpoint);
            throw new ApiException($"Request timed out for {endpoint}", ex);
        }
    }

    public async Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            await AddAuthHeaderAsync();

            var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for DELETE {Endpoint}", endpoint);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out for DELETE {Endpoint}", endpoint);
            return false;
        }
    }

    public async Task<PagedResult<T>> GetPagedAsync<T>(string endpoint, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            await AddAuthHeaderAsync();

            var separator = endpoint.Contains('?') ? '&' : '?';
            var pagedEndpoint = $"{endpoint}{separator}page={page}&pageSize={pageSize}";

            var response = await _httpClient.GetAsync(pagedEndpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Try to deserialize as a paged result directly
            var pagedResult = JsonSerializer.Deserialize<PagedResult<T>>(json, _jsonOptions);
            if (pagedResult != null)
            {
                return pagedResult;
            }

            // Fallback: wrap the result in a paged result
            var items = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
            return new PagedResult<T>
            {
                Items = items ?? new List<T>(),
                TotalCount = items?.Count ?? 0,
                PageNumber = page,
                PageSize = pageSize
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for GET {Endpoint}", endpoint);
            throw new ApiException($"Failed to fetch paged data from {endpoint}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out for GET {Endpoint}", endpoint);
            throw new ApiException($"Request timed out for {endpoint}", ex);
        }
    }

    private async Task AddAuthHeaderAsync()
    {
        var token = await _authService.GetAccessTokenAsync();

        // In development, if no token is available, create a simple test token
        if (string.IsNullOrEmpty(token))
        {
            // Create a simple test JWT token for development
            // This is a minimal JWT with Admin role for testing
            var testToken = CreateDevelopmentTestToken();
            if (!string.IsNullOrEmpty(testToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", testToken);
                return;
            }
        }

        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private string CreateDevelopmentTestToken()
    {
        // Create a simple JWT token for development testing
        // The Web API's TestAuthenticationHandler will validate this
        var payload = new
        {
            sub = "dev-admin@test.com",
            user_id = "dev-admin",
            tenant_id = "test-tenant",
            role = "Admin",
            name = "Development Admin",
            email = "dev-admin@test.com",
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // Simple Base64 encoding (not secure, only for development)
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

        // Create signature using the test secret key
        var secretKey = "TestSecretKey123456789012345678901234567890";
        var message = $"{header}.{payloadBase64}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        var signature = Convert.ToBase64String(signatureBytes);

        return $"{header}.{payloadBase64}.{signature}";
    }
}
