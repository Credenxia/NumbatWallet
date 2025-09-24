using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace NumbatWallet.Web.Api.Telemetry;

/// <summary>
/// Performance metrics for NumbatWallet API
/// </summary>
public sealed class PerformanceMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<long> _errorCounter;
    private readonly UpDownCounter<long> _activeRequests;
    private readonly Histogram<long> _dbQueryDuration;
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly ObservableGauge<long> _memoryUsage;
    private readonly ObservableGauge<int> _threadCount;
    private readonly ObservableGauge<double> _cpuUsage;

    private readonly Process _currentProcess;
    private DateTime _lastCpuCheck;
    private TimeSpan _lastTotalProcessorTime;

    public PerformanceMetrics()
    {
        _meter = new Meter("NumbatWallet.Web.Api", "1.0.0");
        _currentProcess = Process.GetCurrentProcess();
        _lastCpuCheck = DateTime.UtcNow;
        _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;

        // Request metrics
        _requestCounter = _meter.CreateCounter<long>(
            "numbatwallet.api.requests",
            unit: "requests",
            description: "Total number of API requests");

        _requestDuration = _meter.CreateHistogram<double>(
            "numbatwallet.api.request.duration",
            unit: "ms",
            description: "Request duration in milliseconds");

        _errorCounter = _meter.CreateCounter<long>(
            "numbatwallet.api.errors",
            unit: "errors",
            description: "Total number of errors");

        _activeRequests = _meter.CreateUpDownCounter<long>(
            "numbatwallet.api.active_requests",
            unit: "requests",
            description: "Number of currently active requests");

        // Database metrics
        _dbQueryDuration = _meter.CreateHistogram<long>(
            "numbatwallet.db.query.duration",
            unit: "ms",
            description: "Database query duration in milliseconds");

        // Cache metrics
        _cacheHits = _meter.CreateCounter<long>(
            "numbatwallet.cache.hits",
            unit: "hits",
            description: "Number of cache hits");

        _cacheMisses = _meter.CreateCounter<long>(
            "numbatwallet.cache.misses",
            unit: "misses",
            description: "Number of cache misses");

        // System metrics
        _memoryUsage = _meter.CreateObservableGauge<long>(
            "numbatwallet.memory.used",
            observeValue: () => GC.GetTotalMemory(false),
            unit: "bytes",
            description: "Memory usage in bytes");

        _threadCount = _meter.CreateObservableGauge<int>(
            "numbatwallet.threads.count",
            observeValue: () => Process.GetCurrentProcess().Threads.Count,
            unit: "threads",
            description: "Number of threads");

        _cpuUsage = _meter.CreateObservableGauge<double>(
            "numbatwallet.cpu.usage",
            observeValue: GetCpuUsage,
            unit: "%",
            description: "CPU usage percentage");
    }

    /// <summary>
    /// Record an API request
    /// </summary>
    public void RecordRequest(string endpoint, string method, int statusCode, double durationMs)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("status_code", statusCode)
        };

        _requestCounter.Add(1, tags);
        _requestDuration.Record(durationMs, tags);

        if (statusCode >= 400)
        {
            _errorCounter.Add(1, tags);
        }
    }

    /// <summary>
    /// Increment active requests
    /// </summary>
    public void IncrementActiveRequests() => _activeRequests.Add(1);

    /// <summary>
    /// Decrement active requests
    /// </summary>
    public void DecrementActiveRequests() => _activeRequests.Add(-1);

    /// <summary>
    /// Record database query duration
    /// </summary>
    public void RecordDbQuery(string queryType, long durationMs)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("query_type", queryType)
        };

        _dbQueryDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Record cache hit
    /// </summary>
    public void RecordCacheHit(string cacheKey)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("cache_key", cacheKey)
        };

        _cacheHits.Add(1, tags);
    }

    /// <summary>
    /// Record cache miss
    /// </summary>
    public void RecordCacheMiss(string cacheKey)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("cache_key", cacheKey)
        };

        _cacheMisses.Add(1, tags);
    }

    private double GetCpuUsage()
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            _currentProcess.Refresh();
            var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;

            var timeDiff = (currentTime - _lastCpuCheck).TotalMilliseconds;
            var cpuTimeDiff = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;

            _lastCpuCheck = currentTime;
            _lastTotalProcessorTime = currentTotalProcessorTime;

            var cpuUsage = (cpuTimeDiff / timeDiff) * 100 / Environment.ProcessorCount;
            return Math.Min(100, Math.Max(0, cpuUsage));
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        _meter?.Dispose();
        _currentProcess?.Dispose();
    }
}

/// <summary>
/// Performance monitoring middleware
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PerformanceMetrics _metrics;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        PerformanceMetrics metrics,
        ILogger<PerformanceMonitoringMiddleware> logger)
    {
        _next = next;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip metrics for health checks and metrics endpoints
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/metrics"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _metrics.IncrementActiveRequests();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.DecrementActiveRequests();

            var endpoint = context.GetEndpoint()?.DisplayName ?? context.Request.Path.ToString();
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;
            var duration = stopwatch.Elapsed.TotalMilliseconds;

            _metrics.RecordRequest(endpoint, method, statusCode, duration);

            // Log slow requests
            if (duration > 1000)
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} took {Duration}ms with status {StatusCode}",
                    method, context.Request.Path, duration, statusCode);
            }
        }
    }
}

/// <summary>
/// Extension methods for performance monitoring
/// </summary>
public static class PerformanceMonitoringExtensions
{
    /// <summary>
    /// Add performance monitoring services
    /// </summary>
    public static IServiceCollection AddPerformanceMonitoring(this IServiceCollection services)
    {
        services.AddSingleton<PerformanceMetrics>();

        // Add OpenTelemetry metrics if configured
        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter("NumbatWallet.Web.Api")
                    .AddPrometheusExporter();
            });

        return services;
    }

    /// <summary>
    /// Use performance monitoring middleware
    /// </summary>
    public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder app)
    {
        app.UseMiddleware<PerformanceMonitoringMiddleware>();
        return app;
    }
}