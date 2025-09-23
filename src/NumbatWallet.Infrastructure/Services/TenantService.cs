using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Exceptions;
using NumbatWallet.SharedKernel.Exceptions;

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

        return await GetTenantByIdAsync(tenantId, CancellationToken.None);
    }

    public async Task<TenantDto?> GetTenantByIdAsync(string tenantId, CancellationToken cancellationToken = default)
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
        var tenant = await GetTenantByIdAsync(tenantId, CancellationToken.None);
        return tenant != null && tenant.IsActive;
    }

    public async Task<IEnumerable<TenantDto>> GetAllTenantsAsync()
    {
        var tenants = new List<TenantDto>();
        var tenantsSection = _configuration.GetSection("Tenants");

        foreach (var tenantSection in tenantsSection.GetChildren())
        {
            var tenantId = tenantSection.Key;
            var tenant = await GetTenantByIdAsync(tenantId, CancellationToken.None);
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

    public async Task<IEnumerable<TenantDto>> GetAllTenants(CancellationToken cancellationToken = default)
    {
        return await GetAllTenantsAsync();
    }

    public async Task<TenantDto> CreateTenantAsync(CreateTenantDto dto, CancellationToken cancellationToken = default)
    {
        // In production, this would create tenant in database
        // For now, return a mock tenant
        var tenant = new TenantDto
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Identifier = dto.Identifier,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Created tenant {TenantId} with name {TenantName}", tenant.TenantId, tenant.Name);
        return tenant;
    }

    public async Task<TenantDto> UpdateTenantAsync(string tenantId, UpdateTenantDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await GetTenantByIdAsync(tenantId, cancellationToken);
        if (existing == null)
        {
            throw new EntityNotFoundException("Tenant", tenantId);
        }

        // In production, this would update tenant in database
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }
        if (dto.Description != null)
        {
            existing.Description = dto.Description;
        }
        if (dto.IsActive.HasValue)
        {
            existing.IsActive = dto.IsActive.Value;
        }

        _cache.Remove($"tenant_{tenantId}");
        _logger.LogInformation("Updated tenant {TenantId}", tenantId);
        return existing;
    }

    public async Task<bool> DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var existing = await GetTenantByIdAsync(tenantId, cancellationToken);
        if (existing == null)
        {
            return false;
        }

        // In production, this would delete tenant from database
        _cache.Remove($"tenant_{tenantId}");
        _logger.LogWarning("Deleted tenant {TenantId}", tenantId);
        return true;
    }

    public async Task<bool> ActivateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return false;
        }

        tenant.IsActive = true;
        _cache.Remove($"tenant_{tenantId}");
        _logger.LogInformation("Activated tenant {TenantId}", tenantId);
        return true;
    }

    public async Task<bool> DeactivateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return false;
        }

        tenant.IsActive = false;
        _cache.Remove($"tenant_{tenantId}");
        _logger.LogInformation("Deactivated tenant {TenantId}", tenantId);
        return true;
    }
}
