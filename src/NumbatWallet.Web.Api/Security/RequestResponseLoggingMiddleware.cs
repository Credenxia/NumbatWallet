using System.Diagnostics;
using System.Text;

namespace NumbatWallet.Web.Api.Security;

/// <summary>
/// Middleware for logging HTTP requests and responses
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly RequestResponseLoggingOptions _options;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        RequestResponseLoggingOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldLog(context))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();

        // Log request
        var requestLog = await LogRequest(context, requestId);

        // Capture response
        var originalBodyStream = context.Response.Body;

        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);

            // Log response
            stopwatch.Stop();
            await LogResponse(context, requestId, responseBodyStream, stopwatch.ElapsedMilliseconds);

            // Copy response body back to original stream
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogError(context, requestId, ex, stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldLog(HttpContext context)
    {
        // Skip logging for excluded paths
        var path = context.Request.Path.ToString();

        if (_options.ExcludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Skip logging for health checks
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private async Task<RequestLog> LogRequest(HttpContext context, string requestId)
    {
        context.Request.EnableBuffering();

        var request = context.Request;
        var requestLog = new RequestLog
        {
            Id = requestId,
            Timestamp = DateTime.UtcNow,
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.ToString(),
            Headers = _options.LogHeaders ? GetSafeHeaders(request.Headers) : null,
            ClientIp = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = request.Headers.UserAgent.ToString(),
            CorrelationId = context.Items["CorrelationId"]?.ToString(),
            UserId = context.User?.FindFirst("sub")?.Value
        };

        if (_options.LogRequestBody && request.ContentLength > 0 && request.ContentLength <= _options.MaxBodySize)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            requestLog.Body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        if (_options.LogLevel == LogLevel.Debug)
        {
            _logger.LogDebug("HTTP Request {RequestId}: {Method} {Path} from {ClientIp}",
                requestId, request.Method, request.Path, requestLog.ClientIp);
        }
        else
        {
            _logger.LogInformation("HTTP Request {RequestId}: {Method} {Path}",
                requestId, request.Method, request.Path);
        }

        if (_options.LogRequestBody && !string.IsNullOrEmpty(requestLog.Body))
        {
            _logger.LogDebug("Request Body: {Body}", SanitizeBody(requestLog.Body));
        }

        return requestLog;
    }

    private async Task LogResponse(HttpContext context, string requestId, Stream responseBodyStream, long elapsedMs)
    {
        responseBodyStream.Seek(0, SeekOrigin.Begin);

        var responseLog = new ResponseLog
        {
            RequestId = requestId,
            Timestamp = DateTime.UtcNow,
            StatusCode = context.Response.StatusCode,
            Headers = _options.LogHeaders ? GetSafeHeaders(context.Response.Headers) : null,
            ElapsedMs = elapsedMs
        };

        if (_options.LogResponseBody && responseBodyStream.Length > 0 && responseBodyStream.Length <= _options.MaxBodySize)
        {
            using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
            responseLog.Body = await reader.ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);
        }

        var logLevel = context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(logLevel, "HTTP Response {RequestId}: {StatusCode} in {ElapsedMs}ms",
            requestId, context.Response.StatusCode, elapsedMs);

        if (_options.LogResponseBody && !string.IsNullOrEmpty(responseLog.Body))
        {
            _logger.LogDebug("Response Body: {Body}", SanitizeBody(responseLog.Body));
        }

        // Log slow requests
        if (elapsedMs > _options.SlowRequestThresholdMs)
        {
            _logger.LogWarning("Slow request detected {RequestId}: {Method} {Path} took {ElapsedMs}ms",
                requestId, context.Request.Method, context.Request.Path, elapsedMs);
        }
    }

    private void LogError(HttpContext context, string requestId, Exception exception, long elapsedMs)
    {
        _logger.LogError(exception, "HTTP Request {RequestId} failed after {ElapsedMs}ms: {Method} {Path}",
            requestId, elapsedMs, context.Request.Method, context.Request.Path);
    }

    private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        var safeHeaders = new Dictionary<string, string>();

        foreach (var header in headers)
        {
            if (_options.SensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                safeHeaders[header.Key] = "[REDACTED]";
            }
            else
            {
                safeHeaders[header.Key] = header.Value.ToString();
            }
        }

        return safeHeaders;
    }

    private string SanitizeBody(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return body;
        }

        // Sanitize sensitive data patterns
        foreach (var pattern in _options.SensitiveDataPatterns)
        {
            body = System.Text.RegularExpressions.Regex.Replace(
                body,
                pattern.Pattern,
                pattern.Replacement,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return body;
    }
}

/// <summary>
/// Options for request/response logging
/// </summary>
public class RequestResponseLoggingOptions
{
    public bool LogRequestBody { get; set; } = true;
    public bool LogResponseBody { get; set; } = true;
    public bool LogHeaders { get; set; } = true;
    public int MaxBodySize { get; set; } = 32768; // 32KB
    public int SlowRequestThresholdMs { get; set; } = 1000; // 1 second
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/metrics",
        "/swagger",
        "/favicon.ico"
    };

    public List<string> SensitiveHeaders { get; set; } = new()
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key",
        "X-Auth-Token"
    };

    public List<SensitiveDataPattern> SensitiveDataPatterns { get; set; } = new()
    {
        new SensitiveDataPattern { Pattern = @"""password""\s*:\s*""[^""]+""", Replacement = @"""password"":""[REDACTED]""" },
        new SensitiveDataPattern { Pattern = @"""secret""\s*:\s*""[^""]+""", Replacement = @"""secret"":""[REDACTED]""" },
        new SensitiveDataPattern { Pattern = @"""token""\s*:\s*""[^""]+""", Replacement = @"""token"":""[REDACTED]""" },
        new SensitiveDataPattern { Pattern = @"""apiKey""\s*:\s*""[^""]+""", Replacement = @"""apiKey"":""[REDACTED]""" },
        new SensitiveDataPattern { Pattern = @"""creditCard""\s*:\s*""[^""]+""", Replacement = @"""creditCard"":""[REDACTED]""" },
        new SensitiveDataPattern { Pattern = @"""ssn""\s*:\s*""[^""]+""", Replacement = @"""ssn"":""[REDACTED]""" },
        new SensitiveDataPattern { Pattern = @"""taxFileNumber""\s*:\s*""[^""]+""", Replacement = @"""taxFileNumber"":""[REDACTED]""" }
    };
}

/// <summary>
/// Pattern for sensitive data sanitization
/// </summary>
public class SensitiveDataPattern
{
    public required string Pattern { get; set; }
    public required string Replacement { get; set; }
}

/// <summary>
/// Request log model
/// </summary>
public class RequestLog
{
    public required string Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Method { get; set; }
    public required string Path { get; set; }
    public string? QueryString { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? Body { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
}

/// <summary>
/// Response log model
/// </summary>
public class ResponseLog
{
    public required string RequestId { get; set; }
    public DateTime Timestamp { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? Body { get; set; }
    public long ElapsedMs { get; set; }
}

/// <summary>
/// Extensions for request/response logging
/// </summary>
public static class RequestResponseLoggingExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(
        this IApplicationBuilder app,
        Action<RequestResponseLoggingOptions>? configureOptions = null)
    {
        var options = new RequestResponseLoggingOptions();
        configureOptions?.Invoke(options);

        return app.UseMiddleware<RequestResponseLoggingMiddleware>(options);
    }

    public static IServiceCollection AddRequestResponseLogging(
        this IServiceCollection services,
        Action<RequestResponseLoggingOptions>? configureOptions = null)
    {
        var options = new RequestResponseLoggingOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);

        return services;
    }
}