using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace NumbatWallet.Web.Api.Telemetry;

/// <summary>
/// Extensions for configuring OpenTelemetry
/// </summary>
public static class TelemetryExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["Telemetry:ServiceName"] ?? "NumbatWallet.Api";
        var serviceVersion = configuration["Telemetry:ServiceVersion"] ?? "1.0.0";

        // Add activity source for tracing
        services.AddSingleton(new ActivitySource(serviceName, serviceVersion));

        // Add metrics
        services.AddSingleton<TelemetryMetrics>();

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["environment"] = configuration["Environment"] ?? "Development",
                    ["deployment.environment"] = configuration["Environment"] ?? "Development",
                    ["service.namespace"] = "NumbatWallet",
                    ["cloud.provider"] = "azure",
                    ["cloud.region"] = "australia-east"
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = (httpContext) =>
                        {
                            // Don't trace health checks
                            return !httpContext.Request.Path.StartsWithSegments("/health");
                        };
                        options.EnrichWithHttpRequest = (activity, httpRequest) =>
                        {
                            activity.SetTag("http.request.body.size", httpRequest.ContentLength);
                            activity.SetTag("http.flavor", httpRequest.Protocol);
                        };
                        options.EnrichWithHttpResponse = (activity, httpResponse) =>
                        {
                            activity.SetTag("http.response.body.size", httpResponse.ContentLength);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.FilterHttpRequestMessage = (httpRequestMessage) =>
                        {
                            // Don't trace calls to telemetry endpoints
                            return !httpRequestMessage.RequestUri?.Host.Contains("applicationinsights") ?? true;
                        };
                        options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                        {
                            activity.SetTag("http.request.method", httpRequestMessage.Method);
                            activity.SetTag("http.url", httpRequestMessage.RequestUri?.ToString());
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                        options.EnrichWithIDbCommand = (activity, command) =>
                        {
                            activity.SetTag("db.command.timeout", command.CommandTimeout);
                        };
                    })
                    .AddSource(serviceName)
                    .AddSource("NumbatWallet.Application")
                    .AddSource("NumbatWallet.Infrastructure");

                // Add exporters based on configuration
                // Console exporter would require OpenTelemetry.Exporter.Console package
                // if (configuration.GetValue<bool>("Telemetry:UseConsoleExporter"))
                // {
                //     tracing.AddConsoleExporter();
                // }

                if (!string.IsNullOrEmpty(configuration["Telemetry:OtlpEndpoint"]))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(configuration["Telemetry:OtlpEndpoint"]!);
                        options.Protocol = OtlpExportProtocol.Grpc;
                        options.Headers = configuration["Telemetry:OtlpHeaders"];
                    });
                }

                // Azure Monitor exporter would require Azure.Monitor.OpenTelemetry.Exporter package
                // if (!string.IsNullOrEmpty(configuration["ApplicationInsights:ConnectionString"]))
                // {
                //     tracing.AddAzureMonitorTraceExporter(options =>
                //     {
                //         options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
                //     });
                // }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter(serviceName)
                    .AddMeter("NumbatWallet.Application")
                    .AddMeter("NumbatWallet.Infrastructure")
                    .AddView("http.server.request.duration",
                        new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = new double[] { 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10 }
                        });

                // Add exporters
                // Console exporter would require OpenTelemetry.Exporter.Console package
                // if (configuration.GetValue<bool>("Telemetry:UseConsoleExporter"))
                // {
                //     metrics.AddConsoleExporter();
                // }

                if (!string.IsNullOrEmpty(configuration["Telemetry:OtlpEndpoint"]))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(configuration["Telemetry:OtlpEndpoint"]!);
                        options.Protocol = OtlpExportProtocol.Grpc;
                        options.Headers = configuration["Telemetry:OtlpHeaders"];
                    });
                }
            });

        return services;
    }

    public static IApplicationBuilder UseTelemetry(this IApplicationBuilder app)
    {
        // Add correlation ID middleware
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Add trace context propagation middleware
        app.UseMiddleware<TraceContextMiddleware>();

        return app;
    }
}

