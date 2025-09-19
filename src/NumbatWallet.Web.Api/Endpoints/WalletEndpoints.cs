using Carter;
using Microsoft.AspNetCore.Mvc;
using NumbatWallet.Application.Commands.Wallets;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Application.Queries.Wallets;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.Endpoints;

public class WalletEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/wallets")
            .RequireAuthorization()
            .WithTags("Wallets")
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        // GET /api/v1/wallets
        group.MapGet("/", GetMyWallets)
            .WithName("GetMyWallets")
            .WithOpenApi()
            .Produces<IEnumerable<WalletDto>>(StatusCodes.Status200OK);

        // GET /api/v1/wallets/{id}
        group.MapGet("/{id:guid}", GetWalletById)
            .WithName("GetWalletById")
            .WithOpenApi()
            .Produces<WalletDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/wallets
        group.MapPost("/", CreateWallet)
            .WithName("CreateWallet")
            .WithOpenApi()
            .Accepts<CreateWalletRequest>("application/json")
            .Produces<WalletDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        // PUT /api/v1/wallets/{id}/settings
        group.MapPut("/{id:guid}/settings", UpdateWalletSettings)
            .WithName("UpdateWalletSettings")
            .WithOpenApi()
            .Accepts<UpdateWalletSettingsRequest>("application/json")
            .Produces<WalletDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        // POST /api/v1/wallets/{id}/backup
        group.MapPost("/{id:guid}/backup", BackupWallet)
            .WithName("BackupWallet")
            .WithOpenApi()
            .Produces<BackupResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/wallets/restore
        group.MapPost("/restore", RestoreWallet)
            .WithName("RestoreWallet")
            .WithOpenApi()
            .Accepts<RestoreWalletRequest>("application/json")
            .Produces<WalletDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        // POST /api/v1/wallets/{id}/lock
        group.MapPost("/{id:guid}/lock", LockWallet)
            .WithName("LockWallet")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/wallets/{id}/unlock
        group.MapPost("/{id:guid}/unlock", UnlockWallet)
            .WithName("UnlockWallet")
            .WithOpenApi()
            .Accepts<UnlockWalletRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/v1/wallets/{id}/credentials
        group.MapGet("/{id:guid}/credentials", GetWalletCredentials)
            .WithName("GetWalletCredentials")
            .WithOpenApi()
            .Produces<IEnumerable<CredentialDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/v1/wallets/{id}/statistics
        group.MapGet("/{id:guid}/statistics", GetWalletStatistics)
            .WithName("GetWalletStatistics")
            .WithOpenApi()
            .Produces<WalletStatistics>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/v1/wallets/{id}
        group.MapDelete("/{id:guid}", DeleteWallet)
            .WithName("DeleteWallet")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetMyWallets(
        ClaimsPrincipal user,
        [FromServices] IQueryHandler<GetMyWalletsQuery, IEnumerable<WalletDto>> handler,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var query = new GetMyWalletsQuery(userId);
        var result = await handler.HandleAsync(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetWalletById(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        [FromServices] IQueryHandler<GetWalletByIdQuery, WalletDto> handler,
        [FromServices] IWalletService walletService,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // Check if user has access to this wallet
        if (!await walletService.UserHasAccessAsync(id, userId, cancellationToken))
        {
            return Results.Forbid();
        }

        var query = new GetWalletByIdQuery(id);
        var result = await handler.HandleAsync(query, cancellationToken);

        return result != null
            ? Results.Ok(result)
            : Results.NotFound(new { message = $"Wallet with ID {id} not found" });
    }

    private static async Task<IResult> CreateWallet(
        [FromBody] CreateWalletRequest request,
        ClaimsPrincipal user,
        [FromServices] ICommandHandler<CreateWalletCommand, WalletDto> handler,
        [FromServices] IValidator<CreateWalletCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var command = new CreateWalletCommand(
            request.PersonId,
            request.Name ?? "My Wallet",
            userId);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await handler.HandleAsync(command, cancellationToken);
        return Results.Created($"/api/v1/wallets/{result.Id}", result);
    }

    private static async Task<IResult> UpdateWalletSettings(
        [FromRoute] Guid id,
        [FromBody] UpdateWalletSettingsRequest request,
        ClaimsPrincipal user,
        [FromServices] ICommandHandler<UpdateWalletSettingsCommand, WalletDto> handler,
        [FromServices] IValidator<UpdateWalletSettingsCommand> validator,
        [FromServices] IWalletService walletService,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        if (!await walletService.UserHasAccessAsync(id, userId, cancellationToken))
        {
            return Results.Forbid();
        }

        var command = new UpdateWalletSettingsCommand(
            id,
            request.Name,
            request.EnableBiometric,
            request.RequirePinForAccess,
            request.AutoLockTimeoutMinutes);

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
            return Results.NotFound(new { message = $"Wallet with ID {id} not found" });
        }
    }

    private static async Task<IResult> BackupWallet(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        [FromServices] ICommandHandler<BackupWalletCommand, BackupResult> handler,
        [FromServices] IWalletService walletService,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        if (!await walletService.UserHasAccessAsync(id, userId, cancellationToken))
        {
            return Results.Forbid();
        }

        var command = new BackupWalletCommand(id);

        try
        {
            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (NotFoundException)
        {
            return Results.NotFound(new { message = $"Wallet with ID {id} not found" });
        }
    }

    private static async Task<IResult> RestoreWallet(
        [FromBody] RestoreWalletRequest request,
        ClaimsPrincipal user,
        [FromServices] ICommandHandler<RestoreWalletCommand, WalletDto> handler,
        [FromServices] IValidator<RestoreWalletCommand> validator,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var command = new RestoreWalletCommand(
            request.BackupData,
            request.RecoveryPhrase,
            request.Pin);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await handler.HandleAsync(command, cancellationToken);
        return Results.Created($"/api/v1/wallets/{result.Id}", result);
    }

    private static async Task<IResult> LockWallet(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        [FromServices] IWalletService walletService,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        if (!await walletService.UserHasAccessAsync(id, userId, cancellationToken))
        {
            return Results.Forbid();
        }

        try
        {
            await walletService.LockWalletAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (NotFoundException)
        {
            return Results.NotFound(new { message = $"Wallet with ID {id} not found" });
        }
    }

    private static async Task<IResult> UnlockWallet(
        [FromRoute] Guid id,
        [FromBody] UnlockWalletRequest request,
        ClaimsPrincipal user,
        [FromServices] IWalletService walletService,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        if (!await walletService.UserHasAccessAsync(id, userId, cancellationToken))
        {
            return Results.Forbid();
        }

        try
        {
            var success = await walletService.UnlockWalletAsync(id, request.Pin, cancellationToken);
            return success
                ? Results.NoContent()
                : Results.BadRequest(new { message = "Invalid PIN" });
        }
        catch (NotFoundException)
        {
            return Results.NotFound(new { message = $"Wallet with ID {id} not found" });
        }
    }

    private static async Task<IResult> GetWalletCredentials(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        [FromServices] ICredentialService credentialService,
        [FromServices] IWalletService walletService,
        [AsParameters] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        if (!await walletService.UserHasAccessAsync(id, userId, cancellationToken))
        {
            return Results.Forbid();
        }

        try
        {
            var credentials = await credentialService.GetByWalletIdAsync(id, cancellationToken);
            var paginatedResults = credentials
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize);

            return Results.Ok(paginatedResults);
        }
        catch (NotFoundException)
        {
            return Results.NotFound(new { message = $"Wallet with ID {id} not found" });
        }
    }

    private static async Task<IResult> GetWalletStatistics(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        [FromServices] IWalletService walletService,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        if (!await walletService.UserHasAccessAsync(id, userId, cancellationToken))
        {
            return Results.Forbid();
        }

        try
        {
            var statistics = await walletService.GetWalletStatisticsAsync(id, cancellationToken);
            return Results.Ok(statistics);
        }
        catch (NotFoundException)
        {
            return Results.NotFound(new { message = $"Wallet with ID {id} not found" });
        }
    }

    private static async Task<IResult> DeleteWallet(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        [FromServices] ICommandHandler<DeleteWalletCommand, bool> handler,
        [FromServices] IWalletService walletService,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        if (!await walletService.UserHasAccessAsync(id, userId, cancellationToken))
        {
            return Results.Forbid();
        }

        var command = new DeleteWalletCommand(id);

        try
        {
            await handler.HandleAsync(command, cancellationToken);
            return Results.NoContent();
        }
        catch (NotFoundException)
        {
            return Results.NotFound(new { message = $"Wallet with ID {id} not found" });
        }
    }
}

// Request/Response DTOs
public record CreateWalletRequest(
    Guid PersonId,
    string? Name);

public record UpdateWalletSettingsRequest(
    string? Name,
    bool? EnableBiometric,
    bool? RequirePinForAccess,
    int? AutoLockTimeoutMinutes);

public record RestoreWalletRequest(
    string BackupData,
    string RecoveryPhrase,
    string? Pin);

public record UnlockWalletRequest(
    string Pin);

public record BackupResult(
    string BackupData,
    string BackupId,
    DateTime CreatedAt,
    bool IsEncrypted);

public record WalletStatistics(
    int TotalCredentials,
    int ActiveCredentials,
    int ExpiredCredentials,
    int RevokedCredentials,
    DateTime LastAccessedAt,
    Dictionary<string, int> CredentialsByType);