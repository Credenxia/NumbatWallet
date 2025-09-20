using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NumbatWallet.Web.Api.Health;
using System.Text.Json;

namespace NumbatWallet.Web.Api.Extensions;

public static class HealthCheckExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    public static IServiceCollection AddCustomHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            // Liveness check is already added by ServiceDefaults
            // .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })

            // Readiness checks - dependencies
            .AddCheck<DatabaseHealthCheck>("database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready", "database" })

            .AddCheck<RedisHealthCheck>("redis",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "ready", "cache" })

            .AddCheck<StorageHealthCheck>("storage",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "ready", "storage" });

        return services;
    }

    public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponse
        });

        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse
        });

        app.UseHealthChecks("/health/startup", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteHealthCheckResponse
        });

        // Detailed health check endpoint (all checks)
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedHealthCheckResponse
        });

        return app;
    }

    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }

    private static async Task WriteDetailedHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                tags = entry.Value.Tags,
                data = entry.Value.Data,
                exception = entry.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }

    public static IEndpointRouteBuilder MapHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Map health check endpoints with metadata for OpenAPI
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponse
        })
        .WithName("LivenessCheck")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Liveness probe for container orchestration",
            Description = "Returns 200 if the application is alive",
            Tags = new List<Microsoft.OpenApi.Models.OpenApiTag>
            {
                new() { Name = "Health" }
            }
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse
        })
        .WithName("ReadinessCheck")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Readiness probe for container orchestration",
            Description = "Returns 200 if the application is ready to accept traffic",
            Tags = new List<Microsoft.OpenApi.Models.OpenApiTag>
            {
                new() { Name = "Health" }
            }
        });

        endpoints.MapHealthChecks("/health/startup", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteHealthCheckResponse
        })
        .WithName("StartupCheck")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Startup probe for container orchestration",
            Description = "Returns 200 if the application has completed startup",
            Tags = new List<Microsoft.OpenApi.Models.OpenApiTag>
            {
                new() { Name = "Health" }
            }
        });

        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedHealthCheckResponse
        })
        .WithName("DetailedHealthCheck")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Detailed health check",
            Description = "Returns detailed health status of all components",
            Tags = new List<Microsoft.OpenApi.Models.OpenApiTag>
            {
                new() { Name = "Health" }
            }
        });

        return endpoints;
    }
}