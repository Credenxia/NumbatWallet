using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Presentations.Handlers;

/// <summary>
/// Verifies a signed presentation token: validates the JWT (signature, lifetime, issuer,
/// audience, typ), loads the Presentation record by the token's jti, re-checks the underlying
/// credential (a credential revoked AFTER presentation must fail verification), records the
/// verification, and returns the disclosed claims.
/// Token validation parameters mirror creation in Infrastructure.JwtPresentationTokenService
/// (HS256 with Jwt:SecretKey, Jwt:Issuer / Jwt:Audience with the platform defaults).
/// </summary>
public class VerifyPresentationCommandHandler : ICommandHandler<VerifyPresentationCommand, PresentationVerificationResultDto>
{
    internal const string PresentationTokenType = "presentation";
    internal const string DefaultIssuer = "https://numbatwallet.wa.gov.au";
    internal const string DefaultAudience = "https://api.numbatwallet.wa.gov.au";

    private readonly IPresentationRepository _presentationRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Interfaces.IEventDispatcher _eventDispatcher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VerifyPresentationCommandHandler> _logger;

    public VerifyPresentationCommandHandler(
        IPresentationRepository presentationRepository,
        ICredentialRepository credentialRepository,
        IUnitOfWork unitOfWork,
        Interfaces.IEventDispatcher eventDispatcher,
        IConfiguration configuration,
        ILogger<VerifyPresentationCommandHandler> logger)
    {
        _presentationRepository = presentationRepository;
        _credentialRepository = credentialRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
        _configuration = configuration;
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

        // 1. Validate the JWT itself (signature, lifetime, issuer, audience).
        var (jwtToken, failureReason) = ValidateToken(command.PresentationToken);
        if (jwtToken is null)
        {
            _logger.LogWarning("Presentation token validation failed: {Reason}", failureReason);
            return Invalid(failureReason!);
        }

        // 2. The token must be a presentation token, not e.g. an access token.
        var typ = jwtToken.Claims.FirstOrDefault(c => c.Type == "typ")?.Value;
        if (!string.Equals(typ, PresentationTokenType, StringComparison.Ordinal))
        {
            return Invalid("Token is not a presentation token.");
        }

        // 3. The jti identifies the persisted presentation record.
        var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
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

        // 4. Re-check the underlying credential. A credential revoked, suspended or expired
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

        // 5. Record the verification (re-verification within the token lifetime is allowed).
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

    private (JwtSecurityToken? Token, string? FailureReason) ValidateToken(string token)
    {
        // SECURITY: never fall back to a hardcoded secret — fail fast if unconfigured.
        // (Configuration errors throw; they are NOT normal verification failures.)
        var secret = _configuration["Jwt:SecretKey"] ?? _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException(
                "JWT signing key is not configured. Set 'Jwt:SecretKey' (sourced from Key Vault in production).");
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = true,
            ValidIssuer = _configuration["Jwt:Issuer"] ?? DefaultIssuer,
            ValidateAudience = true,
            ValidAudience = _configuration["Jwt:Audience"] ?? DefaultAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(token, parameters, out var validatedToken);
            return (validatedToken as JwtSecurityToken, null);
        }
        catch (SecurityTokenExpiredException)
        {
            return (null, "Presentation token has expired.");
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return (null, "Presentation token is invalid (signature or format).");
        }
    }

    private static PresentationVerificationResultDto Invalid(string reason) => new()
    {
        IsValid = false,
        Reason = reason
    };
}
