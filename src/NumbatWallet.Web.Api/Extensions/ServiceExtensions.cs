using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.EventSourcing;
using NumbatWallet.Web.Api.Caching;
using NumbatWallet.Web.Api.Webhooks;
using Polly;
using Polly.Extensions.Http;

namespace NumbatWallet.Web.Api.Extensions;

public static class ServiceExtensions
{
    /// <summary>
    /// Add caching services and policies
    /// </summary>
    public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add output caching
        services.AddOutputCache(options =>
        {
            // Default policy
            options.AddBasePolicy(builder => builder
                .Expire(TimeSpan.FromSeconds(30))
                .SetVaryByQuery("page", "limit", "sort", "filter")
                .Tag("default"));

            // Credential-specific policy
            options.AddPolicy("Credentials", builder => builder
                .Expire(TimeSpan.FromSeconds(30))
                .SetVaryByQuery("walletId", "type", "status")
                .SetVaryByHeader("Authorization")
                .Tag(CacheTags.Credentials));

            // Health check policy
            options.AddPolicy("Health", builder => builder
                .Expire(TimeSpan.FromSeconds(10))
                .Tag(CacheTags.Health));

            // No cache policy
            options.AddPolicy("NoCache", builder => builder.NoCache());
        });

        // Register cache policies
        services.AddSingleton<ApiCachePolicy>();
        services.AddSingleton<CredentialCachePolicy>();
        services.AddSingleton<NoCachePolicy>();

        // Register cache services
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        services.AddSingleton<ICacheWarmupService, CacheWarmupService>();

        // Register cache maintenance as hosted service
        services.AddHostedService<CacheMaintenanceService>();

        return services;
    }

    /// <summary>
    /// Add webhook services
    /// </summary>
    public static IServiceCollection AddWebhookServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register webhook service
        services.AddScoped<IWebhookService, WebhookService>();

        // Add HTTP client for webhooks with retry policy
        services.AddHttpClient("webhook")
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    /// <summary>
    /// Add event sourcing services
    /// </summary>
    public static IServiceCollection AddEventSourcingServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register event sourcing services
        services.AddEventSourcing();

        // Configure DbContext to use event sourcing interceptor
        services.AddDbContext<NumbatWalletDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("numbatwallet");
            options.UseNpgsql(connectionString);
            options.AddEventSourcingInterceptor(serviceProvider);
        });

        return services;
    }

    /// <summary>
    /// Configure output caching in the pipeline
    /// </summary>
    public static IApplicationBuilder UseOutputCaching(this IApplicationBuilder app)
    {
        app.UseOutputCache();
        return app;
    }

    /// <summary>
    /// Warmup cache on application start
    /// </summary>
    public static async Task WarmupCacheAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var cacheWarmupService = scope.ServiceProvider.GetService<ICacheWarmupService>();

        if (cacheWarmupService != null)
        {
            await cacheWarmupService.WarmupAsync();
        }
    }

    private static Polly.Retry.AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static Polly.CircuitBreaker.AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30));
    }
}