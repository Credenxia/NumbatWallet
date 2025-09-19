using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.Domain.Repositories;

namespace NumbatWallet.Application.Commands.Credentials;

public record RevokeCredentialCommand : ICommand<bool>
{
    public required string CredentialId { get; init; }
    public required string Reason { get; init; }
    public string? RevokedBy { get; init; }
}

public class RevokeCredentialCommandHandler : ICommandHandler<RevokeCredentialCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICredentialRepository _credentialRepository;
    private readonly ILogger<RevokeCredentialCommandHandler> _logger;

    public RevokeCredentialCommandHandler(
        IUnitOfWork unitOfWork,
        ICredentialRepository credentialRepository,
        ILogger<RevokeCredentialCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _credentialRepository = credentialRepository;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        RevokeCredentialCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing RevokeCredentialCommand for credential {CredentialId}", command.CredentialId);

        // Get credential
        var credentialId = Guid.Parse(command.CredentialId);
        var credential = await _credentialRepository.GetByIdAsync(credentialId, cancellationToken);

        if (credential == null)
        {
            throw new EntityNotFoundException("Credential", command.CredentialId);
        }

        // Check if already revoked
        if (credential.Status == SharedKernel.Enums.CredentialStatus.Revoked)
        {
            _logger.LogWarning("Credential {CredentialId} is already revoked", command.CredentialId);
            return false;
        }

        // Revoke the credential
        var revokeResult = credential.Revoke(command.Reason);
        if (revokeResult.IsFailure)
        {
            throw new DomainValidationException(revokeResult.Error.Message);
        }

        // Update credential
        await _credentialRepository.UpdateAsync(credential, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Credential {CredentialId} revoked successfully", credential.Id);

        return true;
    }
}
