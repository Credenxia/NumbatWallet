using Microsoft.AspNetCore.Diagnostics;

namespace NumbatWallet.Web.Api.Middleware;

/// <summary>
/// Global exception handler for NotFoundException
/// Converts NotFoundException to 404 ProblemDetails response
/// </summary>
public sealed class NotFoundExceptionHandler : IExceptionHandler
{
    private readonly ILogger<NotFoundExceptionHandler> _logger;

    public NotFoundExceptionHandler(ILogger<NotFoundExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not Application.Common.Exceptions.NotFoundException notFoundException)
        {
            return false;
        }

        _logger.LogWarning(
            notFoundException,
            "Resource not found: {Message}",
            notFoundException.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource Not Found",
            Detail = notFoundException.Message,
            Instance = httpContext.Request.Path,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
