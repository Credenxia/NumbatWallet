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
                return _tenantId;

            // Try to get from HTTP context (header, claim, or route)
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                // Check header first
                if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
                {
                    _tenantId = headerTenantId.ToString();
                    return _tenantId;
                }

                // Check user claims
                var tenantClaim = context.User?.FindFirst("tenant_id")
                    ?? context.User?.FindFirst("TenantId");
                if (tenantClaim != null)
                {
                    _tenantId = tenantClaim.Value;
                    return _tenantId;
                }

                // Check route data
                if (context.Request.RouteValues.TryGetValue("tenantId", out var routeTenantId))
                {
                    _tenantId = routeTenantId?.ToString();
                    return _tenantId;
                }
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