using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Interface for metrics collection and reporting service
/// </summary>
public interface IMetricsService : IDisposable
{
    /// <summary>
    /// Record metrics for an HTTP request
    /// </summary>
    Task RecordRequestMetricsAsync(RequestMetricsDto metrics);

    /// <summary>
    /// Send an alert for a slow request
    /// </summary>
    Task SendSlowRequestAlertAsync(RequestMetricsDto metrics);

    /// <summary>
    /// Increment a counter metric
    /// </summary>
    Task IncrementCounterAsync(string name, double value = 1, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Record a gauge metric (current value)
    /// </summary>
    Task RecordGaugeAsync(string name, double value, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Record a histogram metric (distribution of values)
    /// </summary>
    Task RecordHistogramAsync(string name, double value, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Get a snapshot of all current metrics
    /// </summary>
    Task<Dictionary<string, object>> GetMetricsSnapshotAsync();

    /// <summary>
    /// Reset all metrics
    /// </summary>
    Task ResetMetricsAsync();
}