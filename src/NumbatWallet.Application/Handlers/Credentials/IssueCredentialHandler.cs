using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Application.Handlers.Credentials;

public class IssueCredentialHandler : ICommandHandler<IssueCredentialCommand, CredentialDto>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IssueCredentialHandler> _logger;

    public IssueCredentialHandler(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        ILogger<IssueCredentialHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CredentialDto> HandleAsync(IssueCredentialCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Issuing credential for wallet {WalletId}", command.WalletId);

        var wallet = await _walletRepository.GetByIdAsync(command.WalletId, cancellationToken);
        if (wallet == null)
        {
            throw new ArgumentException($"Wallet {command.WalletId} not found");
        }

        // Serialize claims to JSON
        var credentialData = JsonSerializer.Serialize(command.Claims);

        var credentialResult = Credential.Create(
            command.WalletId,
            command.IssuerOrganizationId,
            command.CredentialType.ToString(),
            credentialData,
            "default-schema");

        if (!credentialResult.IsSuccess)
        {
            throw new InvalidOperationException(credentialResult.Error?.Message ?? "Failed to create credential");
        }

        var credential = credentialResult.Value;
        credential.SetTenantId(wallet.TenantId);

        // Set expiry if provided
        if (command.ValidUntil.HasValue)
        {
            credential.SetExpiry(new DateTimeOffset(command.ValidUntil.Value));
        }

        // Activate the credential
        credential.Activate();

        await _credentialRepository.AddAsync(credential, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Credential {CredentialId} issued successfully", credential.Id);

        return MapToDto(credential);
    }

    private static CredentialDto MapToDto(Credential credential)
    {
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