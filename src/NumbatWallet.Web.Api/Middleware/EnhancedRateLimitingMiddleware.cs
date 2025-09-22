using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;

namespace NumbatWallet.Web.Api.Middleware;

/// <summary>
/// Enhanced rate limiting middleware with security features
/// POA: High security implementation for API protection
/// </summary>
public class EnhancedRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EnhancedRateLimitingMiddleware> _logger;
    private readonly IDistributedCache _cache;
    private readonly RateLimitingOptions _options;

    public EnhancedRateLimitingMiddleware(
        RequestDelegate next,
        ILogger<EnhancedRateLimitingMiddleware> logger,
        IDistributedCache cache,
        IOptions<RateLimitingOptions> options)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health checks
        if (IsHealthCheckEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var endpoint = GetEndpointIdentifier(context);
        var rateLimitKey = $"ratelimit:{clientId}:{endpoint}";

        // Check if client is blocked
        if (await IsClientBlockedAsync(clientId))
        {
            await HandleBlockedClient(context, clientId);
            return;
        }

        // Apply rate limiting
        var allowed = await CheckRateLimitAsync(rateLimitKey, endpoint);

        if (!allowed)
        {
            await HandleRateLimitExceeded(context, clientId, endpoint);
            return;
        }

        // Track request
        await TrackRequestAsync(clientId, endpoint);

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Priority: API Key > JWT Subject > IP Address
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            return HashIdentifier($"apikey:{apiKey}");
        }

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value ?? context.User.Identity.Name;
            if (!string.IsNullOrEmpty(userId))
            {
                return HashIdentifier($"user:{userId}");
            }
        }

        // Fallback to IP address
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return HashIdentifier($"ip:{ip}");
    }

    private string GetEndpointIdentifier(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";

        // Normalize path by removing IDs and query strings
        path = NormalizePath(path);

        return $"{method}:{path}";
    }

    private string NormalizePath(string path)
    {
        // Remove GUIDs
        path = System.Text.RegularExpressions.Regex.Replace(
            path,
            @"[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}",
            "{id}",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove numeric IDs
        path = System.Text.RegularExpressions.Regex.Replace(
            path,
            @"\/\d+",
            "/{id}");

        return path;
    }

    private string HashIdentifier(string identifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(identifier));
        return Convert.ToBase64String(bytes);
    }

    private async Task<bool> CheckRateLimitAsync(string key, string endpoint)
    {
        var limits = GetLimitsForEndpoint(endpoint);
        var now = DateTimeOffset.UtcNow;

        // Check multiple time windows
        foreach (var limit in limits)
        {
            var windowKey = $"{key}:{limit.Window.TotalSeconds}";
            var count = await GetRequestCountAsync(windowKey, limit.Window);

            if (count >= limit.Requests)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for key {Key} - {Count}/{Limit} requests in {Window}",
                    key, count, limit.Requests, limit.Window);

                return false;
            }

            await IncrementRequestCountAsync(windowKey, limit.Window);
        }

        return true;
    }

    private async Task<int> GetRequestCountAsync(string key, TimeSpan window)
    {
        var value = await _cache.GetStringAsync(key);
        if (int.TryParse(value, out var count))
        {
            return count;
        }
        return 0;
    }

    private async Task IncrementRequestCountAsync(string key, TimeSpan window)
    {
        var value = await _cache.GetStringAsync(key);
        var count = 0;

        if (int.TryParse(value, out count))
        {
            count++;
        }
        else
        {
            count = 1;
        }

        await _cache.SetStringAsync(
            key,
            count.ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = window
            });
    }

    private List<RateLimit> GetLimitsForEndpoint(string endpoint)
    {
        // Check for endpoint-specific limits
        if (_options.EndpointLimits.TryGetValue(endpoint, out var specificLimits))
        {
            return specificLimits;
        }

        // Check for pattern-based limits
        foreach (var pattern in _options.PatternLimits)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(endpoint, pattern.Key))
            {
                return pattern.Value;
            }
        }

        // Return default limits
        return _options.DefaultLimits;
    }

    private async Task<bool> IsClientBlockedAsync(string clientId)
    {
        var blockKey = $"blocked:{clientId}";
        var blocked = await _cache.GetStringAsync(blockKey);
        return !string.IsNullOrEmpty(blocked);
    }

    private async Task BlockClientAsync(string clientId, TimeSpan duration, string reason)
    {
        var blockKey = $"blocked:{clientId}";
        var blockData = new
        {
            Reason = reason,
            BlockedAt = DateTime.UtcNow,
            Duration = duration
        };

        await _cache.SetStringAsync(
            blockKey,
            System.Text.Json.JsonSerializer.Serialize(blockData),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration
            });

        _logger.LogWarning(
            "Client {ClientId} blocked for {Duration} - Reason: {Reason}",
            clientId, duration, reason);
    }

    private async Task HandleRateLimitExceeded(HttpContext context, string clientId, string endpoint)
    {
        // Track violations
        var violationKey = $"violations:{clientId}";
        var violations = await IncrementViolationsAsync(violationKey);

        // Auto-block after repeated violations
        if (violations >= _options.MaxViolationsBeforeBlock)
        {
            await BlockClientAsync(
                clientId,
                _options.BlockDuration,
                $"Exceeded rate limit {violations} times");
        }

        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.Headers.Add("X-RateLimit-Limit", _options.DefaultLimits[0].Requests.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", "0");
        context.Response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString());
        context.Response.Headers.Add("Retry-After", "60");

        await context.Response.WriteAsJsonAsync(new
        {
            error = "Rate limit exceeded",
            message = "Too many requests. Please retry after some time.",
            retryAfter = 60
        });

        _logger.LogWarning(
            "Rate limit exceeded for client {ClientId} on endpoint {Endpoint} - Violations: {Violations}",
            clientId, endpoint, violations);
    }

    private async Task HandleBlockedClient(HttpContext context, string clientId)
    {
        context.Response.StatusCode = 403; // Forbidden

        await context.Response.WriteAsJsonAsync(new
        {
            error = "Access blocked",
            message = "Your access has been temporarily blocked due to repeated violations."
        });

        _logger.LogWarning("Blocked client {ClientId} attempted access", clientId);
    }

    private async Task<int> IncrementViolationsAsync(string key)
    {
        var value = await _cache.GetStringAsync(key);
        var violations = 0;

        if (int.TryParse(value, out violations))
        {
            violations++;
        }
        else
        {
            violations = 1;
        }

        await _cache.SetStringAsync(
            key,
            violations.ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

        return violations;
    }

    private async Task TrackRequestAsync(string clientId, string endpoint)
    {
        // Track request for analytics
        var analyticsKey = $"analytics:{DateTime.UtcNow:yyyy-MM-dd:HH}:{endpoint}";
        await IncrementRequestCountAsync(analyticsKey, TimeSpan.FromHours(25)); // Keep for reporting
    }

    private bool IsHealthCheckEndpoint(PathString path)
    {
        var healthEndpoints = new[] { "/health", "/ready", "/live" };
        return healthEndpoints.Any(endpoint => path.StartsWithSegments(endpoint));
    }
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitingOptions
{
    public List<RateLimit> DefaultLimits { get; set; } = new()
    {
        new RateLimit { Requests = 100, Window = TimeSpan.FromMinutes(1) },
        new RateLimit { Requests = 1000, Window = TimeSpan.FromHours(1) }
    };

    public Dictionary<string, List<RateLimit>> EndpointLimits { get; set; } = new()
    {
        ["POST:/api/v1/bulk"] = new List<RateLimit>
        {
            new() { Requests = 10, Window = TimeSpan.FromMinutes(1) },
            new() { Requests = 50, Window = TimeSpan.FromHours(1) }
        },
        ["POST:/graphql"] = new List<RateLimit>
        {
            new() { Requests = 200, Window = TimeSpan.FromMinutes(1) },
            new() { Requests = 2000, Window = TimeSpan.FromHours(1) }
        }
    };

    public Dictionary<string, List<RateLimit>> PatternLimits { get; set; } = new()
    {
        [@"^POST:/api/v1/credentials"] = new List<RateLimit>
        {
            new() { Requests = 50, Window = TimeSpan.FromMinutes(1) }
        }
    };

    public int MaxViolationsBeforeBlock { get; set; } = 5;
    public TimeSpan BlockDuration { get; set; } = TimeSpan.FromHours(1);
}

public class RateLimit
{
    public int Requests { get; set; }
    public TimeSpan Window { get; set; }
}

/// <summary>
/// Extension methods for rate limiting
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddEnhancedRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options
        services.Configure<RateLimitingOptions>(
            configuration.GetSection("RateLimiting"));

        // Add distributed cache (Redis in production)
        if (configuration["Cache:Provider"] == "Redis")
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Cache:Redis:ConnectionString"];
                options.InstanceName = "numbatwallet";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        // Add ASP.NET Core rate limiting
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.",
                    cancellationToken);
            };
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext context)
    {
        // Use user ID if authenticated, otherwise use IP
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            return context.User.Identity.Name ?? "anonymous";
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public static IApplicationBuilder UseEnhancedRateLimiting(
        this IApplicationBuilder app)
    {
        app.UseMiddleware<EnhancedRateLimitingMiddleware>();
        app.UseRateLimiter();
        return app;
    }
}