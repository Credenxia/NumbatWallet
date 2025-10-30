using NumbatWallet.Web.Admin.Models;
using System.Text.Json;

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

    public async Task<List<WalletTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/wallet-templates", cancellationToken);
            response.EnsureSuccessStatusCode();

            var templates = await response.Content.ReadFromJsonAsync<List<WalletTemplateDto>>(s_jsonOptions, cancellationToken);
            return templates ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching wallet templates from API");
            return [];
        }
    }

    public async Task<List<WalletTemplateDto>> GetTemplatesByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/wallet-templates/tenant/{tenantId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var templates = await response.Content.ReadFromJsonAsync<List<WalletTemplateDto>>(s_jsonOptions, cancellationToken);
            return templates ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching wallet templates for tenant {TenantId} from API", tenantId);
            return [];
        }
    }

    public async Task<WalletTemplateDto?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/wallet-templates/{templateId}", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WalletTemplateDto>(s_jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching wallet template {TemplateId} from API", templateId);
            return null;
        }
    }

    public async Task<WalletTemplateDto> CreateTemplateAsync(WalletTemplateDto walletTemplate, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/wallet-templates", walletTemplate, s_jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<WalletTemplateDto>(s_jsonOptions, cancellationToken);
            return created ?? walletTemplate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating wallet template via API");
            throw;
        }
    }

    public async Task<WalletTemplateDto> UpdateTemplateAsync(WalletTemplateDto walletTemplate, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/v1/wallet-templates/{walletTemplate.Id}", walletTemplate, s_jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var updated = await response.Content.ReadFromJsonAsync<WalletTemplateDto>(s_jsonOptions, cancellationToken);
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
}
