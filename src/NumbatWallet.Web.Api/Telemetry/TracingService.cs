using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NumbatWallet.Web.Api.Telemetry;

/// <summary>
/// Service for managing distributed tracing
/// </summary>
public interface ITracingService
{
    Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal, [CallerMemberName] string? caller = null);
    void RecordException(Exception exception, Activity? activity = null);
    void AddEvent(string name, Dictionary<string, object?>? attributes = null, Activity? activity = null);
    void SetStatus(Activity? activity, bool success, string? description = null);
}

/// <summary>
/// Implementation of tracing service
/// </summary>
public class TracingService : ITracingService
{
    private readonly ActivitySource _activitySource;
    private readonly ILogger<TracingService> _logger;
    private readonly TelemetryMetrics _metrics;

    public TracingService(
        ActivitySource activitySource,
        ILogger<TracingService> logger,
        TelemetryMetrics metrics)
    {
        _activitySource = activitySource;
        _logger = logger;
        _metrics = metrics;
    }

    public Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal, [CallerMemberName] string? caller = null)
    {
        var activity = _activitySource.StartActivity(operationName, kind);

        if (activity != null)
        {
            activity.SetTag("operation.name", operationName);
            activity.SetTag("operation.caller", caller ?? "unknown");
            activity.SetTag("operation.start_time", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            _logger.LogDebug("Started activity {OperationName} with TraceId {TraceId}",
                operationName, activity.TraceId);
        }

        return activity;
    }

    public void RecordException(Exception exception, Activity? activity = null)
    {
        activity ??= Activity.Current;

        if (activity != null)
        {
            // Add exception details as tags
            activity.SetTag("exception.type", exception.GetType().FullName);
            activity.SetTag("exception.message", exception.Message);
            if (exception.StackTrace != null)
            {
                activity.SetTag("exception.stacktrace", exception.StackTrace);
            }
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);

            _logger.LogError(exception, "Exception recorded in activity {OperationName} with TraceId {TraceId}",
                activity.OperationName, activity.TraceId);
        }
        else
        {
            _logger.LogError(exception, "Exception occurred but no activity context available");
        }
    }

    public void AddEvent(string name, Dictionary<string, object?>? attributes = null, Activity? activity = null)
    {
        activity ??= Activity.Current;

        if (activity != null)
        {
            var tags = new ActivityTagsCollection();
            if (attributes != null)
            {
                foreach (var kvp in attributes)
                {
                    tags.Add(kvp.Key, kvp.Value);
                }
            }

            activity.AddEvent(new ActivityEvent(name, tags: tags));

            _logger.LogDebug("Added event {EventName} to activity {OperationName}",
                name, activity.OperationName);
        }
    }

    public void SetStatus(Activity? activity, bool success, string? description = null)
    {
        if (activity != null)
        {
            var status = success ? ActivityStatusCode.Ok : ActivityStatusCode.Error;
            activity.SetStatus(status, description);

            // Record duration metric
            var duration = (DateTimeOffset.UtcNow - activity.StartTimeUtc).TotalMilliseconds;
            _metrics.RecordOperationDuration(activity.OperationName, duration, success);

            _logger.LogDebug("Set activity {OperationName} status to {Status}",
                activity.OperationName, status);
        }
    }
}

/// <summary>
/// Extension methods for activity enrichment
/// </summary>
public static class ActivityExtensions
{
    public static Activity? AddBaggage(this Activity? activity, string key, string value)
    {
        activity?.AddBaggage(key, value);
        return activity;
    }

    public static Activity? AddSecurityContext(this Activity? activity, HttpContext httpContext)
    {
        if (activity != null && httpContext != null)
        {
            activity.SetTag("user.id", httpContext.User?.FindFirst("sub")?.Value ?? "anonymous");
            activity.SetTag("user.name", httpContext.User?.Identity?.Name ?? "anonymous");
            activity.SetTag("user.authenticated", httpContext.User?.Identity?.IsAuthenticated ?? false);
            activity.SetTag("user.roles", string.Join(",", httpContext.User?.Claims
                .Where(c => c.Type == "role")
                .Select(c => c.Value) ?? Enumerable.Empty<string>()));
        }

        return activity;
    }

