using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading.RateLimiting;

namespace NumbatWallet.Web.Api.Middleware;

/// <summary>
/// Custom distributed rate limiting using Redis
/// </summary>
public class DistributedRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DistributedRateLimitingMiddleware> _logger;

    public DistributedRateLimitingMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<DistributedRateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_configuration.GetValue<bool>("RateLimiting:Distributed:Enabled"))
        {
            await _next(context);
            return;
        }

        var key = GetRateLimitKey(context);
        var limit = GetRateLimit(context);
        var window = TimeSpan.FromMinutes(_configuration.GetValue<int>("RateLimiting:Distributed:WindowMinutes", 1));

        var currentCount = await GetCurrentCountAsync(key);

        if (currentCount >= limit)
        {
            _logger.LogWarning("Rate limit exceeded for key: {Key}", key);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Append("X-RateLimit-Limit", limit.ToString());
            context.Response.Headers.Append("X-RateLimit-Remaining", "0");
            context.Response.Headers.Append("X-RateLimit-Reset", GetResetTime(window).ToString());
            context.Response.Headers.Append("Retry-After", window.TotalSeconds.ToString());

            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        await IncrementCountAsync(key, window);

        context.Response.Headers.Append("X-RateLimit-Limit", limit.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining", (limit - currentCount - 1).ToString());
        context.Response.Headers.Append("X-RateLimit-Reset", GetResetTime(window).ToString());

        await _next(context);
    }

    private string GetRateLimitKey(HttpContext context)
    {
        var prefix = "rate_limit:";

        if (context.User.Identity?.IsAuthenticated == true)
        {
            return $"{prefix}user:{context.User.Identity.Name}";
        }

        var ip = GetClientIpAddress(context);
        return $"{prefix}ip:{ip}";
    }

    private int GetRateLimit(HttpContext context)
    {
        // Different limits based on user role
        if (context.User.IsInRole("Admin"))
        {
            return _configuration.GetValue<int>("RateLimiting:Distributed:Admin", 1000);
        }

        if (context.User.IsInRole("Officer"))
        {
            return _configuration.GetValue<int>("RateLimiting:Distributed:Officer", 500);
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            return _configuration.GetValue<int>("RateLimiting:Distributed:Authenticated", 100);
        }

        return _configuration.GetValue<int>("RateLimiting:Distributed:Anonymous", 20);
    }

    private async Task<int> GetCurrentCountAsync(string key)
    {
        var value = await _cache.GetStringAsync(key);
        return int.TryParse(value, out var count) ? count : 0;
    }

    private async Task IncrementCountAsync(string key, TimeSpan window)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = window
        };

        var currentCount = await GetCurrentCountAsync(key);
        await _cache.SetStringAsync(key, (currentCount + 1).ToString(), options);
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private long GetResetTime(TimeSpan window)
    {
        return DateTimeOffset.UtcNow.Add(window).ToUnixTimeSeconds();
    }
}

public static class DistributedRateLimitingExtensions
{
    public static IApplicationBuilder UseDistributedRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DistributedRateLimitingMiddleware>();
    }
}