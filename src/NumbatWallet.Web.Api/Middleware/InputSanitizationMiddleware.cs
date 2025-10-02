using NumbatWallet.Web.Api.Security;

namespace NumbatWallet.Web.Api.Middleware;

/// <summary>
/// Middleware that validates request content-type and checks for basic injection attempts
/// </summary>
public class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputSanitizationMiddleware> _logger;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "application/x-www-form-urlencoded",
        "multipart/form-data",
        "text/plain"
    };

    public InputSanitizationMiddleware(
        RequestDelegate next,
        ILogger<InputSanitizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate POST, PUT, PATCH requests
        if (context.Request.Method == HttpMethods.Post ||
            context.Request.Method == HttpMethods.Put ||
            context.Request.Method == HttpMethods.Patch)
        {
            // Validate content-type
            var contentType = context.Request.ContentType;
            if (!string.IsNullOrEmpty(contentType))
            {
                var baseContentType = contentType.Split(';')[0].Trim();
                if (!AllowedContentTypes.Contains(baseContentType))
                {
                    _logger.LogWarning(
                        "Blocked request with invalid content-type: {ContentType} from {IpAddress}",
                        contentType,
                        context.Connection.RemoteIpAddress);

                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Error = "Unsupported Media Type",
                        Message = "Content-Type not supported. Allowed types: application/json"
                    });
                    return;
                }
            }

            // Check for suspicious patterns in headers
            foreach (var header in context.Request.Headers)
            {
                if (ContainsSuspiciousPattern(header.Value.ToString()))
                {
                    _logger.LogWarning(
                        "Blocked request with suspicious header pattern: {HeaderName} from {IpAddress}",
                        header.Key,
                        context.Connection.RemoteIpAddress);

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Error = "Bad Request",
                        Message = "Request contains invalid characters"
                    });
                    return;
                }
            }

            // Check query string for SQL injection patterns
            if (context.Request.QueryString.HasValue)
            {
                var queryString = context.Request.QueryString.Value ?? string.Empty;
                if (ContainsSqlInjectionPattern(queryString))
                {
                    _logger.LogWarning(
                        "Blocked request with SQL injection pattern in query string from {IpAddress}",
                        context.Connection.RemoteIpAddress);

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Error = "Bad Request",
                        Message = "Query parameters contain invalid characters"
                    });
                    return;
                }
            }
        }

        // Check for path traversal attempts
        if (context.Request.Path.HasValue)
        {
            var path = context.Request.Path.Value;
            if (path.Contains("../") || path.Contains("..\\") || path.Contains("%2e%2e"))
            {
                _logger.LogWarning(
                    "Blocked request with path traversal attempt: {Path} from {IpAddress}",
                    path,
                    context.Connection.RemoteIpAddress);

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = "Bad Request",
                    Message = "Invalid request path"
                });
                return;
            }
        }

        await _next(context);
    }

    private static bool ContainsSuspiciousPattern(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        // Check for common XSS patterns
        var suspiciousPatterns = new[]
        {
            "<script",
            "javascript:",
            "onerror=",
            "onload=",
            "eval(",
            "expression(",
            "<iframe"
        };

        return suspiciousPatterns.Any(pattern =>
            value.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsSqlInjectionPattern(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        // Check for common SQL injection patterns
        var sqlPatterns = new[]
        {
            "'; DROP TABLE",
            "'; DELETE FROM",
            "'; INSERT INTO",
            "'; UPDATE ",
            "UNION SELECT",
            "UNION ALL SELECT",
            "1=1--",
            "1=1 --",
            "OR 1=1",
            "' OR '1'='1",
            "\" OR \"1\"=\"1",
            "xp_cmdshell",
            "sp_executesql"
        };

        return sqlPatterns.Any(pattern =>
            value.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
