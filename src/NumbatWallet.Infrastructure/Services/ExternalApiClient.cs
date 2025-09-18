using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NumbatWallet.Infrastructure.Services;

public class ExternalApiClient : IExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExternalApiClient(
        HttpClient httpClient,
        ILogger<ExternalApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task<TResponse?> GetAsync<TResponse>(
        string endpoint,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            AddHeaders(request, headers);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling GET {Endpoint}", endpoint);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling GET {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GET {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            AddHeaders(httpRequest, headers);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling POST {Endpoint}", endpoint);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling POST {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling POST {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, endpoint)
            {
                Content = content
            };
            AddHeaders(httpRequest, headers);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling PUT {Endpoint}", endpoint);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling PUT {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PUT {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        string endpoint,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            AddHeaders(request, headers);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling DELETE {Endpoint}", endpoint);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling DELETE {Endpoint}", endpoint);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling DELETE {Endpoint}", endpoint);
            return false;
        }
    }

    private void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
    {
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }
}

// Specialized client for identity verification APIs
public class IdentityVerificationApiClient : IIdentityVerificationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityVerificationApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public IdentityVerificationApiClient(
        HttpClient httpClient,
        ILogger<IdentityVerificationApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<IdentityVerificationResult> VerifyIdentityAsync(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string documentNumber,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                firstName,
                lastName,
                dateOfBirth = dateOfBirth.ToString("yyyy-MM-dd"),
                documentNumber,
                documentType
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync("/api/verify/identity", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<IdentityVerificationResult>(responseJson, _jsonOptions);

            _logger.LogInformation("Identity verification completed for {FirstName} {LastName}: {Result}",
                firstName, lastName, result?.IsVerified);

            return result ?? new IdentityVerificationResult { IsVerified = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying identity for {FirstName} {LastName}", firstName, lastName);
            return new IdentityVerificationResult { IsVerified = false, Error = ex.Message };
        }
    }

    public async Task<DocumentVerificationResult> VerifyDocumentAsync(
        byte[] documentImage,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(documentImage), "document", "document.jpg");
            content.Add(new StringContent(documentType), "documentType");

            using var response = await _httpClient.PostAsync("/api/verify/document", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<DocumentVerificationResult>(responseJson, _jsonOptions);

            _logger.LogInformation("Document verification completed for type {DocumentType}: {Result}",
                documentType, result?.IsValid);

            return result ?? new DocumentVerificationResult { IsValid = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying document of type {DocumentType}", documentType);
            return new DocumentVerificationResult { IsValid = false, Error = ex.Message };
        }
    }
}