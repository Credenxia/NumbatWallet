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

    public VerifyCredentialCommandHandler(
        ICredentialRepository credentialRepository,
        ILogger<VerifyCredentialCommandHandler> logger,
        ICacheService cacheService)
    {
        _credentialRepository = credentialRepository;
        _logger = logger;
        _cacheService = cacheService;
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

            // TODO: Implement actual signature verification
            checks.Signature = true;
            checks.Issuer = true;
            checks.Schema = true;
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
}
