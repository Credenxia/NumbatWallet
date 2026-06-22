using Carter;
using NumbatWallet.Application.Commands.Passes;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Web.Api.Endpoints;

public class WalletPassEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/passes")
            .RequireAuthorization()
            .WithTags("Wallet Passes");

        // POST /api/v1/passes/apple/{tenantId}/{personId}
        group.MapPost("/apple/{tenantId:guid}/{personId:guid}", GenerateApplePass)
            .WithName("GenerateApplePass")
            .WithOpenApi()
            .Produces<WalletPackageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/passes/google/{tenantId}/{personId}
        group.MapPost("/google/{tenantId:guid}/{personId:guid}", GenerateGooglePass)
            .WithName("GenerateGooglePass")
            .WithOpenApi()
            .Produces<WalletPackageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/passes/apple/{tenantId}/{personId}/download
        group.MapPost("/apple/{tenantId:guid}/{personId:guid}/download", DownloadApplePass)
            .WithName("DownloadApplePass")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK, contentType: "application/vnd.apple.pkpass")
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GenerateApplePass(
        Guid tenantId,
        Guid personId,
        GeneratePassRequest? request,
        ICommandHandler<GenerateApplePassCommand, WalletPackageDto> handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateApplePassCommand
            {
                TenantId = tenantId,
                PersonId = personId,
                TemplateId = request?.TemplateId,
                AdditionalData = request?.AdditionalData ?? []
            };

            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GenerateGooglePass(
        Guid tenantId,
        Guid personId,
        GeneratePassRequest? request,
        ICommandHandler<GenerateGooglePassCommand, WalletPackageDto> handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateGooglePassCommand
            {
                TenantId = tenantId,
                PersonId = personId,
                TemplateId = request?.TemplateId,
                AdditionalData = request?.AdditionalData ?? []
            };

            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DownloadApplePass(
        Guid tenantId,
        Guid personId,
        GeneratePassRequest? request,
        ICommandHandler<GenerateApplePassCommand, WalletPackageDto> handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateApplePassCommand
            {
                TenantId = tenantId,
                PersonId = personId,
                TemplateId = request?.TemplateId,
                AdditionalData = request?.AdditionalData ?? []
            };

            var result = await handler.HandleAsync(command, cancellationToken);

            if (result.PackageData is null)
            {
                return Results.BadRequest(new { error = "Failed to generate pass data" });
            }

            return Results.File(
                result.PackageData,
                "application/vnd.apple.pkpass",
                result.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

public record GeneratePassRequest
{
    public Guid? TemplateId { get; init; }
    public Dictionary<string, object> AdditionalData { get; init; } = [];
}
