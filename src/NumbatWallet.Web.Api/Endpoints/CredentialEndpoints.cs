using Carter;
using Microsoft.AspNetCore.Mvc;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Application.Queries.Credentials;
using NumbatWallet.Domain.Enums;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.Endpoints;

public class CredentialEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/credentials")
            .RequireAuthorization()
            .WithTags("Credentials")
            .RequireRateLimiting("api");

        // POST /api/v1/credentials/issue
        group.MapPost("/issue", IssueCredential)
            .WithName("IssueCredential")
            .WithOpenApi()
            .RequireAuthorization("CanIssueCredentials")
            .Accepts<IssueCredentialRequest>("application/json")
            .Produces<CredentialDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .RequireRateLimiting("expensive");

        // POST /api/v1/credentials/bulk-issue
        group.MapPost("/bulk-issue", BulkIssueCredentials)
            .WithName("BulkIssueCredentials")
            .WithOpenApi()
            .RequireAuthorization("CanIssueCredentials")
            .Accepts<BulkIssueCredentialsRequest>("application/json")
            .Produces<BulkIssueResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .RequireRateLimiting("expensive");

        // GET /api/v1/credentials/{id}
        group.MapGet("/{id:guid}", GetCredentialById)
            .WithName("GetCredentialById")
            .WithOpenApi()
            .Produces<CredentialDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/credentials/{id}/verify
        group.MapPost("/{id:guid}/verify", VerifyCredential)
            .WithName("VerifyCredential")
            .WithOpenApi()
            .Produces<VerificationResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/credentials/{id}/revoke
        group.MapPost("/{id:guid}/revoke", RevokeCredential)
            .WithName("RevokeCredential")
            .WithOpenApi()
            .RequireAuthorization("CanRevokeCredentials")
            .Accepts<RevokeCredentialRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        // POST /api/v1/credentials/{id}/share
        group.MapPost("/{id:guid}/share", ShareCredential)
            .WithName("ShareCredential")
            .WithOpenApi()
            .Accepts<ShareCredentialRequest>("application/json")
            .Produces<ShareCredentialResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        // GET /api/v1/credentials/types
        group.MapGet("/types", GetCredentialTypes)
            .WithName("GetCredentialTypes")
            .WithOpenApi()
            .AllowAnonymous()
            .Produces<IEnumerable<CredentialTypeInfo>>(StatusCodes.Status200OK);

        // GET /api/v1/credentials/search
        group.MapGet("/search", SearchCredentials)
            .WithName("SearchCredentials")
            .WithOpenApi()
            .RequireAuthorization("AdminOrOfficer")
            .Produces<IEnumerable<CredentialDto>>(StatusCodes.Status200OK);

        // POST /api/v1/credentials/validate-jwt-vc
        group.MapPost("/validate-jwt-vc", ValidateJwtVc)
            .WithName("ValidateJwtVc")
            .WithOpenApi()
            .AllowAnonymous()
            .Accepts<ValidateJwtVcRequest>("application/json")
            .Produces<JwtVcValidationResult>(StatusCodes.Status200OK);

        // POST /api/v1/credentials/present
        group.MapPost("/present", PresentCredential)
            .WithName("PresentCredential")
            .WithOpenApi()
            .Accepts<PresentCredentialRequest>("application/json")
            .Produces<PresentationResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> IssueCredential(
        [FromBody] IssueCredentialRequest request,
        ClaimsPrincipal user,
        [FromServices] ICommandHandler<IssueCredentialCommand, CredentialDto> handler,
        [FromServices] IValidator<IssueCredentialCommand> validator,
        CancellationToken cancellationToken)
    {
        var issuerId = user.FindFirst("sub")?.Value
            ?? throw new Domain.Exceptions.UnauthorizedException("Issuer not authenticated");

        var command = new IssueCredentialCommand(
            request.WalletId,
            request.CredentialType,
            request.Subject,
            request.Claims,
            request.ValidFrom ?? DateTime.UtcNow,
            request.ValidUntil,
            issuerId,
            request.IssuerOrganizationId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await handler.HandleAsync(command, cancellationToken);
        return Results.Created($"/api/v1/credentials/{result.Id}", result);
    }

    private static async Task<IResult> BulkIssueCredentials(
        [FromBody] BulkIssueCredentialsRequest request,
        ClaimsPrincipal user,
        [FromServices] ICommandHandler<BulkIssueCredentialsCommand, BulkIssueResult> handler,
        [FromServices] IValidator<BulkIssueCredentialsCommand> validator,
        CancellationToken cancellationToken)
    {
        var issuerId = user.FindFirst("sub")?.Value
            ?? throw new Domain.Exceptions.UnauthorizedException("Issuer not authenticated");

        var command = new BulkIssueCredentialsCommand(
            request.WalletIds,
            request.CredentialType,
            request.Template,
            issuerId,
            request.IssuerOrganizationId,
            request.ValidFrom ?? DateTime.UtcNow,
            request.ValidUntil);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await handler.HandleAsync(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCredentialById(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        [FromServices] IQueryHandler<GetCredentialByIdQuery, CredentialDto?> handler,
        [FromServices] ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // Check if user has access to this credential
        if (!await credentialService.UserHasAccessAsync(id, userId, cancellationToken))
        {
            return Results.Forbid();
        }

        var query = new GetCredentialByIdQuery(id);
        var result = await handler.HandleAsync(query, cancellationToken);

        return result != null
            ? Results.Ok(result)
            : Results.NotFound(new { message = $"Credential with ID {id} not found" });
    }

    private static async Task<IResult> VerifyCredential(
        [FromRoute] Guid id,
        [FromServices] ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await credentialService.VerifyCredentialAsync(id, cancellationToken);
            return Results.Ok(result);
        }
        catch (NotFoundException)
        {
            return Results.NotFound(new { message = $"Credential with ID {id} not found" });
        }
    }

    private static async Task<IResult> RevokeCredential(
        [FromRoute] Guid id,
        [FromBody] RevokeCredentialRequest request,
        ClaimsPrincipal user,
        [FromServices] ICommandHandler<RevokeCredentialCommand, bool> handler,
        [FromServices] IValidator<RevokeCredentialCommand> validator,
        CancellationToken cancellationToken)
    {
        var revokerId = user.FindFirst("sub")?.Value
            ?? throw new Domain.Exceptions.UnauthorizedException("User not authenticated");

        var command = new RevokeCredentialCommand(id, request.Reason, revokerId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            await handler.HandleAsync(command, cancellationToken);
            return Results.NoContent();
        }
        catch (NotFoundException)
        {
            return Results.NotFound(new { message = $"Credential with ID {id} not found" });
        }
    }

    private static async Task<IResult> ShareCredential(
        [FromRoute] Guid id,
        [FromBody] ShareCredentialRequest request,
        ClaimsPrincipal user,
        [FromServices] ICommandHandler<ShareCredentialCommand, NumbatWallet.Application.Commands.Credentials.ShareCredentialResult> handler,
        [FromServices] IValidator<ShareCredentialCommand> validator,
        [FromServices] ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // Check if user has access to this credential
        if (!await credentialService.UserHasAccessAsync(id, userId, cancellationToken))
        {
            return Results.Forbid();
        }

        var command = new ShareCredentialCommand(
            id,
            request.RecipientEmail,
            request.ExpiresInMinutes ?? 60,
            request.RequirePin,
            request.Pin);

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
            return Results.NotFound(new { message = $"Credential with ID {id} not found" });
        }
    }

    private static async Task<IResult> GetCredentialTypes(
        [FromServices] ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        var types = await credentialService.GetAvailableCredentialTypesAsync(cancellationToken);
        return Results.Ok(types);
    }

    private static async Task<IResult> SearchCredentials(
        [AsParameters] CredentialSearchRequest searchRequest,
        [FromServices] IQueryHandler<SearchCredentialsQuery, IEnumerable<CredentialDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new SearchCredentialsQuery(
            searchRequest.SearchTerm,
            searchRequest.CredentialType,
            searchRequest.IssuerId,
            searchRequest.WalletId,
            searchRequest.IsActive,
            searchRequest.IssuedAfter,
            searchRequest.IssuedBefore);

        var result = await handler.HandleAsync(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> ValidateJwtVc(
        [FromBody] ValidateJwtVcRequest request,
        [FromServices] ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        var result = await credentialService.ValidateJwtVcAsync(request.Token, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> PresentCredential(
        [FromBody] PresentCredentialRequest request,
        ClaimsPrincipal user,
        [FromServices] ICommandHandler<PresentCredentialCommand, NumbatWallet.Application.Commands.Credentials.PresentationResult> handler,
        [FromServices] IValidator<PresentCredentialCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var command = new PresentCredentialCommand(
            request.CredentialId,
            request.VerifierId,
            request.Purpose,
            request.SelectiveDisclosure);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await handler.HandleAsync(command, cancellationToken);
        return Results.Ok(result);
    }
}

// Request/Response DTOs
public record IssueCredentialRequest(
    Guid WalletId,
    CredentialType CredentialType,
    string Subject,
    Dictionary<string, object> Claims,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    Guid IssuerOrganizationId);

public record BulkIssueCredentialsRequest(
    List<Guid> WalletIds,
    CredentialType CredentialType,
    Dictionary<string, object> Template,
    Guid IssuerOrganizationId,
    DateTime? ValidFrom,
    DateTime? ValidUntil);

public record RevokeCredentialRequest(
    string Reason);

public record ShareCredentialRequest(
    string RecipientEmail,
    int? ExpiresInMinutes,
    bool RequirePin,
    string? Pin);

public record ValidateJwtVcRequest(
    string Token);

public record PresentCredentialRequest(
    Guid CredentialId,
    string VerifierId,
    string Purpose,
    List<string>? SelectiveDisclosure);

public record CredentialSearchRequest(
    [FromQuery] string? SearchTerm,
    [FromQuery] CredentialType? CredentialType,
    [FromQuery] Guid? IssuerId,
    [FromQuery] Guid? WalletId,
    [FromQuery] bool? IsActive,
    [FromQuery] DateTime? IssuedAfter,
    [FromQuery] DateTime? IssuedBefore);

public record CredentialTypeInfo(
    string Type,
    string Name,
    string Description,
    List<string> RequiredClaims,
    List<string> OptionalClaims,
    int DefaultValidityDays);

public record VerificationResult(
    bool IsValid,
    string? Issuer,
    DateTime? IssuedAt,
    DateTime? ExpiresAt,
    List<string> Errors,
    Dictionary<string, object> Claims);

public record JwtVcValidationResult(
    bool IsValid,
    string? Subject,
    string? Issuer,
    DateTime? IssuedAt,
    DateTime? ExpiresAt,
    Dictionary<string, object> Claims,
    List<string> ValidationErrors);

// Note: These DTOs are duplicated from Application layer for endpoint responses
// Consider consolidating in a shared location