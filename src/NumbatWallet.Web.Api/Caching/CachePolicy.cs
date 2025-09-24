using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace NumbatWallet.Web.Api.Caching;

/// <summary>
/// Cache policy for general API responses
/// </summary>
public sealed class ApiCachePolicy : IOutputCachePolicy
{
    public static readonly ApiCachePolicy Instance = new();

    private ApiCachePolicy() { }

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var attemptOutputCaching = AttemptOutputCaching(context);
        context.EnableOutputCaching = true;
        context.AllowCacheLookup = attemptOutputCaching;
        context.AllowCacheStorage = attemptOutputCaching;
        context.AllowLocking = true;

        // Vary by query
        context.CacheVaryByRules.QueryKeys = "*";

        // Vary by user
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.CacheVaryByRules.VaryByValues.Add("user", context.HttpContext.User.Identity.Name ?? "anonymous");
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var response = context.HttpContext.Response;

        // Don't cache non-success status codes
        if (response.StatusCode != StatusCodes.Status200OK)
        {
            context.AllowCacheStorage = false;
        }

        return ValueTask.CompletedTask;
    }

    private static bool AttemptOutputCaching(OutputCacheContext context)
    {
        var request = context.HttpContext.Request;

        // Only cache GET and HEAD requests
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            return false;
        }

        // Don't cache if authorization header is present (except bearer tokens)
        if (request.Headers.Authorization.Count > 0)
        {
            var authHeader = request.Headers.Authorization.ToString();
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Cache policy for credential-related endpoints
/// </summary>
public sealed class CredentialCachePolicy : IOutputCachePolicy
{
    public static readonly CredentialCachePolicy Instance = new();

    private CredentialCachePolicy() { }

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var attemptOutputCaching = AttemptOutputCaching(context);
        context.EnableOutputCaching = true;
        context.AllowCacheLookup = attemptOutputCaching;
        context.AllowCacheStorage = attemptOutputCaching;
        context.AllowLocking = true;

        // Short cache duration for credentials (30 seconds)
        context.ResponseExpirationTimeSpan = TimeSpan.FromSeconds(30);

        // Vary by query and user
        context.CacheVaryByRules.QueryKeys = "*";

        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.CacheVaryByRules.VaryByValues.Add("user", context.HttpContext.User.Identity.Name ?? "anonymous");
        }

        // Vary by wallet ID if present in route
        if (context.HttpContext.Request.RouteValues.TryGetValue("walletId", out var walletId))
        {
            context.CacheVaryByRules.VaryByValues.Add("wallet", walletId?.ToString() ?? "none");
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var response = context.HttpContext.Response;

        // Don't cache non-success status codes or sensitive operations
        if (response.StatusCode != StatusCodes.Status200OK)
        {
            context.AllowCacheStorage = false;
        }

        // Add cache headers
        response.Headers.CacheControl = "private, max-age=30";

        return ValueTask.CompletedTask;
    }

    private static bool AttemptOutputCaching(OutputCacheContext context)
    {
        var request = context.HttpContext.Request;

        // Only cache GET requests for credentials
        if (!HttpMethods.IsGet(request.Method))
        {
            return false;
        }

        // Don't cache verification endpoints
        if (request.Path.Value?.Contains("/verify", StringComparison.OrdinalIgnoreCase) == true)
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// No-cache policy for sensitive endpoints
/// </summary>
public sealed class NoCachePolicy : IOutputCachePolicy
{
    public static readonly NoCachePolicy Instance = new();

    private NoCachePolicy() { }

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        context.EnableOutputCaching = false;
        context.AllowCacheLookup = false;
        context.AllowCacheStorage = false;
        context.AllowLocking = false;

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var response = context.HttpContext.Response;

        // Ensure no caching
        response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        response.Headers.Pragma = "no-cache";
        response.Headers.Expires = "0";

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Cache tags for invalidation
/// </summary>
public static class CacheTags
{
    public const string Credentials = "credentials";
    public const string Wallets = "wallets";
    public const string Issuances = "issuances";
    public const string Organizations = "organizations";
    public const string Users = "users";
    public const string Health = "health";

    public static string ForWallet(Guid walletId) => $"wallet:{walletId}";
    public static string ForCredential(string credentialId) => $"credential:{credentialId}";
    public static string ForUser(string userId) => $"user:{userId}";
    public static string ForOrganization(Guid organizationId) => $"org:{organizationId}";
}