using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace NumbatWallet.Web.Api.RateLimiting;

/// <summary>
/// Rate limiting policy configurations for the API
/// </summary>
public static class RateLimitingPolicies
{
    public const string Fixed = "fixed";
    public const string Sliding = "sliding";
    public const string Token = "token";
    public const string Concurrency = "concurrency";
    public const string ApiKey = "apikey";
    public const string PerUser = "peruser";
    public const string Public = "public";
    public const string Authenticated = "authenticated";
    public const string Admin = "admin";
    public const string BatchOperations = "batch";
    public const string Verification = "verification";

    /// <summary>
    /// Configure rate limiting policies for the application
    /// </summary>
    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Default 429 response
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Fixed window policy - 100 requests per minute for general API
            options.AddFixedWindowLimiter(policyName: Fixed, limiterOptions =>
            {
                limiterOptions.PermitLimit = 100;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 10;
            });

            // Sliding window policy - smoother rate limiting
            options.AddSlidingWindowLimiter(policyName: Sliding, limiterOptions =>
            {
                limiterOptions.PermitLimit = 60;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.SegmentsPerWindow = 4; // 15-second segments
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 5;
            });

            // Token bucket policy - burst handling
            options.AddTokenBucketLimiter(policyName: Token, limiterOptions =>
            {
                limiterOptions.TokenLimit = 100;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 10;
                limiterOptions.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
                limiterOptions.TokensPerPeriod = 20;
                limiterOptions.AutoReplenishment = true;
            });

            // Concurrency limiter - limit parallel requests
            options.AddConcurrencyLimiter(policyName: Concurrency, limiterOptions =>
            {
                limiterOptions.PermitLimit = 10;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 20;
            });

            // Per-user rate limiting for authenticated users
            options.AddPolicy(policyName: PerUser, context =>
            {
                var userId = context.User?.Identity?.Name ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 200, // Higher limit for authenticated users
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 20
                    });
            });

            // Public endpoints - stricter limits
            options.AddPolicy(policyName: Public, context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ipAddress,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30, // Lower limit for public endpoints
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    });
            });

            // API key based rate limiting
            options.AddPolicy(policyName: ApiKey, context =>
            {
                var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault() ?? "no-key";

                // Different limits based on API key tier
                var (permitLimit, window) = GetApiKeyLimits(apiKey);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: apiKey,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    });
            });

            // Admin endpoints - higher limits
            options.AddPolicy(policyName: Admin, context =>
            {
                if (!context.User?.IsInRole("Admin") ?? false)
                {
                    // Non-admin users get regular limits
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: "non-admin",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 10
                        });
                }

                var userId = context.User?.Identity?.Name ?? "admin";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 500, // Very high limit for admins
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 50
                    });
            });

            // Batch operations - special handling for bulk requests
            options.AddPolicy(policyName: BatchOperations, context =>
            {
                var userId = context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetConcurrencyLimiter(
                    partitionKey: userId,
                    factory: partition => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = 3, // Only 3 concurrent batch operations
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    });
            });

            // Verification endpoints - prevent abuse
            options.AddPolicy(policyName: Verification, context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: ipAddress,
                    factory: partition => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 50,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 50,
                        AutoReplenishment = true
                    });
            });

            // Global limiter as fallback
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "global";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 1000,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 100
                    });
            });

            // Custom rejection response
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var retryAfter = GetRetryAfterSeconds(context.Lease);
                if (retryAfter.HasValue)
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.Value.ToString();
                }

                var response = new
                {
                    error = "Too many requests",
                    message = "You have exceeded the rate limit. Please try again later.",
                    retryAfter = retryAfter
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Get rate limits based on API key tier
    /// </summary>
    private static (int permitLimit, TimeSpan window) GetApiKeyLimits(string apiKey)
    {
        // In production, this would look up the API key in a database
        // For demo, we'll use simple logic
        return apiKey switch
        {
            var key when key.StartsWith("premium-", StringComparison.Ordinal) => (1000, TimeSpan.FromMinutes(1)),
            var key when key.StartsWith("standard-", StringComparison.Ordinal) => (500, TimeSpan.FromMinutes(1)),
            var key when key.StartsWith("basic-", StringComparison.Ordinal) => (100, TimeSpan.FromMinutes(1)),
            _ => (50, TimeSpan.FromMinutes(1)) // Default/unknown keys
        };
    }

    /// <summary>
    /// Calculate retry-after seconds from lease metadata
    /// </summary>
    private static int? GetRetryAfterSeconds(RateLimitLease lease)
    {
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            return (int)retryAfter.TotalSeconds;
        }

        // Default to 60 seconds if no metadata available
        return 60;
    }
}

/// <summary>
/// Rate limiting attribute for controller actions
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RateLimitAttribute : Attribute
{
    public string PolicyName { get; }

    public RateLimitAttribute(string policyName)
    {
        PolicyName = policyName;
    }
}

/// <summary>
/// Extension methods for applying rate limiting to endpoints
/// </summary>
public static class RateLimitingExtensions
{
    public static IEndpointConventionBuilder RequireRateLimiting(
        this IEndpointConventionBuilder builder,
        string policyName)
    {
        return builder.RequireRateLimiting(policyName);
    }

    public static IApplicationBuilder UseRateLimitingPolicies(this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }
}