using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace NumbatWallet.Web.Api.Middleware;

/// <summary>
/// Global exception handler for ArgumentException and ArgumentNullException
/// Converts argument validation failures to 400 BadRequest ProblemDetails response
/// </summary>
public sealed class ArgumentExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ArgumentExceptionHandler> _logger;

    public ArgumentExceptionHandler(ILogger<ArgumentExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ArgumentException and not ArgumentNullException)
        {
            return false;
        }

        _logger.LogWarning(
            exception,
            "Argument validation failed: {Message}",
            exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid Request",
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
