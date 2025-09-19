using Carter;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NumbatWallet.Web.Api.Rest.Common;
using System.Text.Json;

namespace NumbatWallet.Web.Api.Rest.Modules;

/// <summary>
/// Health check endpoints for Kubernetes and monitoring
/// </summary>
public class HealthModule : RestEndpointBase
{
    public override string RoutePrefix => "";

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        // Basic health check
        app.MapGet("/health", GetHealth)
            .WithName("GetHealth")
            .WithTags("Health")
            .WithOpenApi()
            .Produces(200)
            .AllowAnonymous();

        // Readiness probe - checks if app is ready to accept traffic
        app.MapGet("/ready", GetReadiness)
            .WithName("GetReadiness")
            .WithTags("Health")
            .WithOpenApi()
            .Produces(200)
            .Produces(503)
            .AllowAnonymous();

        // Liveness probe - checks if app is running
        app.MapGet("/live", GetLiveness)
            .WithName("GetLiveness")
            .WithTags("Health")
            .WithOpenApi()
            .Produces(200)
            .AllowAnonymous();

        // Detailed health check with all components
        app.MapGet("/health/details", GetDetailedHealth)
            .WithName("GetDetailedHealth")
            .WithTags("Health")
            .WithOpenApi()
            .Produces(200)
            .RequireAuthorization("Admin");
    }

    private static async Task<IResult> GetHealth(HealthCheckService healthCheckService)
    {
        var result = await healthCheckService.CheckHealthAsync();

        return Results.Json(new
        {
            status = result.Status.ToString(),
            timestamp = DateTime.UtcNow,
            version = typeof(HealthModule).Assembly.GetName().Version?.ToString() ?? "1.0.0"
        }, statusCode: result.Status == HealthStatus.Healthy ? 200 : 503);
    }

    private static async Task<IResult> GetReadiness(
        HealthCheckService healthCheckService,
        IConfiguration configuration)
    {
        // Check only critical dependencies for readiness
        var result = await healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains("critical"));

        if (result.Status == HealthStatus.Healthy)
        {
            return Results.Json(new
            {
                status = "Ready",
                timestamp = DateTime.UtcNow,
                checks = result.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString()
                })
            });
        }

        return Results.Json(new
        {
            status = "NotReady",
            timestamp = DateTime.UtcNow,
            failedChecks = result.Entries
                .Where(e => e.Value.Status != HealthStatus.Healthy)
                .Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description
                })
        }, statusCode: 503);
    }

    private static IResult GetLiveness()
    {
        // Simple liveness check - if we can respond, we're alive
        return Results.Json(new
        {
            status = "Alive",
            timestamp = DateTime.UtcNow,
            uptime = GetUptime()
        });
    }

    private static async Task<IResult> GetDetailedHealth(HealthCheckService healthCheckService)
    {
        var result = await healthCheckService.CheckHealthAsync();

        var response = new
        {
            status = result.Status.ToString(),
            totalDuration = result.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            uptime = GetUptime(),
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            checks = result.Entries.Select(e => new
            {
                component = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                tags = e.Value.Tags,
                description = e.Value.Description,
                exception = e.Value.Exception?.Message,
                data = e.Value.Data.Count > 0 ? e.Value.Data : null
            }).OrderBy(c => c.component),
            summary = new
            {
                healthy = result.Entries.Count(e => e.Value.Status == HealthStatus.Healthy),
                degraded = result.Entries.Count(e => e.Value.Status == HealthStatus.Degraded),
                unhealthy = result.Entries.Count(e => e.Value.Status == HealthStatus.Unhealthy),
                total = result.Entries.Count
            }
        };

        return Results.Json(response, statusCode: result.Status == HealthStatus.Healthy ? 200 : 503);
    }

    private static string GetUptime()
    {
        var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }
}