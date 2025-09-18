namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for telemetry logging with automatic PII redaction
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Logs a custom event with automatic PII redaction
    /// </summary>
    /// <param name="eventName">Name of the event</param>
    /// <param name="properties">Event properties (will be scanned for PII)</param>
    /// <param name="metrics">Event metrics</param>
    Task LogEventAsync(
        string eventName,
        Dictionary<string, string>? properties = null,
        Dictionary<string, double>? metrics = null);

    /// <summary>
    /// Logs a security event with enhanced tracking
    /// </summary>
    /// <param name="eventType">Type of security event</param>
    /// <param name="severity">Severity level (Low, Medium, High, Critical)</param>
    /// <param name="details">Event details (will be redacted)</param>
    Task LogSecurityEventAsync(
        string eventType,
        string severity,
        object? details = null);

    /// <summary>
    /// Logs an exception with automatic PII redaction from message and stack trace
    /// </summary>
    /// <param name="exception">The exception to log</param>
    /// <param name="properties">Additional context properties</param>
    Task LogExceptionAsync(
        Exception exception,
        Dictionary<string, string>? properties = null);

    /// <summary>
    /// Logs a metric value
    /// </summary>
    /// <param name="metricName">Name of the metric</param>
    /// <param name="value">Metric value</param>
    /// <param name="properties">Additional properties</param>
    Task LogMetricAsync(
        string metricName,
        double value,
        Dictionary<string, string>? properties = null);

    /// <summary>
    /// Logs a dependency call with automatic PII redaction
    /// </summary>
    /// <param name="dependencyType">Type of dependency (HTTP, SQL, etc.)</param>
    /// <param name="dependencyName">Name of the dependency</param>
    /// <param name="data">Call data (will be redacted)</param>
    /// <param name="startTime">Start time of the call</param>
    /// <param name="duration">Duration of the call</param>
    /// <param name="success">Whether the call succeeded</param>
    Task LogDependencyAsync(
        string dependencyType,
        string dependencyName,
        string data,
        DateTimeOffset startTime,
        TimeSpan duration,
        bool success);

    /// <summary>
    /// Logs a page view event
    /// </summary>
    /// <param name="pageName">Name of the page</param>
    /// <param name="url">Page URL (will be redacted)</param>
    /// <param name="duration">Time spent on page</param>
    /// <param name="properties">Additional properties</param>
    Task LogPageViewAsync(
        string pageName,
        string url,
        TimeSpan? duration = null,
        Dictionary<string, string>? properties = null);

    /// <summary>
    /// Logs a request (API call)
    /// </summary>
    /// <param name="requestName">Name of the request</param>
    /// <param name="startTime">Start time</param>
    /// <param name="duration">Duration</param>
    /// <param name="responseCode">HTTP response code</param>
    /// <param name="success">Whether successful</param>
    /// <param name="properties">Additional properties</param>
    Task LogRequestAsync(
        string requestName,
        DateTimeOffset startTime,
        TimeSpan duration,
        string responseCode,
        bool success,
        Dictionary<string, string>? properties = null);

    /// <summary>
    /// Logs a trace message
    /// </summary>
    /// <param name="message">Trace message (will be redacted)</param>
    /// <param name="severityLevel">Severity level</param>
    /// <param name="properties">Additional properties</param>
    Task LogTraceAsync(
        string message,
        TelemetrySeverityLevel severityLevel = TelemetrySeverityLevel.Information,
        Dictionary<string, string>? properties = null);

    /// <summary>
    /// Logs an availability test result
    /// </summary>
    /// <param name="testName">Name of the test</param>
    /// <param name="startTime">Start time</param>
    /// <param name="duration">Duration</param>
    /// <param name="runLocation">Test run location</param>
    /// <param name="success">Whether successful</param>
    /// <param name="message">Optional message</param>
    Task LogAvailabilityAsync(
        string testName,
        DateTimeOffset startTime,
        TimeSpan duration,
        string runLocation,
        bool success,
        string? message = null);
}

/// <summary>
/// Severity level for telemetry traces
/// </summary>
public enum TelemetrySeverityLevel
{
    /// <summary>
    /// Verbose level
    /// </summary>
    Verbose,

    /// <summary>
    /// Information level
    /// </summary>
    Information,

    /// <summary>
    /// Warning level
    /// </summary>
    Warning,

    /// <summary>
    /// Error level
    /// </summary>
    Error,

    /// <summary>
    /// Critical level
    /// </summary>
    Critical
}