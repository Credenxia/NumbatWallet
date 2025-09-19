using System.Security.Claims;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Exceptions;

namespace NumbatWallet.Web.Api.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        try
        {
            var tenantId = ResolveTenantId(context);

            if (!string.IsNullOrEmpty(tenantId))
            {
                await tenantService.SetCurrentTenantAsync(tenantId);
                _logger.LogDebug("Tenant context set for tenant: {TenantId}", tenantId);

                context.Items["TenantId"] = tenantId;
                context.Response.Headers.Append("X-Tenant-Id", tenantId);
            }
            else if (RequiresTenant(context))
            {
                _logger.LogWarning("Tenant context required but not provided for path: {Path}", context.Request.Path);
                throw new UnauthorizedException("Tenant context is required");
            }

            await _next(context);
        }
        catch (TenantNotFoundException ex)
        {
            _logger.LogError(ex, "Tenant not found");
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not found" });
        }
        finally
        {
            if (tenantService != null)
            {
                await tenantService.ClearCurrentTenantAsync();
            }
        }
    }

    private string? ResolveTenantId(HttpContext context)
    {
        // Priority 1: Check for tenant claim in authenticated user
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id")
                ?? context.User.FindFirst("tid")
                ?? context.User.FindFirst(ClaimTypes.GroupSid);

            if (tenantClaim != null)
            {
                return tenantClaim.Value;
            }
        }

        // Priority 2: Check for X-Tenant-Id header
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
        {
            return headerTenantId.ToString();
        }

        // Priority 3: Check for subdomain (e.g., tenant1.api.numbatwallet.gov.au)
        var host = context.Request.Host.Host;
        if (!string.IsNullOrEmpty(host))
        {
            var segments = host.Split('.');
            if (segments.Length > 2 && !segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            {
                return segments[0];
            }
        }

        // Priority 4: Check route parameter (for REST endpoints)
        if (context.Request.RouteValues.TryGetValue("tenantId", out var routeTenantId))
        {
            return routeTenantId?.ToString();
        }

        // Priority 5: Check query parameter (fallback for testing)
        if (context.Request.Query.TryGetValue("tenant", out var queryTenantId))
        {
            return queryTenantId.ToString();
        }

        return null;
    }

    private bool RequiresTenant(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Endpoints that don't require tenant context
        var tenantFreeEndpoints = new[]
        {
            "/health",
            "/metrics",
            "/swagger",
            "/.well-known",
            "/api/v1/auth/login",
            "/api/v1/auth/register",
            "/api/v1/tenants/discover"
        };

        return !tenantFreeEndpoints.Any(endpoint => path.Contains(endpoint, StringComparison.OrdinalIgnoreCase));
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}