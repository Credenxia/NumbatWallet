using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NumbatWallet.Web.Api.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(
        HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Get overall health status
    /// </summary>
    [HttpGet]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(HealthReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthReportDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var response = new HealthReportDto
        {
            Status = report.Status.ToString(),
            TotalDuration = report.TotalDuration,
            Entries = report.Entries.Select(e => new HealthCheckEntryDto
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Duration = e.Value.Duration,
                Description = e.Value.Description,
                Tags = e.Value.Tags.ToList(),
                Data = e.Value.Data.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty
                ),
                Exception = e.Value.Exception?.Message
            }).ToList()
        };

        var statusCode = report.Status == HealthStatus.Healthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Get health status for a specific component
    /// </summary>
    [HttpGet("{component}")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(HealthCheckEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HealthCheckEntryDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetComponentHealth(string component)
    {
        var report = await _healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains(component));

        if (!report.Entries.Any())
        {
            return NotFound($"Health check for component '{component}' not found");
        }

        var entry = report.Entries.First();
        var response = new HealthCheckEntryDto
        {
            Name = entry.Key,
            Status = entry.Value.Status.ToString(),
            Duration = entry.Value.Duration,
            Description = entry.Value.Description,
            Tags = entry.Value.Tags.ToList(),
            Data = entry.Value.Data.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty
            ),
            Exception = entry.Value.Exception?.Message
        };

        var statusCode = entry.Value.Status == HealthStatus.Healthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Get liveness status (basic check if service is alive)
    /// </summary>
    [HttpGet("live")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(LivenessCheckDto), StatusCodes.Status200OK)]
    public IActionResult GetLiveness()
    {
        return Ok(new LivenessCheckDto
        {
            Status = "Alive",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get readiness status (check if service is ready to handle requests)
    /// </summary>
    [HttpGet("ready")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(ReadinessCheckDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReadinessCheckDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReadiness()
    {
        var report = await _healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains("readiness"));

        var isReady = report.Status == HealthStatus.Healthy;

        var response = new ReadinessCheckDto
        {
            Ready = isReady,
            Status = report.Status.ToString(),
            Timestamp = DateTime.UtcNow,
            Checks = report.Entries.Select(e => new ReadinessCheckItemDto
            {
                Name = e.Key,
                Ready = e.Value.Status == HealthStatus.Healthy,
                Message = e.Value.Description ?? e.Value.Exception?.Message
            }).ToList()
        };

        var statusCode = isReady
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Get startup status (check if service has completed startup)
    /// </summary>
    [HttpGet("startup")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(StartupCheckDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(StartupCheckDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetStartup()
    {
        var report = await _healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains("startup"));

        var isStarted = report.Status == HealthStatus.Healthy;

        var response = new StartupCheckDto
        {
            Started = isStarted,
            Status = report.Status.ToString(),
            Timestamp = DateTime.UtcNow,
            StartupChecks = report.Entries.Select(e => new StartupCheckItemDto
            {
                Component = e.Key,
                Started = e.Value.Status == HealthStatus.Healthy,
                Duration = e.Value.Duration,
                Message = e.Value.Description ?? e.Value.Exception?.Message
            }).ToList()
        };

        var statusCode = isStarted
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;

        return StatusCode(statusCode, response);
    }
}

// DTOs for health checks
public class HealthReportDto
{
    public required string Status { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public List<HealthCheckEntryDto> Entries { get; set; } = new();
}

public class HealthCheckEntryDto
{
    public required string Name { get; set; }
    public required string Status { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> Data { get; set; } = new();
    public string? Exception { get; set; }
}

public class LivenessCheckDto
{
    public required string Status { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ReadinessCheckDto
{
    public bool Ready { get; set; }
    public required string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public List<ReadinessCheckItemDto> Checks { get; set; } = new();
}

public class ReadinessCheckItemDto
{
    public required string Name { get; set; }
    public bool Ready { get; set; }
    public string? Message { get; set; }
}

public class StartupCheckDto
{
    public bool Started { get; set; }
    public required string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public List<StartupCheckItemDto> StartupChecks { get; set; } = new();
}

public class StartupCheckItemDto
{
    public required string Component { get; set; }
    public bool Started { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Message { get; set; }
}