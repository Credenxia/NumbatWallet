using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using Microsoft.Extensions.Logging;
using NumbatWallet.SharedKernel.Enums;
using System.Globalization;

namespace NumbatWallet.Application.Handlers.Credentials;

public class UpdateCredentialStatusHandler : ICommandHandler<UpdateCredentialStatusCommand, CredentialDto>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCredentialStatusHandler> _logger;

    public UpdateCredentialStatusHandler(
        ICredentialRepository credentialRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateCredentialStatusHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CredentialDto> HandleAsync(UpdateCredentialStatusCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating credential {CredentialId} status to {Status}", command.CredentialId, command.Status);

        var credential = await _credentialRepository.GetByIdAsync(command.CredentialId, cancellationToken);
        if (credential == null)
        {
            throw new ArgumentException($"Credential {command.CredentialId} not found");
        }

        if (credential.TenantId != command.TenantId.ToString())
        {
            throw new UnauthorizedAccessException("Cannot update credential from different tenant");
        }

        switch (command.Status.ToLower(CultureInfo.InvariantCulture))
        {
            case "revoked":
                var revokeResult = credential.Revoke(command.Reason ?? "Status updated via API");
                if (!revokeResult.IsSuccess)
                {
                    throw new InvalidOperationException(revokeResult.Error?.Message ?? "Failed to revoke credential");
                }
                break;
            case "suspended":
                var suspendResult = credential.Suspend(command.Reason ?? "Status updated via API");
                if (!suspendResult.IsSuccess)
                {
                    throw new InvalidOperationException(suspendResult.Error?.Message ?? "Failed to suspend credential");
                }
                break;
            case "active":
                var activateResult = credential.Activate();
                if (!activateResult.IsSuccess)
                {
                    throw new InvalidOperationException(activateResult.Error?.Message ?? "Failed to activate credential");
                }
                break;
            default:
                throw new ArgumentException($"Invalid status: {command.Status}");
        }

        await _credentialRepository.UpdateAsync(credential, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Credential {CredentialId} status updated successfully", credential.Id);

        return new CredentialDto
        {
            Id = credential.Id.ToString(),
            HolderId = credential.WalletId.ToString(),
            IssuerId = credential.IssuerId.ToString(),
            Type = credential.CredentialType,
            CredentialSubject = new Dictionary<string, object>(credential.Claims),
            IssuanceDate = credential.IssuedAt.DateTime,
            ExpirationDate = credential.ExpiresAt?.DateTime,
            Status = credential.Status.ToString(),
            Proof = null,
            Metadata = null,
            IsRevoked = credential.Status == CredentialStatus.Revoked,
            RevocationDate = credential.RevokedAt?.DateTime,
            RevocationReason = credential.RevocationReason
        };
    }
}