using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NumbatWallet.Web.Admin.Models;

namespace NumbatWallet.Web.Admin.Services;

/// <summary>
/// GraphQL-based implementation of ITenantService that communicates with the API
/// instead of directly accessing the database
/// </summary>
public class GraphQLTenantService : ITenantService
{
    private readonly IApiClient _apiClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GraphQLTenantService> _logger;

    public GraphQLTenantService(
        IApiClient apiClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GraphQLTenantService> logger)
    {
        _apiClient = apiClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<TenantInfo?> GetCurrentTenantAsync()
    {
        try
        {
            // Get tenant from current user's claims
            var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                // If no tenant claim, get from header or query string (for multi-tenant scenarios)
                tenantId = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault()
                    ?? _httpContextAccessor.HttpContext?.Request.Query["tenantId"].FirstOrDefault();
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("No tenant ID found in request");
                return null;
            }

            return await GetTenantByIdAsync(tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current tenant");
            return null;
        }
    }

    public async Task<TenantInfo?> GetTenantByIdAsync(string tenantId)
    {
        try
        {
            // TODO: Replace with GraphQL query once Strawberry Shake is configured
            // For now, use REST API
            var tenant = await _apiClient.GetAsync<TenantInfo>($"/api/admin/tenants/{tenantId}");
            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<List<TenantInfo>> GetAllTenantsAsync()
    {
        try
        {
            // TODO: Replace with GraphQL query once Strawberry Shake is configured
            // For now, use REST API
            var tenants = await _apiClient.GetAsync<List<TenantInfo>>("/api/admin/tenants");
            return tenants ?? new List<TenantInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tenants");
            return new List<TenantInfo>();
        }
    }

    public async Task<bool> UpdateTenantSettingsAsync(string tenantId, TenantSettings settings)
    {
        try
        {
            // TODO: Replace with GraphQL mutation once Strawberry Shake is configured
            // For now, use REST API
            var result = await _apiClient.PutAsync<object>($"/api/admin/tenants/{tenantId}/settings", settings);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant settings for {TenantId}", tenantId);
            return false;
        }
    }
}
