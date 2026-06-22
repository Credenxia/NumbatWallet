using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.Common;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Presentations.Handlers;

/// <summary>
/// Verifies a W3C JWT-VP presentation token end to end. Every check failure is a NORMAL
/// verification outcome (IsValid=false with a precise Reason) — never an exception/500:
/// <list type="number">
/// <item>VP-JWT signature + lifetime (keys/algorithm from <see cref="IAccessTokenSigner"/>,
/// the same selection model used to mint the token — HS256 dev / RS256 Key Vault).</item>
/// <item>VP structure: <c>vp</c> claim present with the base W3C @context,
/// type containing VerifiablePresentation, and a non-empty verifiableCredential array.</item>
/// <item>Replay-protection <c>nonce</c> present.</item>
/// <item><c>jti</c> resolves to a persisted, unexpired Presentation whose VerifierId matches
/// the token's <c>aud</c>.</item>
/// <item>Embedded VC-JWT signature + lifetime, <c>vc</c> structure (@context/type), and
/// VC↔presentation consistency (the VC's <c>jti</c> must be the presented credential).</item>
/// <item>The underlying credential still exists, is Active, not revoked, not expired —
/// a credential revoked AFTER presentation must fail verification.</item>
/// </list>
/// </summary>
public class VerifyPresentationCommandHandler : ICommandHandler<VerifyPresentationCommand, PresentationVerificationResultDto>
{
    private readonly IPresentationRepository _presentationRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Interfaces.IEventDispatcher _eventDispatcher;
    private readonly IAccessTokenSigner _tokenSigner;
    private readonly ILogger<VerifyPresentationCommandHandler> _logger;

    public VerifyPresentationCommandHandler(
        IPresentationRepository presentationRepository,
        ICredentialRepository credentialRepository,
        IUnitOfWork unitOfWork,
        Interfaces.IEventDispatcher eventDispatcher,
        IAccessTokenSigner tokenSigner,
        ILogger<VerifyPresentationCommandHandler> logger)
    {
        _presentationRepository = presentationRepository;
        _credentialRepository = credentialRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
        _tokenSigner = tokenSigner;
        _logger = logger;
    }