/// <summary>
/// Custom telemetry metrics
/// </summary>
public class TelemetryMetrics : IDisposable
{
    private readonly Meter _meter;
    private bool _disposed;
    private readonly Counter<long> _credentialsIssued;
    private readonly Counter<long> _credentialsVerified;
    private readonly Counter<long> _credentialsRevoked;
    private readonly Counter<long> _walletsCreated;
    private readonly Counter<long> _authenticationAttempts;
    private readonly Histogram<double> _operationDuration;
    private readonly ObservableGauge<int> _activeWallets;
    private int _activeWalletCount = 0;

    public TelemetryMetrics(IConfiguration configuration)
    {
        var serviceName = configuration["Telemetry:ServiceName"] ?? "NumbatWallet.Api";
        var serviceVersion = configuration["Telemetry:ServiceVersion"] ?? "1.0.0";

        _meter = new Meter(serviceName, serviceVersion);

        _credentialsIssued = _meter.CreateCounter<long>("credentials.issued", "credentials", "Number of credentials issued");
        _credentialsVerified = _meter.CreateCounter<long>("credentials.verified", "credentials", "Number of credentials verified");
        _credentialsRevoked = _meter.CreateCounter<long>("credentials.revoked", "credentials", "Number of credentials revoked");
        _walletsCreated = _meter.CreateCounter<long>("wallets.created", "wallets", "Number of wallets created");
        _authenticationAttempts = _meter.CreateCounter<long>("auth.attempts", "attempts", "Number of authentication attempts");

        _operationDuration = _meter.CreateHistogram<double>("operation.duration", "ms", "Duration of operations in milliseconds");

        _activeWallets = _meter.CreateObservableGauge("wallets.active", () => _activeWalletCount, "wallets", "Number of active wallets");
    }

    public void RecordCredentialIssued(string credentialType, string issuerId)
    {
        _credentialsIssued.Add(1, new KeyValuePair<string, object?>("type", credentialType),
            new KeyValuePair<string, object?>("issuer", issuerId));
    }

    public void RecordCredentialVerified(string credentialType, bool success)
    {
        _credentialsVerified.Add(1, new KeyValuePair<string, object?>("type", credentialType),
            new KeyValuePair<string, object?>("success", success));
    }

    public void RecordCredentialRevoked(string credentialType, string reason)
    {
        _credentialsRevoked.Add(1, new KeyValuePair<string, object?>("type", credentialType),
            new KeyValuePair<string, object?>("reason", reason));
    }

    public void RecordWalletCreated(string walletType)
    {
        _walletsCreated.Add(1, new KeyValuePair<string, object?>("type", walletType));
        Interlocked.Increment(ref _activeWalletCount);
    }

    public void RecordAuthenticationAttempt(string method, bool success)
    {
        _authenticationAttempts.Add(1, new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("success", success));
    }

    public void RecordOperationDuration(string operation, double durationMs, bool success)
    {
        _operationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("success", success));
    }

    public void UpdateActiveWalletCount(int count)
    {
        _activeWalletCount = count;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _meter?.Dispose();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Middleware for correlation ID propagation
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId;

        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationIdValue))
        {
            correlationId = correlationIdValue.ToString();
            _logger.LogDebug("Found correlation ID in request: {CorrelationId}", correlationId);
        }
        else
        {
            correlationId = Guid.NewGuid().ToString();
            _logger.LogDebug("Generated new correlation ID: {CorrelationId}", correlationId);
        }

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);

        // Add to activity tags for distributed tracing
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Middleware for trace context propagation
/// </summary>
public class TraceContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TraceContextMiddleware> _logger;

    public TraceContextMiddleware(RequestDelegate next, ILogger<TraceContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var activity = Activity.Current;

        if (activity != null)
        {
            // Add trace context to response headers
            context.Response.Headers.Append("X-Trace-Id", activity.TraceId.ToString());
            context.Response.Headers.Append("X-Span-Id", activity.SpanId.ToString());

            // Add custom tags
            activity.SetTag("user.id", context.User?.FindFirst("sub")?.Value ?? "anonymous");
            activity.SetTag("tenant.id", context.Items["TenantId"]?.ToString() ?? "default");
            activity.SetTag("request.path", context.Request.Path.ToString());
            activity.SetTag("request.method", context.Request.Method);

            _logger.LogDebug("Processing request with TraceId: {TraceId}, SpanId: {SpanId}",
                activity.TraceId, activity.SpanId);
        }

        await _next(context);
    }
}