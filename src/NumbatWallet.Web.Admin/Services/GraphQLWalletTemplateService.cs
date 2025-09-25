using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Web.Admin.Services;

/// <summary>
/// GraphQL-based implementation of IWalletTemplateService for Admin Portal
/// Communicates with the API via GraphQL/REST instead of direct database access
/// </summary>
public class GraphQLWalletTemplateService : IWalletTemplateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GraphQLWalletTemplateService> _logger;
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public GraphQLWalletTemplateService(
        IHttpClientFactory httpClientFactory,
        ILogger<GraphQLWalletTemplateService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
        _logger = logger;
    }

    public async Task<List<WalletTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/wallet-templates", cancellationToken);
            response.EnsureSuccessStatusCode();

            var templates = await response.Content.ReadFromJsonAsync<List<WalletTemplate>>(s_jsonOptions, cancellationToken);
            return templates ?? new List<WalletTemplate>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching wallet templates from API");
            return new List<WalletTemplate>();
        }
    }

    public async Task<List<WalletTemplate>> GetTemplatesByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/wallet-templates/tenant/{tenantId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var templates = await response.Content.ReadFromJsonAsync<List<WalletTemplate>>(s_jsonOptions, cancellationToken);
            return templates ?? new List<WalletTemplate>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching wallet templates for tenant {TenantId} from API", tenantId);
            return new List<WalletTemplate>();
        }
    }

    public async Task<WalletTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/wallet-templates/{templateId}", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WalletTemplate>(s_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching wallet template {TemplateId} from API", templateId);
            return null;
        }
    }

    public async Task<WalletTemplate> CreateTemplateAsync(WalletTemplate walletTemplate, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/wallet-templates", walletTemplate, s_jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<WalletTemplate>(s_jsonOptions, cancellationToken);
            return created ?? walletTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating wallet template via API");
            throw;
        }
    }

    public async Task<WalletTemplate> UpdateTemplateAsync(WalletTemplate walletTemplate, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/v1/wallet-templates/{walletTemplate.Id}", walletTemplate, s_jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var updated = await response.Content.ReadFromJsonAsync<WalletTemplate>(s_jsonOptions, cancellationToken);
            return updated ?? walletTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating wallet template {TemplateId} via API", walletTemplate.Id);
            throw;
        }
    }

    public async Task DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/wallet-templates/{templateId}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting wallet template {TemplateId} via API", templateId);
            throw;
        }
    }

    public async Task<WalletTemplate> CloneTemplateAsync(Guid templateId, string newName, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { templateId, newName };
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/wallet-templates/{templateId}/clone", request, s_jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var cloned = await response.Content.ReadFromJsonAsync<WalletTemplate>(s_jsonOptions, cancellationToken);
            if (cloned == null)
            {
                throw new InvalidOperationException("Failed to clone template");
            }
            return cloned;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning wallet template {TemplateId} via API", templateId);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> MapCredentialToTemplate(Guid templateId, Dictionary<string, object> credentialData, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { templateId, credentialData };
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/wallet-templates/{templateId}/map", request, s_jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var mapped = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(s_jsonOptions, cancellationToken);
            return mapped ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping credential to template {TemplateId} via API", templateId);
            return new Dictionary<string, object>();
        }
    }

    public async Task<bool> ValidateCredentialAgainstTemplate(Guid templateId, Dictionary<string, object> credentialData, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { templateId, credentialData };
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/wallet-templates/{templateId}/validate", request, s_jsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<ValidationResult>(s_jsonOptions, cancellationToken);
            return result?.IsValid ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credential against template {TemplateId} via API", templateId);
            return false;
        }
    }

    private record ValidationResult(bool IsValid, string[] Errors);
}