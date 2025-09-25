using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// Service for collecting and reporting application metrics
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly ConcurrentDictionary<string, MetricData> _metrics;
    private readonly Timer _reportingTimer;
    private readonly MetricsConfiguration _configuration;

    public MetricsService(ILogger<MetricsService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _metrics = new ConcurrentDictionary<string, MetricData>();
        _configuration = configuration.GetSection("Metrics").Get<MetricsConfiguration>() ?? new MetricsConfiguration();

        // Start periodic reporting if enabled
        if (_configuration.EnablePeriodicReporting)
        {
            _reportingTimer = new Timer(
                ReportMetrics,
                null,
                TimeSpan.FromSeconds(_configuration.ReportingIntervalSeconds),
                TimeSpan.FromSeconds(_configuration.ReportingIntervalSeconds));
        }
        else
        {
            _reportingTimer = null!;
        }
    }

    public Task RecordRequestMetricsAsync(RequestMetricsDto metrics)
    {
        try
        {
            var key = $"{metrics.Method}:{metrics.Path}";

            _metrics.AddOrUpdate(key,
                new MetricData
                {
                    Count = 1,
                    TotalResponseTime = metrics.ResponseTime,
                    MinResponseTime = metrics.ResponseTime,
                    MaxResponseTime = metrics.ResponseTime,
                    ErrorCount = metrics.StatusCode >= 500 ? 1 : 0,
                    LastUpdated = DateTime.UtcNow
                },
                (_, existing) =>
                {
                    existing.Count++;
                    existing.TotalResponseTime += metrics.ResponseTime;
                    existing.MinResponseTime = Math.Min(existing.MinResponseTime, metrics.ResponseTime);
                    existing.MaxResponseTime = Math.Max(existing.MaxResponseTime, metrics.ResponseTime);
                    if (metrics.StatusCode >= 500)
                    {
                        existing.ErrorCount++;
                    }
                    existing.LastUpdated = DateTime.UtcNow;
                    return existing;
                });

            // Track status code distribution
            var statusKey = $"status:{metrics.StatusCode}";
            _metrics.AddOrUpdate(statusKey,
                new MetricData { Count = 1, LastUpdated = DateTime.UtcNow },
                (_, existing) =>
                {
                    existing.Count++;
                    existing.LastUpdated = DateTime.UtcNow;
                    return existing;
                });

            // Track slow requests
            if (metrics.ResponseTime > _configuration.SlowRequestThresholdMs)
            {
                _logger.LogDebug(
                    "Slow request recorded: {Method} {Path} took {ResponseTime}ms",
                    metrics.Method, metrics.Path, metrics.ResponseTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record request metrics");
        }

        return Task.CompletedTask;
    }

    public Task SendSlowRequestAlertAsync(RequestMetricsDto metrics)
    {
        try
        {
            _logger.LogWarning(
                "SLOW REQUEST ALERT: {Method} {Path} took {ResponseTime}ms. " +
                "RequestId: {RequestId}, ClientIP: {ClientIP}",
                metrics.Method,
                metrics.Path,
                metrics.ResponseTime,
                metrics.RequestId,
                metrics.ClientIp);

            // In production, this would send to alerting system (e.g., Azure Monitor, PagerDuty)
            // For now, we just log it
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send slow request alert");
        }

        return Task.CompletedTask;
    }

    public Task IncrementCounterAsync(string name, double value = 1, Dictionary<string, string>? tags = null)
    {
        try
        {
            var key = BuildMetricKey(name, tags);

            _metrics.AddOrUpdate(key,
                new MetricData
                {
                    Count = (long)value,
                    LastUpdated = DateTime.UtcNow,
                    Tags = tags ?? new Dictionary<string, string>()
                },
                (_, existing) =>
                {
                    existing.Count += (long)value;
                    existing.LastUpdated = DateTime.UtcNow;
                    return existing;
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment counter {Name}", name);
        }

        return Task.CompletedTask;
    }

    public Task RecordGaugeAsync(string name, double value, Dictionary<string, string>? tags = null)
    {
        try
        {
            var key = BuildMetricKey(name, tags);

            _metrics.AddOrUpdate(key,
                new MetricData
                {
                    Value = value,
                    LastUpdated = DateTime.UtcNow,
                    Tags = tags ?? new Dictionary<string, string>()
                },
                (_, existing) =>
                {
                    existing.Value = value;
                    existing.LastUpdated = DateTime.UtcNow;
                    return existing;
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record gauge {Name}", name);
        }

        return Task.CompletedTask;
    }

    public Task RecordHistogramAsync(string name, double value, Dictionary<string, string>? tags = null)
    {
        try
        {
            var key = BuildMetricKey(name, tags);

            _metrics.AddOrUpdate(key,
                new MetricData
                {
                    Count = 1,
                    TotalValue = value,
                    MinValue = value,
                    MaxValue = value,
                    LastUpdated = DateTime.UtcNow,
                    Tags = tags ?? new Dictionary<string, string>()
                },
                (_, existing) =>
                {
                    existing.Count++;
                    existing.TotalValue += value;
                    existing.MinValue = Math.Min(existing.MinValue, value);
                    existing.MaxValue = Math.Max(existing.MaxValue, value);
                    existing.LastUpdated = DateTime.UtcNow;
                    return existing;
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record histogram {Name}", name);
        }

        return Task.CompletedTask;
    }

    public Task<Dictionary<string, object>> GetMetricsSnapshotAsync()
    {
        var snapshot = new Dictionary<string, object>();

        foreach (var kvp in _metrics)
        {
            var metric = kvp.Value;
            var metricSnapshot = new Dictionary<string, object>();

            if (metric.Count > 0)
            {
                metricSnapshot["count"] = metric.Count;
            }

            if (metric.Value.HasValue)
            {
                metricSnapshot["value"] = metric.Value.Value;
            }

            if (metric.TotalResponseTime > 0)
            {
                metricSnapshot["total_response_time"] = metric.TotalResponseTime;
                metricSnapshot["avg_response_time"] = metric.TotalResponseTime / metric.Count;
                metricSnapshot["min_response_time"] = metric.MinResponseTime;
                metricSnapshot["max_response_time"] = metric.MaxResponseTime;
            }

            if (metric.TotalValue > 0)
            {
                metricSnapshot["total"] = metric.TotalValue;
                metricSnapshot["average"] = metric.TotalValue / metric.Count;
                metricSnapshot["min"] = metric.MinValue;
                metricSnapshot["max"] = metric.MaxValue;
            }

            if (metric.ErrorCount > 0)
            {
                metricSnapshot["error_count"] = metric.ErrorCount;
            }

            metricSnapshot["last_updated"] = metric.LastUpdated;

            if (metric.Tags.Any())
            {
                metricSnapshot["tags"] = metric.Tags;
            }

            snapshot[kvp.Key] = metricSnapshot;
        }

        return Task.FromResult(snapshot);
    }

    public async Task ResetMetricsAsync()
    {
        _logger.LogInformation("Resetting all metrics");
        _metrics.Clear();
        await Task.CompletedTask;
    }

    private string BuildMetricKey(string name, Dictionary<string, string>? tags)
    {
        if (tags == null || !tags.Any())
        {
            return name;
        }

        var tagString = string.Join(",", tags.OrderBy(t => t.Key).Select(t => $"{t.Key}={t.Value}"));
        return $"{name}:{tagString}";
    }

    private async void ReportMetrics(object? state)
    {
        try
        {
            var snapshot = await GetMetricsSnapshotAsync();

            if (snapshot.Any())
            {
                _logger.LogInformation("Metrics Report: {MetricsCount} metrics collected", snapshot.Count);

                // Log summary of key metrics
                var requestMetrics = snapshot.Where(k => k.Key.Contains(':')).ToList();
                if (requestMetrics.Any())
                {
                    _logger.LogInformation(
                        "Request metrics: {EndpointCount} endpoints tracked",
                        requestMetrics.Count);
                }

                var statusMetrics = snapshot.Where(k => k.Key.StartsWith("status:", StringComparison.Ordinal)).ToList();
                if (statusMetrics.Any())
                {
                    var statusSummary = string.Join(", ",
                        statusMetrics.Select(m => $"{m.Key}: {((Dictionary<string, object>)m.Value)["count"]}"));
                    _logger.LogInformation("Status code distribution: {StatusSummary}", statusSummary);
                }
            }

            // Clean up old metrics
            var cutoff = DateTime.UtcNow.AddMinutes(-_configuration.MetricRetentionMinutes);
            var keysToRemove = _metrics
                .Where(m => m.Value.LastUpdated < cutoff)
                .Select(m => m.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _metrics.TryRemove(key, out _);
            }

            if (keysToRemove.Any())
            {
                _logger.LogDebug("Cleaned up {Count} old metrics", keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report metrics");
        }
    }

    public void Dispose()
    {
        _reportingTimer?.Dispose();
    }

    private class MetricData
    {
        public long Count { get; set; }
        public double? Value { get; set; }
        public long TotalResponseTime { get; set; }
        public long MinResponseTime { get; set; } = long.MaxValue;
        public long MaxResponseTime { get; set; }
        public double TotalValue { get; set; }
        public double MinValue { get; set; } = double.MaxValue;
        public double MaxValue { get; set; }
        public int ErrorCount { get; set; }
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }
}

/// <summary>
/// Configuration for metrics service
/// </summary>
public class MetricsConfiguration
{
    public bool EnablePeriodicReporting { get; set; } = true;
    public int ReportingIntervalSeconds { get; set; } = 60;
    public int MetricRetentionMinutes { get; set; } = 60;
    public long SlowRequestThresholdMs { get; set; } = 1000;
}