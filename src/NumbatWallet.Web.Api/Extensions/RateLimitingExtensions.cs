using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace NumbatWallet.Web.Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Default policy for authenticated users
            options.AddPolicy("authenticated", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context, true),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue("RateLimiting:Authenticated:PermitLimit", 100),
                        Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:Authenticated:WindowMinutes", 1))
                    }));

            // Strict policy for sensitive operations (credential issuance, revocation)
            options.AddPolicy("strict", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetPartitionKey(context, true),
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue("RateLimiting:Strict:PermitLimit", 10),
                        Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:Strict:WindowMinutes", 1)),
                        SegmentsPerWindow = 4
                    }));

            // Relaxed policy for read operations
            options.AddPolicy("relaxed", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context, false),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue("RateLimiting:Relaxed:PermitLimit", 500),
                        Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:Relaxed:WindowMinutes", 1))
                    }));

            // API key policy for service accounts
            options.AddPolicy("api-key", context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: GetApiKeyPartition(context),
                    factory: partition => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = configuration.GetValue("RateLimiting:ApiKey:TokenLimit", 1000),
                        ReplenishmentPeriod = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:ApiKey:ReplenishmentSeconds", 60)),
                        TokensPerPeriod = configuration.GetValue("RateLimiting:ApiKey:TokensPerPeriod", 100),
                        AutoReplenishment = true
                    }));

            // GraphQL specific policy
            options.AddPolicy("graphql", context =>
                RateLimitPartition.GetConcurrencyLimiter(
                    partitionKey: GetPartitionKey(context, true),
                    factory: partition => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = configuration.GetValue("RateLimiting:GraphQL:ConcurrentRequests", 10),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = configuration.GetValue("RateLimiting:GraphQL:QueueLimit", 50)
                    }));

            // Anonymous/public endpoints
            options.AddPolicy("anonymous", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetIpAddress(context),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue("RateLimiting:Anonymous:PermitLimit", 30),
                        Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:Anonymous:WindowMinutes", 1))
                    }));

            // Global limiter as ultimate fallback
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetIpAddress(context),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue("RateLimiting:Global:PermitLimit", 60),
                        Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:Global:WindowMinutes", 1))
                    }));

            // Custom rejection response
            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                var response = httpContext.Response;

                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.ContentType = "application/json";

                var retryAfter = GetRetryAfterSeconds(context.Lease);
                if (retryAfter > 0)
                {
                    response.Headers.RetryAfter = retryAfter.ToString();
                }

                await response.WriteAsJsonAsync(new
                {
                    error = "rate_limit_exceeded",
                    message = "Too many requests. Please retry later.",
                    retry_after_seconds = retryAfter
                }, cancellationToken);
            };
        });

        return services;
    }

    public static IApplicationBuilder UseCustomRateLimiting(this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }

    private static string GetPartitionKey(HttpContext context, bool useAuth)
    {
        if (useAuth && context.User?.Identity?.IsAuthenticated == true)
        {
            // Use tenant + user for multi-tenant isolation
            var tenantId = context.Items["TenantId"]?.ToString() ?? "default";
            var userId = context.User.Identity.Name ?? "anonymous";
            return $"{tenantId}:{userId}";
        }

        return GetIpAddress(context);
    }

    private static string GetApiKeyPartition(HttpContext context)
    {
        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey))
        {
            // Hash the API key for security
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(apiKey));
            return Convert.ToBase64String(hash);
        }
        return "no-api-key";
    }

    private static string GetIpAddress(HttpContext context)
    {
        // Check for proxy headers first
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static int GetRetryAfterSeconds(RateLimitLease? lease)
    {
        if (lease?.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) == true)
        {
            return (int)retryAfter.TotalSeconds;
        }
        return 60; // Default retry after 60 seconds
    }
}