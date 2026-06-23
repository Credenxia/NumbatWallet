using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Credentials.Handlers;

/// <summary>
/// Verifies N credentials in a single operation, returning per-id validity.
/// Reuses the single-credential <see cref="VerifyCredentialCommand"/> handler so the
/// verification rules stay in one place; never fails the whole batch on a single bad item.
/// </summary>
public class BulkVerifyCredentialsCommandHandler : ICommandHandler<BulkVerifyCredentialsCommand, BulkVerificationResult>
{
    private readonly ICommandHandler<VerifyCredentialCommand, VerificationResultDto> _verifyHandler;
    private readonly ILogger<BulkVerifyCredentialsCommandHandler> _logger;

    public BulkVerifyCredentialsCommandHandler(
        ICommandHandler<VerifyCredentialCommand, VerificationResultDto> verifyHandler,
        ILogger<BulkVerifyCredentialsCommandHandler> logger)
    {
        _verifyHandler = verifyHandler;
        _logger = logger;
    }

    public async Task<BulkVerificationResult> HandleAsync(BulkVerifyCredentialsCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bulk verifying {Count} credentials", command.CredentialIds.Count);

        var results = new List<VerificationResultItem>();

        foreach (var credentialId in command.CredentialIds)
        {
            try
            {
                var verifyCommand = new VerifyCredentialCommand
                {
                    CredentialId = credentialId.ToString()
                };

                var verification = await _verifyHandler.HandleAsync(verifyCommand, cancellationToken);

                results.Add(new VerificationResultItem(
                    credentialId,
                    verification.IsValid,
                    verification.IsValid ? null : verification.ErrorMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error verifying credential {CredentialId}", credentialId);
                results.Add(new VerificationResultItem(credentialId, false, $"Unexpected error: {ex.Message}"));
            }
        }

        var successCount = results.Count(r => r.IsValid);

        var result = new BulkVerificationResult(
            command.CredentialIds.Count,
            successCount,
            results.Count - successCount,
            results);

        _logger.LogInformation("Bulk verification complete: {Success}/{Total} valid",
            result.SuccessCount, result.TotalRequested);

        return result;
    }
}
