using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Exceptions;

namespace NumbatWallet.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TenantService> _logger;
    private static readonly AsyncLocal<string?> _currentTenantId = new();

    public TenantService(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<TenantService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TenantDto?> GetCurrentTenantAsync()
    {
        var tenantId = GetCurrentTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            return null;
        }

        return await GetTenantByIdAsync(tenantId);
    }

    public async Task<TenantDto?> GetTenantByIdAsync(string tenantId)
    {
        return await _cache.GetOrCreateAsync($"tenant_{tenantId}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);

            // In production, this would query a database
            // For now, using configuration-based tenants
            var tenantConfig = _configuration.GetSection($"Tenants:{tenantId}");
            if (!tenantConfig.Exists())
            {
                return null;
            }

            return new TenantDto
            {
                Id = tenantId,
                Name = tenantConfig["Name"] ?? tenantId,
                Identifier = tenantConfig["Identifier"] ?? tenantId,
                IsActive = tenantConfig.GetValue("IsActive", true),
                ConnectionString = tenantConfig["ConnectionString"] ?? GetDefaultConnectionString(tenantId),
                Settings = tenantConfig.GetSection("Settings").Get<Dictionary<string, string>>() ?? new(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };
        });
    }

    public async Task SetCurrentTenantAsync(string tenantId)
    {
        var tenant = await GetTenantByIdAsync(tenantId);
        if (tenant == null)
        {
            throw new TenantNotFoundException(tenantId);
        }

        if (!tenant.IsActive)
        {
            throw new UnauthorizedException($"Tenant {tenantId} is not active");
        }

        _currentTenantId.Value = tenantId;

        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Items["TenantId"] = tenantId;
            _httpContextAccessor.HttpContext.Items["Tenant"] = tenant;
        }

        _logger.LogDebug("Tenant context set to: {TenantId}", tenantId);
    }

    public Task ClearCurrentTenantAsync()
    {
        _currentTenantId.Value = null;

        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Items.Remove("TenantId");
            _httpContextAccessor.HttpContext.Items.Remove("Tenant");
        }

        _logger.LogDebug("Tenant context cleared");
        return Task.CompletedTask;
    }

    public async Task<bool> ValidateTenantAsync(string tenantId)
    {
        var tenant = await GetTenantByIdAsync(tenantId);
        return tenant != null && tenant.IsActive;
    }

    public async Task<IEnumerable<TenantDto>> GetAllTenantsAsync()
    {
        var tenants = new List<TenantDto>();
        var tenantsSection = _configuration.GetSection("Tenants");

        foreach (var tenantSection in tenantsSection.GetChildren())
        {
            var tenantId = tenantSection.Key;
            var tenant = await GetTenantByIdAsync(tenantId);
            if (tenant != null)
            {
                tenants.Add(tenant);
            }
        }

        return tenants;
    }

    public string? GetCurrentTenantId()
    {
        // Priority 1: Check AsyncLocal storage
        if (!string.IsNullOrEmpty(_currentTenantId.Value))
        {
            return _currentTenantId.Value;
        }

        // Priority 2: Check HttpContext
        if (_httpContextAccessor.HttpContext != null)
        {
            if (_httpContextAccessor.HttpContext.Items.TryGetValue("TenantId", out var tenantId))
            {
                return tenantId?.ToString();
            }

            // Check user claims
            var tenantClaim = _httpContextAccessor.HttpContext.User?.FindFirst("tenant_id");
            if (tenantClaim != null)
            {
                return tenantClaim.Value;
            }
        }

        return null;
    }

    private string GetDefaultConnectionString(string tenantId)
    {
        var baseConnection = _configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=numbatwallet;Username=postgres;Password=postgres";

        // Replace database name with tenant-specific database
        return baseConnection.Replace("numbatwallet", $"numbatwallet_{tenantId.ToLowerInvariant()}");
    }
}