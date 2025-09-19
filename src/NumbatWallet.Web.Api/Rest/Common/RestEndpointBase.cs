using Carter;
using Carter.ModelBinding;
using Carter.Response;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace NumbatWallet.Web.Api.Rest.Common;

/// <summary>
/// Base class for all REST endpoint modules
/// </summary>
public abstract class RestEndpointBase : ICarterModule
{
    public virtual string RoutePrefix => "/api/v1";

    public virtual void AddRoutes(IEndpointRouteBuilder app)
    {
        // Override in derived classes
    }

    /// <summary>
    /// Standard error response for REST API
    /// </summary>
    protected static IResult HandleError(string message, int statusCode = 400)
    {
        return Results.Json(new
        {
            success = false,
            error = new
            {
                message,
                timestamp = DateTime.UtcNow,
                traceId = Guid.NewGuid().ToString()
            }
        }, statusCode: statusCode);
    }

    /// <summary>
    /// Standard success response for REST API
    /// </summary>
    protected static IResult HandleSuccess<T>(T data, string? message = null)
    {
        return Results.Json(new
        {
            success = true,
            data,
            message,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Validate request and handle validation errors
    /// </summary>
    protected static async Task<IResult?> ValidateRequest<T>(T request, IValidator<T> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }
        return null;
    }

    /// <summary>
    /// Standard paginated response
    /// </summary>
    protected static IResult HandlePaginatedSuccess<T>(IEnumerable<T> items, int page, int pageSize, int totalCount)
    {
        return Results.Json(new
        {
            success = true,
            data = items,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                hasNextPage = page * pageSize < totalCount,
                hasPreviousPage = page > 1
            },
            timestamp = DateTime.UtcNow
        });
    }
}