    public async Task<PresentationVerificationResultDto> HandleAsync(
        VerifyPresentationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.PresentationToken))
        {
            return Invalid("Presentation token is required.");
        }

        // 1. Validate the VP-JWT itself (signature + lifetime).
        var (vpToken, failureReason) = ValidateSignedToken(command.PresentationToken, requireExpiry: true);
        if (vpToken is null)
        {
            _logger.LogWarning("Presentation token validation failed: {Reason}", failureReason);
            return Invalid($"Presentation token {failureReason}");
        }

        // 2. The token must be a W3C Verifiable Presentation with a well-formed vp claim.
        var (vcJwt, vpFailure) = ExtractEmbeddedCredential(vpToken);
        if (vcJwt is null)
        {
            return Invalid(vpFailure!);
        }

        // 3. Replay-protection nonce must be present (OID4VP binds it to a request).
        var nonce = vpToken.Claims.FirstOrDefault(c => c.Type == W3cVcConstants.NonceClaim)?.Value;
        if (string.IsNullOrWhiteSpace(nonce))
        {
            return Invalid("Presentation token is missing a nonce.");
        }

        // 4. The jti identifies the persisted presentation record.
        var jti = vpToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        if (!Guid.TryParse(jti, out var presentationId))
        {
            return Invalid("Presentation token has no valid presentation identifier.");
        }

        var presentation = await _presentationRepository.GetByIdAsync(presentationId, cancellationToken);
        if (presentation is null)
        {
            return Invalid("Presentation not found.");
        }

        if (presentation.IsExpired)
        {
            presentation.MarkExpired();
            await _presentationRepository.UpdateAsync(presentation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Invalid("Presentation has expired.");
        }

        // 5. The token's audience must be the verifier the presentation was created for.
        if (!vpToken.Audiences.Contains(presentation.VerifierId, StringComparer.Ordinal))
        {
            return Invalid("Presentation audience does not match the verifier it was created for.");
        }

        // 6. Validate the embedded VC-JWT (signature, lifetime, vc structure).
        var (vcToken, vcReason) = ValidateSignedToken(vcJwt, requireExpiry: false);
        if (vcToken is null)
        {
            return Invalid($"Embedded credential {vcReason}");
        }

        var vcStructureFailure = ValidateCredentialStructure(vcToken);
        if (vcStructureFailure is not null)
        {
            return Invalid(vcStructureFailure);
        }

        // 7. VC ↔ presentation consistency: the embedded credential must BE the presented one.
        var vcJti = vcToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        if (!W3cVcConstants.TryParseUrnUuid(vcJti, out var embeddedCredentialId)
            || embeddedCredentialId != presentation.CredentialId)
        {
            return Invalid("Embedded credential does not match the presented credential.");
        }

        // 8. Re-check the underlying credential. A credential revoked, suspended or expired
        //    AFTER the presentation was created must fail verification.
        var credential = await _credentialRepository.GetByIdAsync(presentation.CredentialId, cancellationToken);
        if (credential is null)
        {
            return Invalid("Credential for this presentation no longer exists.");
        }

        if (credential.Status == CredentialStatus.Revoked)
        {
            return Invalid("Credential has been revoked.");
        }

        if (credential.IsExpired())
        {
            return Invalid("Credential has expired.");
        }

        if (credential.Status != CredentialStatus.Active)
        {
            return Invalid($"Credential is not active (status: {credential.Status}).");
        }

        // 9. Record the verification (re-verification within the token lifetime is allowed).
        var markResult = presentation.MarkVerified();
        if (markResult.IsFailure)
        {
            return Invalid(markResult.Error.Message);
        }

        await _presentationRepository.UpdateAsync(presentation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var verifierId = command.VerifierId ?? presentation.VerifierId;
        await _eventDispatcher.DispatchAsync(
            new PresentationVerifiedEvent(
                presentation.Id,
                verifierId,
                true,
                null,
                presentation.VerifiedAt!.Value),
            cancellationToken);

        _logger.LogInformation(
            "Presentation {PresentationId} verified (count: {Count}) for credential {CredentialId}",
            presentation.Id, presentation.VerificationCount, presentation.CredentialId);

        return new PresentationVerificationResultDto
        {
            IsValid = true,
            PresentationId = presentation.Id,
            CredentialId = credential.Id,
            CredentialType = credential.CredentialType,
            DisclosedClaims = JsonSerializer.Deserialize<Dictionary<string, object>>(presentation.DisclosedClaimsJson)
                ?? new Dictionary<string, object>(),
            PresentedAt = presentation.PresentedAt,
            VerifiedAt = presentation.VerifiedAt
        };
    }

    /// <summary>
    /// Validates a JWS produced by the platform signer. Issuer/audience are entity identifiers
    /// (holder/verifier), not the platform's API issuer — they are consistency-checked against
    /// the persisted presentation instead of static configuration. The accepted algorithm is
    /// pinned to the active signer's to prevent algorithm-confusion attacks.
    /// </summary>
    private (JwtSecurityToken? Token, string? FailureReason) ValidateSignedToken(string token, bool requireExpiry)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = _tokenSigner.GetValidationKeys(),
            ValidAlgorithms = new[] { _tokenSigner.Algorithm },
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            RequireExpirationTime = requireExpiry,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(token, parameters, out var validatedToken);
            return (validatedToken as JwtSecurityToken, null);
        }
        catch (SecurityTokenExpiredException)
        {
            return (null, "has expired.");
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return (null, "is invalid (signature or format).");
        }
    }

    /// <summary>
    /// Asserts the vp claim is a structurally valid W3C VerifiablePresentation and returns the
    /// single embedded JWT-VC string.
    /// </summary>
    private static (string? VcJwt, string? FailureReason) ExtractEmbeddedCredential(JwtSecurityToken vpToken)
    {
        var vpJson = vpToken.Claims.FirstOrDefault(c => c.Type == W3cVcConstants.VpClaim)?.Value;
        if (string.IsNullOrWhiteSpace(vpJson))
        {
            return (null, "Token is not a Verifiable Presentation (missing vp claim).");
        }

        JsonElement vp;
        try
        {
            vp = JsonSerializer.Deserialize<JsonElement>(vpJson);
        }
        catch (JsonException)
        {
            return (null, "Presentation vp claim is not valid JSON.");
        }

        if (vp.ValueKind != JsonValueKind.Object)
        {
            return (null, "Presentation vp claim is not a JSON object.");
        }

        if (!ContainsStringValue(vp, "@context", W3cVcConstants.CredentialsContextV1))
        {
            return (null, "Presentation @context is missing the W3C credentials context.");
        }

        if (!ContainsStringValue(vp, "type", W3cVcConstants.VerifiablePresentationType))
        {
            return (null, "Presentation type does not include VerifiablePresentation.");
        }

        if (!vp.TryGetProperty("verifiableCredential", out var vcArray)
            || vcArray.ValueKind != JsonValueKind.Array
            || vcArray.GetArrayLength() == 0)
        {
            return (null, "Presentation contains no verifiable credential.");
        }

        var first = vcArray[0];
        if (first.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(first.GetString()))
        {
            return (null, "Presentation's embedded credential is not a JWT string.");
        }

        return (first.GetString(), null);
    }

    /// <summary>Asserts the vc claim is a structurally valid W3C VerifiableCredential.</summary>
    private static string? ValidateCredentialStructure(JwtSecurityToken vcToken)
    {
        var vcJson = vcToken.Claims.FirstOrDefault(c => c.Type == W3cVcConstants.VcClaim)?.Value;
        if (string.IsNullOrWhiteSpace(vcJson))
        {
            return "Embedded token is not a Verifiable Credential (missing vc claim).";
        }

        JsonElement vc;
        try
        {
            vc = JsonSerializer.Deserialize<JsonElement>(vcJson);
        }
        catch (JsonException)
        {
            return "Embedded credential vc claim is not valid JSON.";
        }

        if (vc.ValueKind != JsonValueKind.Object)
        {
            return "Embedded credential vc claim is not a JSON object.";
        }

        if (!ContainsStringValue(vc, "@context", W3cVcConstants.CredentialsContextV1))
        {
            return "Embedded credential @context is missing the W3C credentials context.";
        }

        if (!ContainsStringValue(vc, "type", W3cVcConstants.VerifiableCredentialType))
        {
            return "Embedded credential type does not include VerifiableCredential.";
        }

        if (!vc.TryGetProperty("credentialSubject", out var subject)
            || subject.ValueKind != JsonValueKind.Object)
        {
            return "Embedded credential has no credentialSubject.";
        }

        return null;
    }

    private static bool ContainsStringValue(JsonElement obj, string property, string expected)
    {
        if (!obj.TryGetProperty(property, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => string.Equals(value.GetString(), expected, StringComparison.Ordinal),
            JsonValueKind.Array => value.EnumerateArray().Any(e =>
                e.ValueKind == JsonValueKind.String && string.Equals(e.GetString(), expected, StringComparison.Ordinal)),
            _ => false
        };
    }

    private static PresentationVerificationResultDto Invalid(string reason) => new()
    {
        IsValid = false,
        Reason = reason
    };
}
