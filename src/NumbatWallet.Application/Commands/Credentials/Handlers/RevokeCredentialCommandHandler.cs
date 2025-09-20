using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Exceptions;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Commands.Credentials.Handlers;

public class RevokeCredentialCommandHandler : ICommandHandler<RevokeCredentialCommand, bool>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly ILogger<RevokeCredentialCommandHandler> _logger;

    public RevokeCredentialCommandHandler(
        ICredentialRepository credentialRepository,
        IEventDispatcher eventDispatcher,
        ILogger<RevokeCredentialCommandHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(RevokeCredentialCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Revoking credential {CredentialId} for reason: {Reason}",
            command.CredentialId, command.Reason);

        // Get credential
        var credential = await _credentialRepository.GetByIdAsync(command.CredentialId, cancellationToken)
            ?? throw new NotFoundException($"Credential with ID {command.CredentialId} not found");

        // Revoke the credential
        var revokeResult = credential.Revoke(command.Reason);
        if (revokeResult.IsFailure)
        {
            _logger.LogWarning("Failed to revoke credential {CredentialId}: {Error}",
                command.CredentialId, revokeResult.Error.Message);
            throw new BusinessRuleException(revokeResult.Error.Message);
        }

        // Update credential in repository
        await _credentialRepository.UpdateAsync(credential, cancellationToken);

        // Dispatch domain event
        var credentialRevokedEvent = new CredentialRevokedEvent(
            credential.Id,
            credential.IssuerId,
            command.Reason,
            null, // No revocation registry ID for now
            DateTimeOffset.UtcNow);

        await _eventDispatcher.DispatchAsync(credentialRevokedEvent, cancellationToken);

        _logger.LogInformation("Successfully revoked credential {CredentialId}", command.CredentialId);

        return true;
    }
}