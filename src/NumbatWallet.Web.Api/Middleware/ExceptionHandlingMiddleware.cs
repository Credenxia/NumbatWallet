using System.Net;
using System.Text.Json;
using NumbatWallet.Domain.Exceptions;
using FluentValidation;

namespace NumbatWallet.Web.Api.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = environment.IsDevelopment()
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the exception
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        // Prepare the response
        context.Response.ContentType = "application/problem+json";

        var problemDetails = CreateProblemDetails(context, exception);
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, _jsonSerializerOptions));
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            Domain.Exceptions.NotFoundException notFoundEx => new ProblemDetails
            {
                Title = "Resource Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = notFoundEx.Message,
                Type = "https://numbatwallet.wa.gov.au/errors/not-found",
                Instance = context.Request.Path
            },

            Domain.Exceptions.ConflictException conflictEx => new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = conflictEx.Message,
                Type = "https://numbatwallet.wa.gov.au/errors/conflict",
                Instance = context.Request.Path
            },

            Domain.Exceptions.UnauthorizedException unauthorizedEx => new ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = unauthorizedEx.Message,
                Type = "https://numbatwallet.wa.gov.au/errors/unauthorized",
                Instance = context.Request.Path
            },

            DomainException domainEx => new ProblemDetails
            {
                Title = "Domain Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = domainEx.Message,
                Type = "https://numbatwallet.wa.gov.au/errors/domain",
                Instance = context.Request.Path
            },

            ForbiddenException forbiddenEx => new ProblemDetails
            {
                Title = "Forbidden",
                Status = StatusCodes.Status403Forbidden,
                Detail = forbiddenEx.Message,
                Type = "https://numbatwallet.wa.gov.au/errors/forbidden",
                Instance = context.Request.Path
            },

            ValidationException validationEx => new ValidationProblemDetails
            {
                Title = "Validation Failed",
                Status = StatusCodes.Status422UnprocessableEntity,
                Detail = "One or more validation errors occurred.",
                Type = "https://numbatwallet.wa.gov.au/errors/validation",
                Instance = context.Request.Path,
                Errors = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray())
            },

            InvalidOperationException invalidOpEx => new ProblemDetails
            {
                Title = "Invalid Operation",
                Status = StatusCodes.Status400BadRequest,
                Detail = invalidOpEx.Message,
                Type = "https://numbatwallet.wa.gov.au/errors/invalid-operation",
                Instance = context.Request.Path
            },

            TimeoutException timeoutEx => new ProblemDetails
            {
                Title = "Request Timeout",
                Status = StatusCodes.Status408RequestTimeout,
                Detail = "The request took too long to complete.",
                Type = "https://numbatwallet.wa.gov.au/errors/timeout",
                Instance = context.Request.Path
            },

            _ => new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = _environment.IsDevelopment()
                    ? exception.Message
                    : "An error occurred while processing your request.",
                Type = "https://numbatwallet.wa.gov.au/errors/internal",
                Instance = context.Request.Path
            }
        };

        // Add correlation ID
        if (context.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        // Add timestamp
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        // Add tenant ID if available
        if (context.Items.TryGetValue("TenantId", out var tenantId))
        {
            problemDetails.Extensions["tenantId"] = tenantId;
        }

        // Add stack trace in development
        if (_environment.IsDevelopment() && exception.StackTrace != null)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        return problemDetails;
    }
}

// Custom exception types
// NotFoundException is now defined in NumbatWallet.Domain.Exceptions

// ConflictException is now defined in NumbatWallet.Domain.Exceptions

// UnauthorizedException is now defined as UnauthorizedException in NumbatWallet.Domain.Exceptions

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Access forbidden") : base(message) { }
    public ForbiddenException(string message, Exception innerException) : base(message, innerException) { }
}

// Extension class for validation errors
public class ValidationProblemDetails : ProblemDetails
{
    public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}