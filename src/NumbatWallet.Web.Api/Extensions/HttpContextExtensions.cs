using System.Security.Claims;

namespace NumbatWallet.Web.Api.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetTenantId(this HttpContext context)
    {
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value
            ?? context.User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            throw new InvalidOperationException("TenantId not found in user claims");
        }

        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            throw new InvalidOperationException($"Invalid TenantId format: {tenantIdClaim}");
        }

        return tenantId;
    }

    public static Guid? GetTenantIdOrDefault(this HttpContext context)
    {
        try
        {
            return context.GetTenantId();
        }
        catch
        {
            return null;
        }
    }

    public static string GetUserId(this HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("UserId not found in user claims");
        }

        return userId;
    }
}