using Microsoft.AspNetCore.Authorization;
using NumbatWallet.Web.Api.Authorization.Requirements;

namespace NumbatWallet.Web.Api.Authorization.Handlers;

public class TenantAccessHandler : AuthorizationHandler<TenantAccessRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantAccessHandler> _logger;

    public TenantAccessHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<TenantAccessHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantAccessRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("No HTTP context available for tenant authorization");
            return Task.CompletedTask;
        }

        // Allow admins to bypass tenant checks if configured
        if (requirement.AllowAdminBypass && context.User.IsInRole("Admin"))
        {
            _logger.LogDebug("Admin bypassed tenant access check");
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Get tenant from user claims
        var userTenantClaim = context.User.FindFirst("tenant_id");
        if (userTenantClaim == null)
        {
            _logger.LogWarning("User {UserId} has no tenant_id claim",
                context.User.FindFirst("sub")?.Value ?? "unknown");
            return Task.CompletedTask;
        }

        // Get requested tenant from route, query, or header
        string? requestedTenant = null;

        // Check route data
        if (httpContext.Request.RouteValues.TryGetValue("tenantId", out var routeTenant))
        {
            requestedTenant = routeTenant?.ToString();
        }
        // Check query string
        else if (httpContext.Request.Query.TryGetValue("tenantId", out var queryTenant))
        {
            requestedTenant = queryTenant.FirstOrDefault();
        }
        // Check header
        else if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenant))
        {
            requestedTenant = headerTenant.FirstOrDefault();
        }
        // Check items set by middleware
        else if (httpContext.Items.TryGetValue("TenantId", out var itemTenant))
        {
            requestedTenant = itemTenant?.ToString();
        }

        // If no tenant requested, use user's tenant
        if (string.IsNullOrEmpty(requestedTenant))
        {
            requestedTenant = userTenantClaim.Value;
        }

        // Verify tenant match
        if (userTenantClaim.Value.Equals(requestedTenant, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("User {UserId} authorized for tenant {TenantId}",
                context.User.FindFirst("sub")?.Value ?? "unknown", requestedTenant);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("User {UserId} from tenant {UserTenant} denied access to tenant {RequestedTenant}",
                context.User.FindFirst("sub")?.Value ?? "unknown",
                userTenantClaim.Value,
                requestedTenant);
        }

        return Task.CompletedTask;
    }
}