    public static Activity? AddDatabaseContext(this Activity? activity, string operation, string table, int recordCount = 0)
    {
        if (activity != null)
        {
            activity.SetTag("db.operation", operation);
            activity.SetTag("db.table", table);
            if (recordCount > 0)
            {
                activity.SetTag("db.record_count", recordCount);
            }
        }

        return activity;
    }

    public static Activity? AddHttpContext(this Activity? activity, HttpContext httpContext)
    {
        if (activity != null && httpContext != null)
        {
            activity.SetTag("http.method", httpContext.Request.Method);
            activity.SetTag("http.url", httpContext.Request.Path.ToString());
            activity.SetTag("http.scheme", httpContext.Request.Scheme);
            activity.SetTag("http.host", httpContext.Request.Host.ToString());
            activity.SetTag("http.user_agent", httpContext.Request.Headers.UserAgent.ToString());
            activity.SetTag("http.client_ip", httpContext.Connection.RemoteIpAddress?.ToString());
        }

        return activity;
    }

    public static Activity? AddBusinessContext(this Activity? activity, string entityType, string entityId, string operation)
    {
        if (activity != null)
        {
            activity.SetTag("business.entity_type", entityType);
            activity.SetTag("business.entity_id", entityId);
            activity.SetTag("business.operation", operation);
        }

        return activity;
    }
}

/// <summary>
/// Distributed tracing interceptor for database operations
/// </summary>
public class TracingDbCommandInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
{
    private readonly ITracingService _tracingService;

    public TracingDbCommandInterceptor(ITracingService tracingService)
    {
        _tracingService = tracingService;
    }

    public override Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
        System.Data.Common.DbCommand command,
        Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
        Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> result)
    {
        var activity = _tracingService.StartActivity($"db.{command.CommandType}", ActivityKind.Client);

        activity?.SetTag("db.system", "postgresql")
            .SetTag("db.operation", command.CommandType.ToString())
            .SetTag("db.statement", command.CommandText);

        return base.ReaderExecuting(command, eventData, result);
    }

    public override System.Data.Common.DbDataReader ReaderExecuted(
        System.Data.Common.DbCommand command,
        Microsoft.EntityFrameworkCore.Diagnostics.CommandExecutedEventData eventData,
        System.Data.Common.DbDataReader result)
    {
        var activity = Activity.Current;

        if (activity != null)
        {
            activity.SetTag("db.rows_affected", result.RecordsAffected);
            _tracingService.SetStatus(activity, true);
        }

        return base.ReaderExecuted(command, eventData, result);
    }

    public override void CommandFailed(
        System.Data.Common.DbCommand command,
        Microsoft.EntityFrameworkCore.Diagnostics.CommandErrorEventData eventData)
    {
        var activity = Activity.Current;

        if (activity != null && eventData.Exception != null)
        {
            _tracingService.RecordException(eventData.Exception, activity);
            _tracingService.SetStatus(activity, false, eventData.Exception.Message);
        }

        base.CommandFailed(command, eventData);
    }
}

/// <summary>
/// HTTP client handler for distributed tracing
/// </summary>
public class TracingHttpMessageHandler : DelegatingHandler
{
    private readonly ITracingService _tracingService;

    public TracingHttpMessageHandler(ITracingService tracingService)
    {
        _tracingService = tracingService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var activity = _tracingService.StartActivity(
            $"http.client {request.Method} {request.RequestUri?.Host}",
            ActivityKind.Client);

        try
        {
            if (activity != null)
            {
                // Add trace context headers for W3C Trace Context propagation
                if (!request.Headers.Contains("traceparent"))
                {
                    request.Headers.Add("traceparent", activity.Id);
                }

                if (activity.TraceStateString != null && !request.Headers.Contains("tracestate"))
                {
                    request.Headers.Add("tracestate", activity.TraceStateString);
                }

                activity.SetTag("http.method", request.Method.ToString());
                activity.SetTag("http.url", request.RequestUri?.ToString());
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (activity != null)
            {
                activity.SetTag("http.status_code", (int)response.StatusCode);
                _tracingService.SetStatus(activity, response.IsSuccessStatusCode,
                    response.IsSuccessStatusCode ? null : response.ReasonPhrase);
            }

            return response;
        }
        catch (Exception ex)
        {
            _tracingService.RecordException(ex, activity);
            _tracingService.SetStatus(activity, false, ex.Message);
            throw;
        }
        finally
        {
            activity?.Dispose();
        }
    }
}