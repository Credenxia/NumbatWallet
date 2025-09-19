using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading.RateLimiting;

namespace NumbatWallet.Web.Api.Middleware;

/// <summary>
/// Configure rate limiting for the API
/// </summary>
public static class RateLimitingMiddleware
{
    public static IServiceCollection AddCustomRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // Global rate limiter
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:Global:PermitLimit", 100),
                        Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:Global:WindowMinutes", 1))
                    }));

            // Default policy for authenticated users
            options.AddPolicy("authenticated", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? "anonymous",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:Authenticated:PermitLimit", 60),
                        Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:Authenticated:WindowMinutes", 1))
                    }));

            // Strict policy for anonymous users
            options.AddPolicy("anonymous", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context) ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:Anonymous:PermitLimit", 20),
                        Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:Anonymous:WindowMinutes", 1))
                    }));

            // Relaxed policy for admin operations
            options.AddPolicy("admin", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? "admin",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:Admin:PermitLimit", 200),
                        Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:Admin:WindowMinutes", 1))
                    }));

            // Sliding window policy for GraphQL
            options.AddPolicy("graphql", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? GetClientIpAddress(context) ?? "unknown",
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:GraphQL:PermitLimit", 30),
                        Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:GraphQL:WindowMinutes", 1)),
                        SegmentsPerWindow = configuration.GetValue<int>("RateLimiting:GraphQL:SegmentsPerWindow", 4)
                    }));

            // Token bucket policy for API endpoints
            options.AddPolicy("api", context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: partition => new TokenBucketRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        TokenLimit = configuration.GetValue<int>("RateLimiting:Api:TokenLimit", 100),
                        ReplenishmentPeriod = TimeSpan.FromSeconds(configuration.GetValue<int>("RateLimiting:Api:ReplenishmentPeriodSeconds", 30)),
                        TokensPerPeriod = configuration.GetValue<int>("RateLimiting:Api:TokensPerPeriod", 50)
                    }));

            // Concurrency limiter for expensive operations
            options.AddPolicy("expensive", context =>
                RateLimitPartition.GetConcurrencyLimiter(
                    partitionKey: "expensive-operations",
                    factory: partition => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = configuration.GetValue<int>("RateLimiting:Expensive:PermitLimit", 5),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = configuration.GetValue<int>("RateLimiting:Expensive:QueueLimit", 10)
                    }));

            // Configure rejection response
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                var problemDetails = new
                {
                    type = "https://numbatwallet.wa.gov.au/errors/rate-limit",
                    title = "Too Many Requests",
                    status = StatusCodes.Status429TooManyRequests,
                    detail = "Rate limit exceeded. Please retry later.",
                    instance = context.HttpContext.Request.Path,
                    retryAfter = GetRetryAfterSeconds(context.Lease)
                };

                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            };
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext context)
    {
        // Use user identity for authenticated users, IP for anonymous
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return context.User.Identity.Name ?? "authenticated";
        }

        return GetClientIpAddress(context) ?? "anonymous";
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check for proxied IP first
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }

        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            return realIp.FirstOrDefault();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static int GetRetryAfterSeconds(RateLimitLease lease)
    {
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            return (int)retryAfter.TotalSeconds;
        }

        return 60; // Default retry after 60 seconds
    }
}

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