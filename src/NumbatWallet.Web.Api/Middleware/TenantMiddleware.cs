using System.Security.Claims;

namespace NumbatWallet.Web.Api.Middleware;

/// <summary>
/// Middleware to extract and validate tenant information from requests
/// </summary>
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
        var tenantId = ResolveTenantId(context);

        if (!string.IsNullOrEmpty(tenantId))
        {
            // Validate tenant exists and is active
            var tenant = await tenantService.GetTenantAsync(tenantId);
            if (tenant == null)
            {
                _logger.LogWarning("Invalid tenant ID: {TenantId}", tenantId);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid tenant");
                return;
            }

            if (!tenant.IsActive)
            {
                _logger.LogWarning("Inactive tenant accessed: {TenantId}", tenantId);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Tenant is inactive");
                return;
            }

            // Set tenant context
            context.Items["TenantId"] = tenantId;
            context.Items["Tenant"] = tenant;

            // Add tenant claim if authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var claims = new List<Claim>(context.User.Claims)
                {
                    new Claim("tenant_id", tenantId)
                };
                var identity = new ClaimsIdentity(claims, context.User.Identity.AuthenticationType);
                context.User = new ClaimsPrincipal(identity);
            }

            _logger.LogDebug("Tenant context set: {TenantId}", tenantId);
        }

        await _next(context);
    }

    private string? ResolveTenantId(HttpContext context)
    {
        // Priority order for tenant resolution:
        // 1. From JWT claim
        // 2. From header
        // 3. From subdomain
        // 4. From query string

        // 1. Check JWT claim
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(tenantClaim))
            {
                return tenantClaim;
            }
        }

        // 2. Check custom header
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader))
        {
            return tenantHeader.FirstOrDefault();
        }

        // 3. Check subdomain (e.g., tenant1.api.numbatwallet.com)
        var host = context.Request.Host.Host;
        if (!string.IsNullOrEmpty(host))
        {
            var parts = host.Split('.');
            if (parts.Length > 2 && !parts[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            {
                return parts[0].ToLowerInvariant();
            }
        }

        // 4. Check query string
        if (context.Request.Query.TryGetValue("tenant", out var tenantQuery))
        {
            return tenantQuery.FirstOrDefault();
        }

        return null;
    }
}

public interface ITenantService
{
    Task<TenantInfo?> GetTenantAsync(string tenantId);
    Task<bool> ValidateTenantAsync(string tenantId);
}

public class TenantInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public Dictionary<string, string> Settings { get; set; } = new();
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}