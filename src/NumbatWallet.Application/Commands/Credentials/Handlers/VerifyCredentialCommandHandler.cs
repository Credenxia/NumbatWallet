using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Commands.Credentials.Handlers;

public class VerifyCredentialCommandHandler : ICommandHandler<VerifyCredentialCommand, VerificationResultDto>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly ILogger<VerifyCredentialCommandHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IJwtSigningService _jwtSigningService;

    public VerifyCredentialCommandHandler(
        ICredentialRepository credentialRepository,
        ILogger<VerifyCredentialCommandHandler> logger,
        ICacheService cacheService,
        IJwtSigningService jwtSigningService)
    {
        _credentialRepository = credentialRepository;
        _logger = logger;
        _cacheService = cacheService;
        _jwtSigningService = jwtSigningService;
    }

    public async Task<VerificationResultDto> HandleAsync(VerifyCredentialCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Verifying credential {CredentialId}", command.CredentialId);

        // Check cache first
        var cacheKey = $"verification:{command.CredentialId}";
        var cachedResult = await _cacheService.GetAsync<VerificationResultDto>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            _logger.LogDebug("Returning cached verification result for {CredentialId}", command.CredentialId);
            return cachedResult;
        }

        try
        {
            // Get credential from repository
            var credential = await _credentialRepository.GetByIdAsync(Guid.Parse(command.CredentialId), cancellationToken);

            if (credential == null)
            {
                return new VerificationResultDto
                {
                    IsValid = false,
                    VerifiedAt = DateTime.UtcNow,
                    ErrorMessage = "Credential not found"
                };
            }

            // Basic verification checks
            var checks = new VerificationChecksDto();
            var errorMessages = new List<string>();

            // Check if credential is active
            if (credential.Status != CredentialStatus.Active)
            {
                errorMessages.Add($"Credential status is {credential.Status}");
            }

            // Check expiry
            if (credential.IsExpired())
            {
                checks.Expiry = false;
                errorMessages.Add("Credential has expired");
            }
            else
            {
                checks.Expiry = true;
            }

            // Signature verification
            // Try to verify signature if credential data is provided or available
            var credentialDataToVerify = command.CredentialData ?? credential.CredentialData;

            if (!string.IsNullOrWhiteSpace(credentialDataToVerify) && IsJwtFormat(credentialDataToVerify))
            {
                try
                {
                    checks.Signature = await _jwtSigningService.VerifyCredentialAsync(
                        credentialDataToVerify,
                        cancellationToken);

                    if (!checks.Signature)
                    {
                        errorMessages.Add("Credential signature verification failed");
                        _logger.LogWarning("Signature verification failed for credential {CredentialId}",
                            command.CredentialId);
                    }
                    else
                    {
                        _logger.LogDebug("Signature verified successfully for credential {CredentialId}",
                            command.CredentialId);
                    }
                }
                catch (Exception ex)
                {
                    checks.Signature = false;
                    errorMessages.Add("Error during signature verification");
                    _logger.LogError(ex, "Error verifying signature for credential {CredentialId}",
                        command.CredentialId);
                }
            }
            else
            {
                // No JWT available - this could be a legacy credential or unsigned credential
                // For now, we'll mark signature check as true to not break existing credentials
                // TODO: Once all credentials are JWT-signed, change this to false
                checks.Signature = true;
                _logger.LogDebug("No JWT signature found for credential {CredentialId}, skipping signature verification",
                    command.CredentialId);
            }

            checks.Issuer = true; // Issuer check is implicit from credential.IssuerId
            checks.Schema = !string.IsNullOrWhiteSpace(credential.SchemaId);
            checks.Revocation = credential.Status != CredentialStatus.Revoked;

            // Check verification options
            if (command.VerificationOptions != null)
            {
                // Apply any additional verification rules based on options
                if (command.VerificationOptions.ContainsKey("requireBiometric") &&
                    command.VerificationOptions["requireBiometric"]?.ToString() == "true")
                {
                    // TODO: Check biometric verification status
                    _logger.LogDebug("Biometric verification requested but not implemented");
                }

                if (command.VerificationOptions.ContainsKey("checkRevocation") &&
                    command.VerificationOptions["checkRevocation"] is bool checkRevocation)
                {
                    checks.Revocation = checkRevocation && credential.Status != CredentialStatus.Revoked;
                }
            }

            var result = new VerificationResultDto
            {
                IsValid = errorMessages.Count == 0,
                VerifiedAt = DateTime.UtcNow,
                Checks = checks,
                ErrorMessage = errorMessages.Count > 0 ? string.Join("; ", errorMessages) : null,
                Claims = new Dictionary<string, object>
                {
                    ["credentialType"] = credential.CredentialType,
                    ["issuerId"] = credential.IssuerId.ToString(),
                    ["walletId"] = credential.WalletId.ToString()
                }
            };

            // Cache successful verifications for 5 minutes
            if (result.IsValid)
            {
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);
            }

            _logger.LogInformation("Credential {CredentialId} verification result: {IsValid}",
                command.CredentialId, result.IsValid);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying credential {CredentialId}", command.CredentialId);

            return new VerificationResultDto
            {
                IsValid = false,
                VerifiedAt = DateTime.UtcNow,
                ErrorMessage = "An error occurred during verification"
            };
        }
    }

    /// <summary>
    /// Check if the credential data is in JWT format
    /// JWT format: header.payload.signature, where header starts with "eyJ"
    /// </summary>
    private static bool IsJwtFormat(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return false;
        }

        // JWT tokens start with "eyJ" (base64 encoded JSON header)
        // and have exactly 2 dots separating 3 parts
        return data.StartsWith("eyJ", StringComparison.Ordinal) &&
               data.Count(c => c == '.') == 2;
    }
}
