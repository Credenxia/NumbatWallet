using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Credentials.Handlers;

/// <summary>
/// Revokes N credentials in a single operation, returning per-id success/failure.
/// Mirrors <see cref="RevokeCredentialCommandHandler"/> semantics per item (load, Revoke(reason),
/// persist, dispatch event), but never fails the whole batch on a single bad item.
/// </summary>
public class BulkRevokeCredentialsCommandHandler : ICommandHandler<BulkRevokeCredentialsCommand, BulkRevokeResult>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Application.Interfaces.IEventDispatcher _eventDispatcher;
    private readonly ILogger<BulkRevokeCredentialsCommandHandler> _logger;

    public BulkRevokeCredentialsCommandHandler(
        ICredentialRepository credentialRepository,
        IUnitOfWork unitOfWork,
        Application.Interfaces.IEventDispatcher eventDispatcher,
        ILogger<BulkRevokeCredentialsCommandHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<BulkRevokeResult> HandleAsync(BulkRevokeCredentialsCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bulk revoking {Count} credentials for reason: {Reason}",
            command.CredentialIds.Count, command.Reason);

        var revokedIds = new List<Guid>();
        var errors = new List<BulkRevokeError>();

        foreach (var credentialId in command.CredentialIds)
        {
            try
            {
                var credential = await _credentialRepository.GetByIdAsync(credentialId, cancellationToken);
                if (credential == null)
                {
                    _logger.LogWarning("Credential {CredentialId} not found during bulk revoke", credentialId);
                    errors.Add(new BulkRevokeError(credentialId, $"Credential with ID {credentialId} not found"));
                    continue;
                }

                var revokeResult = credential.Revoke(command.Reason);
                if (revokeResult.IsFailure)
                {
                    _logger.LogWarning("Failed to revoke credential {CredentialId}: {Error}",
                        credentialId, revokeResult.Error.Message);
                    errors.Add(new BulkRevokeError(credentialId, revokeResult.Error.Message));
                    continue;
                }

                await _credentialRepository.UpdateAsync(credential, cancellationToken);

                var revokedEvent = new CredentialRevokedEvent(
                    credential.Id,
                    credential.IssuerId,
                    command.Reason,
                    null,
                    DateTimeOffset.UtcNow);

                await _eventDispatcher.DispatchAsync(revokedEvent, cancellationToken);

                revokedIds.Add(credential.Id);
                _logger.LogInformation("Successfully revoked credential {CredentialId}", credential.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error revoking credential {CredentialId}", credentialId);
                errors.Add(new BulkRevokeError(credentialId, $"Unexpected error: {ex.Message}"));
            }
        }

        // Persist all successful revocations once.
        if (revokedIds.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = new BulkRevokeResult(
            command.CredentialIds.Count,
            revokedIds.Count,
            errors.Count,
            revokedIds,
            errors);

        _logger.LogInformation("Bulk revoke complete: {Success}/{Total} successful",
            result.SuccessCount, result.TotalRequested);

        return result;
    }
}
