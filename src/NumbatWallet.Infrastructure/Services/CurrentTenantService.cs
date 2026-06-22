using Microsoft.AspNetCore.Http;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _tenantId;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? TenantId
    {
        get
        {
            if (!string.IsNullOrEmpty(_tenantId))
            {
                return _tenantId;
            }

            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return null;
            }

            // SECURITY: the tenant is resolved from the VALIDATED identity claim only.
            // X-Tenant-Id header and path segments are attacker-controlled and must never
            // determine the tenant for an authenticated request, or a caller could read/write
            // another tenant's data (cross-tenant isolation breach).
            var tenantClaim = context.User?.FindFirst("tenant_id")
                ?? context.User?.FindFirst("TenantId");
            if (tenantClaim != null && !string.IsNullOrEmpty(tenantClaim.Value))
            {
                _tenantId = tenantClaim.Value;
                return _tenantId;
            }

            // Development-only convenience: allow an explicit X-Tenant-Id header ONLY when the
            // caller is unauthenticated (local tooling / tests). Never honoured in production.
            var isDevelopment = string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Development", StringComparison.OrdinalIgnoreCase);
            var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
            if (isDevelopment && !isAuthenticated &&
                context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
            {
                _tenantId = headerTenantId.ToString();
                return _tenantId;
            }

            return null;
        }
    }

    public string? TenantName
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var tenantNameClaim = context.User?.FindFirst("tenant_name")
                    ?? context.User?.FindFirst("TenantName");
                if (tenantNameClaim != null)
                {
                    return tenantNameClaim.Value;
                }
            }

            return null;
        }
    }

    public bool IsMultiTenantContext => !string.IsNullOrEmpty(TenantId);

    public Task<bool> SetTenantAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));
        }

        _tenantId = tenantId;
        return Task.FromResult(true);
    }
}
