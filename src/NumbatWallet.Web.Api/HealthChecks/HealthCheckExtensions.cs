using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace NumbatWallet.Web.Api.HealthChecks;

/// <summary>
/// Extension methods for configuring health checks
/// </summary>
public static class HealthCheckExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    /// <summary>
    /// Add all health checks to the service collection
    /// </summary>
    public static IServiceCollection AddHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Database health check
        healthChecksBuilder.AddTypeActivatedCheck<DatabaseHealthCheck>(
            "database",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "db", "sql", "critical" });

        // Redis cache health check (if Redis is configured)
        if (!string.IsNullOrEmpty(configuration.GetConnectionString("Redis")))
        {
            healthChecksBuilder.AddTypeActivatedCheck<RedisCacheHealthCheck>(
                "redis",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "cache", "redis" });
        }


        // ServiceWA API health check
        healthChecksBuilder.AddTypeActivatedCheck<ServiceWAHealthCheck>(
            "servicewa",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "external", "api" });

        // Memory health check
        healthChecksBuilder.AddTypeActivatedCheck<MemoryHealthCheck>(
            "memory",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "system", "performance" });

        // Disk space health check
        healthChecksBuilder.AddTypeActivatedCheck<DiskSpaceHealthCheck>(
            "diskspace",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "system", "storage" });


        // Health check UI can be added with additional packages if needed

        return services;
    }

    /// <summary>
    /// Configure health check endpoints
    /// </summary>
    public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app)
    {
        // Basic health check endpoint
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse,
            Predicate = _ => true
        });

        // Liveness probe (basic check)
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("critical"),
            ResponseWriter = WriteHealthCheckResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Readiness probe (full check)
        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteHealthCheckResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Detailed health check with individual results
        app.UseHealthChecks("/health/detail", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedHealthCheckResponse,
            Predicate = _ => true,
            AllowCachingResponses = false
        });

        // Health check UI can be added with additional packages if needed

        return app;
    }

    /// <summary>
    /// Write basic health check response
    /// </summary>
    private static async Task WriteHealthCheckResponse(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var response = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Write detailed health check response with individual check results
    /// </summary>
    private static async Task WriteDetailedHealthCheckResponse(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var healthChecks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            duration = entry.Value.Duration.TotalMilliseconds,
            tags = entry.Value.Tags,
            data = entry.Value.Data.Count > 0 ? entry.Value.Data : null,
            exception = entry.Value.Exception?.Message
        });

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            results = healthChecks
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Custom health check options for specific scenarios
/// </summary>
public class HealthCheckConfiguration
{
    /// <summary>
    /// Database connection timeout in seconds
    /// </summary>
    public int DatabaseTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Redis connection timeout in seconds
    /// </summary>
    public int RedisTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// External API timeout in seconds
    /// </summary>
    public int ApiTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum memory usage in MB before considered unhealthy
    /// </summary>
    public long MaxMemoryMb { get; set; } = 1024;

    /// <summary>
    /// Minimum free disk space in MB
    /// </summary>
    public long MinFreeDiskMb { get; set; } = 500;

    /// <summary>
    /// Certificate expiry warning days
    /// </summary>
    public int CertificateExpiryWarningDays { get; set; } = 30;

    /// <summary>
    /// Enable detailed health check responses
    /// </summary>
    public bool EnableDetailedResponses { get; set; } = true;

    /// <summary>
    /// Enable health check UI
    /// </summary>
    public bool EnableHealthCheckUI { get; set; } = true;
}