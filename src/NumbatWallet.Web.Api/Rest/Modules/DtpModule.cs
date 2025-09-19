using Carter;
using Carter.ModelBinding;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Web.Api.Rest.Common;
using NumbatWallet.Web.Api.Rest.Validators;

namespace NumbatWallet.Web.Api.Rest.Modules;

/// <summary>
/// DTP (Digital Trust Protocol) adapter endpoints for legacy integration
/// </summary>
public class DtpModule : RestEndpointBase
{
    public override string RoutePrefix => "/api/v1/dtp";

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix)
            .WithTags("DTP Adapter")
            .RequireAuthorization();

        // Wallet endpoints
        group.MapGet("/wallets/{id}", GetWalletById)
            .WithName("GetDtpWallet")
            .WithOpenApi()
            .Produces<object>(200)
            .Produces(404);

        group.MapGet("/wallets/person/{personId}", GetWalletsByPerson)
            .WithName("GetDtpWalletsByPerson")
            .WithOpenApi()
            .Produces<object>(200);

        // Credential endpoints
        group.MapGet("/credentials/{id}", GetCredentialById)
            .WithName("GetDtpCredential")
            .WithOpenApi()
            .Produces<object>(200)
            .Produces(404);

        group.MapPost("/credentials/verify", VerifyCredential)
            .WithName("VerifyDtpCredential")
            .WithOpenApi()
            .Accepts<DtpVerifyRequest>("application/json")
            .Produces<object>(200)
            .Produces(400);

        group.MapGet("/credentials/wallet/{walletId}", GetCredentialsByWallet)
            .WithName("GetDtpCredentialsByWallet")
            .WithOpenApi()
            .Produces<object>(200);

        // Issuer endpoints
        group.MapPost("/credentials/issue", IssueCredential)
            .WithName("IssueDtpCredential")
            .WithOpenApi()
            .Accepts<DtpIssueRequest>("application/json")
            .Produces<object>(201)
            .Produces(400)
            .RequireAuthorization("Issuer");

        group.MapPost("/credentials/{id}/revoke", RevokeCredential)
            .WithName("RevokeDtpCredential")
            .WithOpenApi()
            .Accepts<DtpRevokeRequest>("application/json")
            .Produces<object>(200)
            .Produces(400)
            .RequireAuthorization("Issuer");
    }

    private static async Task<IResult> GetWalletById(
        string id,
        IWalletService walletService,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(id, out var walletId))
            {
                return HandleError("Invalid wallet ID format", 400);
            }

            var wallet = await walletService.GetByIdAsync(walletId, cancellationToken);
            if (wallet == null)
            {
                return HandleError("Wallet not found", 404);
            }

            return HandleSuccess(DtpResponseMapper.MapToDtpWallet(wallet));
        }
        catch (Exception ex)
        {
            return HandleError($"Failed to retrieve wallet: {ex.Message}", 500);
        }
    }

    private static async Task<IResult> GetWalletsByPerson(
        string personId,
        IWalletService walletService,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(personId, out var personGuid))
            {
                return HandleError("Invalid person ID format", 400);
            }

            var wallets = await walletService.GetByPersonIdAsync(personGuid, cancellationToken);
            var dtpWallets = wallets.Select(DtpResponseMapper.MapToDtpWallet);

            return HandleSuccess(new
            {
                wallets = dtpWallets,
                count = wallets.Count()
            });
        }
        catch (Exception ex)
        {
            return HandleError($"Failed to retrieve wallets: {ex.Message}", 500);
        }
    }

    private static async Task<IResult> GetCredentialById(
        string id,
        ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(id, out var credentialId))
            {
                return HandleError("Invalid credential ID format", 400);
            }

            var credential = await credentialService.GetByIdAsync(credentialId, cancellationToken);
            if (credential == null)
            {
                return HandleError("Credential not found", 404);
            }

            return HandleSuccess(DtpResponseMapper.MapToDtpCredential(credential));
        }
        catch (Exception ex)
        {
            return HandleError($"Failed to retrieve credential: {ex.Message}", 500);
        }
    }

    private static async Task<IResult> GetCredentialsByWallet(
        string walletId,
        ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(walletId, out var walletGuid))
            {
                return HandleError("Invalid wallet ID format", 400);
            }

            var credentials = await credentialService.GetByWalletIdAsync(walletGuid, cancellationToken);
            var dtpCredentials = credentials.Select(DtpResponseMapper.MapToDtpCredential);

            return HandleSuccess(new
            {
                credentials = dtpCredentials,
                count = credentials.Count()
            });
        }
        catch (Exception ex)
        {
            return HandleError($"Failed to retrieve credentials: {ex.Message}", 500);
        }
    }

    private static async Task<IResult> VerifyCredential(
        DtpVerifyRequest request,
        IValidator<DtpVerifyRequest> validator,
        ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequest(request, validator);
        if (validationError != null)
        {
            return validationError;
        }

        try
        {
            if (!Guid.TryParse(request.CredentialId, out var credentialId))
            {
                return HandleError("Invalid credential ID format", 400);
            }

            var result = await credentialService.VerifyCredentialAsync(credentialId, cancellationToken);

            return HandleSuccess(DtpResponseMapper.MapToDtpVerificationResult(
                result.IsValid,
                request.CredentialId,
                result.Claims));
        }
        catch (Exception ex)
        {
            return HandleError($"Verification failed: {ex.Message}", 500);
        }
    }

    private static async Task<IResult> IssueCredential(
        DtpIssueRequest request,
        IValidator<DtpIssueRequest> validator,
        ICredentialService credentialService,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequest(request, validator);
        if (validationError != null)
        {
            return validationError;
        }

        try
        {
            if (!Guid.TryParse(request.WalletId, out var walletId))
            {
                return HandleError("Invalid wallet ID format", 400);
            }

            var issuerId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("Issuer not authenticated");

            var credential = await credentialService.IssueCredentialAsync(
                walletId,
                request.CredentialType,
                request.Subject,
                request.Claims,
                issuerId,
                request.ExpirationDate,
                cancellationToken);

            return Results.Created(
                $"/api/v1/dtp/credentials/{credential.Id}",
                DtpResponseMapper.MapToDtpCredential(credential));
        }
        catch (Exception ex)
        {
            return HandleError($"Failed to issue credential: {ex.Message}", 500);
        }
    }

    private static async Task<IResult> RevokeCredential(
        string id,
        DtpRevokeRequest request,
        IValidator<DtpRevokeRequest> validator,
        ICredentialService credentialService,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequest(request, validator);
        if (validationError != null)
        {
            return validationError;
        }

        try
        {
            if (!Guid.TryParse(id, out var credentialId))
            {
                return HandleError("Invalid credential ID format", 400);
            }

            var revokerId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
                ?? throw new UnauthorizedAccessException("Revoker not authenticated");

            var result = await credentialService.RevokeCredentialAsync(
                credentialId,
                request.Reason,
                revokerId,
                cancellationToken);

            if (result)
            {
                return HandleSuccess(new
                {
                    credentialId = id,
                    revoked = true,
                    revokedAt = DateTime.UtcNow,
                    reason = request.Reason
                }, "Credential revoked successfully");
            }

            return HandleError("Failed to revoke credential", 400);
        }
        catch (Exception ex)
        {
            return HandleError($"Failed to revoke credential: {ex.Message}", 500);
        }
    }
}

// Request DTOs
public record DtpVerifyRequest(string CredentialId, Dictionary<string, object>? Context);
public record DtpIssueRequest(
    string WalletId,
    string CredentialType,
    string Subject,
    Dictionary<string, object> Claims,
    DateTime? ExpirationDate);
public record DtpRevokeRequest(string Reason, string? Details);