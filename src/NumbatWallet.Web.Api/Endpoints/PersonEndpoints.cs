using Carter;
using Microsoft.AspNetCore.Mvc;
using NumbatWallet.Application.Commands.Persons;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Application.Queries.Persons;

namespace NumbatWallet.Web.Api.Endpoints;

public class PersonEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/persons")
            .RequireAuthorization()
            .WithTags("Persons");

        // GET /api/v1/persons
        group.MapGet("/", GetAllPersons)
            .WithName("GetAllPersons")
            .WithOpenApi()
            .RequireAuthorization("AdminOrOfficer")
            .Produces<IEnumerable<PersonDto>>(StatusCodes.Status200OK);

        // GET /api/v1/persons/{id}
        group.MapGet("/{id:guid}", GetPersonById)
            .WithName("GetPersonById")
            .WithOpenApi()
            .Produces<PersonDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/v1/persons/search
        group.MapGet("/search", SearchPersons)
            .WithName("SearchPersons")
            .WithOpenApi()
            .RequireAuthorization("AdminOrOfficer")
            .Produces<IEnumerable<PersonDto>>(StatusCodes.Status200OK);

        // POST /api/v1/persons
        group.MapPost("/", CreatePerson)
            .WithName("CreatePerson")
            .WithOpenApi()
            .RequireAuthorization("AdminOrOfficer")
            .Accepts<CreatePersonCommand>("application/json")
            .Produces<PersonDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        // PUT /api/v1/persons/{id}
        group.MapPut("/{id:guid}", UpdatePerson)
            .WithName("UpdatePerson")
            .WithOpenApi()
            .Accepts<UpdatePersonCommand>("application/json")
            .Produces<PersonDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        // DELETE /api/v1/persons/{id}
        group.MapDelete("/{id:guid}", DeletePerson)
            .WithName("DeletePerson")
            .WithOpenApi()
            .RequireAuthorization("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/persons/{id}/verify-identity
        group.MapPost("/{id:guid}/verify-identity", VerifyIdentity)
            .WithName("VerifyPersonIdentity")
            .WithOpenApi()
            .RequireAuthorization("AdminOrOfficer")
            .Accepts<VerifyIdentityRequest>("application/json")
            .Produces<VerificationResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAllPersons(
        [FromServices] IQueryHandler<GetAllPersonsQuery, IEnumerable<PersonDto>> handler,
        [AsParameters] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var query = new GetAllPersonsQuery(pagination.Page, pagination.PageSize);
        var result = await handler.HandleAsync(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPersonById(
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetPersonByIdQuery, PersonDto?> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetPersonByIdQuery(id);
        var result = await handler.HandleAsync(query, cancellationToken);

        return result != null
            ? Results.Ok(result)
            : Results.NotFound(new { message = $"Person with ID {id} not found" });
    }

    private static async Task<IResult> SearchPersons(
        [AsParameters] PersonSearchRequest searchRequest,
        [FromServices] IQueryHandler<SearchPersonsQuery, IEnumerable<PersonDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new SearchPersonsQuery(
            searchRequest.SearchTerm,
            null,
            1,
            20);

        var result = await handler.HandleAsync(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreatePerson(
        [FromBody] CreatePersonCommand command,
        [FromServices] ICommandHandler<CreatePersonCommand, PersonDto> handler,
        [FromServices] IValidator<CreatePersonCommand> validator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await handler.HandleAsync(command, cancellationToken);

        return Results.Created($"/api/v1/persons/{result.Id}", result);
    }

    private static async Task<IResult> UpdatePerson(
        [FromRoute] Guid id,
        [FromBody] UpdatePersonCommand command,
        [FromServices] ICommandHandler<UpdatePersonCommand, PersonDto> handler,
        [FromServices] IValidator<UpdatePersonCommand> validator,
        CancellationToken cancellationToken)
    {
        if (command.Id != id)
        {
            return Results.BadRequest(new { message = "ID mismatch between route and body" });
        }

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (NotFoundException)
        {
            return Results.NotFound(new { message = $"Person with ID {id} not found" });
        }
    }

    private static async Task<IResult> DeletePerson(
        [FromRoute] Guid id,
        [FromServices] ICommandHandler<DeletePersonCommand, bool> handler,
        CancellationToken cancellationToken)
    {
        var command = new DeletePersonCommand(id);

        try
        {
            await handler.HandleAsync(command, cancellationToken);
            return Results.NoContent();
        }
        catch (NotFoundException)
        {
            return Results.NotFound(new { message = $"Person with ID {id} not found" });
        }
    }

    private static async Task<IResult> VerifyIdentity(
        [FromRoute] Guid id,
        [FromBody] VerifyIdentityRequest request,
        [FromServices] IPersonService personService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await personService.VerifyIdentityAsync(id, request, cancellationToken);
            return Results.Ok(result);
        }
        catch (NotFoundException)
        {
            return Results.NotFound(new { message = $"Person with ID {id} not found" });
        }
    }
}

// Request/Response DTOs
public record PaginationRequest(
    [FromQuery] int Page = 1,
    [FromQuery] int PageSize = 20);

public record PersonSearchRequest(
    [FromQuery] string? SearchTerm,
    [FromQuery] string? Email,
    [FromQuery] string? PhoneNumber);

public record VerifyIdentityRequest(
    string DocumentType,
    string DocumentNumber,
    string? DocumentImage,
    Dictionary<string, string> AdditionalData);

public record IdentityVerificationResult(
    bool IsVerified,
    string? VerificationMethod,
    DateTime VerifiedAt,
    Dictionary<string, object> Details);