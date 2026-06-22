using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Presentations.Handlers;

/// <summary>
/// EXPERIMENTAL (minimal OID4VP subset): verifies a submitted VP-JWT against a pending
/// PresentationRequest. On top of the full Phase-1 W3C VP verification (delegated to
/// <see cref="VerifyPresentationCommandHandler"/> via DI), the submission must SATISFY the
/// request:
/// <list type="bullet">
/// <item>the request exists, is Pending and within its lifetime (one-shot — replays rejected);</item>
/// <item>the VP's <c>nonce</c> equals the request's nonce (binds token to this exchange);</item>
/// <item>the VP's <c>aud</c> is the request's verifier;</item>
/// <item>the presented credential type matches the requested type (presentation_definition);</item>
/// <item>every requested claim was disclosed.</item>
/// </list>
/// Every failure is a normal outcome (IsValid=false with a precise reason) — never a 500.
/// </summary>
public class SubmitPresentationCommandHandler
    : ICommandHandler<SubmitPresentationCommand, PresentationVerificationResultDto>
{
    private readonly IPresentationRequestRepository _presentationRequestRepository;
    private readonly ICommandHandler<VerifyPresentationCommand, PresentationVerificationResultDto> _verifyPresentationHandler;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubmitPresentationCommandHandler> _logger;

    public SubmitPresentationCommandHandler(
        IPresentationRequestRepository presentationRequestRepository,
        ICommandHandler<VerifyPresentationCommand, PresentationVerificationResultDto> verifyPresentationHandler,
        IUnitOfWork unitOfWork,
        ILogger<SubmitPresentationCommandHandler> logger)
    {
        _presentationRequestRepository = presentationRequestRepository;
        _verifyPresentationHandler = verifyPresentationHandler;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PresentationVerificationResultDto> HandleAsync(
        SubmitPresentationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.RequestId == Guid.Empty)
        {
            return Invalid("Presentation request id is required.");
        }

        if (string.IsNullOrWhiteSpace(command.VpToken))
        {
            return Invalid("Presentation token is required.");
        }

        // 1. The request must exist and still be open (one-shot fulfilment).
        var request = await _presentationRequestRepository.GetByIdAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            return Invalid("Presentation request not found.");
        }

        if (request.Status == PresentationRequestStatus.Fulfilled)
        {
            _logger.LogWarning("Replay rejected for fulfilled presentation request {RequestId}", request.Id);
            return Invalid("Presentation request has already been fulfilled.");
        }

        if (request.Status == PresentationRequestStatus.Expired || request.IsExpired)
        {
            if (request.Status != PresentationRequestStatus.Expired)
            {
                request.MarkExpired();
                await _presentationRequestRepository.UpdateAsync(request, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Invalid("Presentation request has expired.");
        }

        // 2. Full Phase-1 W3C VP verification (signature, structure, embedded VC, revocation).
        var verification = await _verifyPresentationHandler.HandleAsync(
            new VerifyPresentationCommand(command.VpToken, request.VerifierId), cancellationToken);
        if (!verification.IsValid)
        {
            return verification;
        }

        // 3. The VP must be bound to THIS request: nonce and audience must match.
        //    (The token is already signature-validated above; reading claims is safe.)
        var vpToken = new JwtSecurityTokenHandler().ReadJwtToken(command.VpToken);

        var nonce = vpToken.Claims.FirstOrDefault(c => c.Type == W3cVcConstants.NonceClaim)?.Value;
        if (!string.Equals(nonce, request.Nonce, StringComparison.Ordinal))
        {
            return Invalid("Presentation nonce does not match the request.");
        }

        if (!vpToken.Audiences.Contains(request.VerifierId, StringComparer.Ordinal))
        {
            return Invalid("Presentation was not created for this request's verifier.");
        }

        // 4. The presentation must satisfy the request's definition: credential type + claims.
        if (!string.Equals(verification.CredentialType, request.RequestedCredentialType, StringComparison.Ordinal))
        {
            return Invalid(
                $"Presented credential type '{verification.CredentialType}' does not satisfy the requested type '{request.RequestedCredentialType}'.");
        }

        var requestedClaims = JsonSerializer.Deserialize<List<string>>(request.RequestedClaimsJson) ?? new List<string>();
        var disclosedKeys = verification.DisclosedClaims?.Keys ?? Enumerable.Empty<string>();
        var missing = requestedClaims.Except(disclosedKeys, StringComparer.Ordinal).ToList();
        if (missing.Count > 0)
        {
            return Invalid($"Presentation does not disclose the requested claim(s): {string.Join(", ", missing)}.");
        }

        // 5. Fulfil — one-shot. A concurrent/duplicate submission fails here.
        var fulfilResult = request.MarkFulfilled(verification.PresentationId!.Value);
        if (fulfilResult.IsFailure)
        {
            return Invalid(fulfilResult.Error.Message);
        }

        await _presentationRequestRepository.UpdateAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Presentation request {RequestId} fulfilled by presentation {PresentationId}",
            request.Id, verification.PresentationId);

        return verification;
    }

    private static PresentationVerificationResultDto Invalid(string reason) => new()
    {
        IsValid = false,
        Reason = reason
    };
}
