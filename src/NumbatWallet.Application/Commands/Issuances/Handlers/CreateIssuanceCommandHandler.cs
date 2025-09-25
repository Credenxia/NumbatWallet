using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Application.Commands.Issuances.Handlers;

public class CreateIssuanceCommandHandler : ICommandHandler<CreateIssuanceCommand, IssuanceDto>
{
    private readonly IIssuanceRepository _issuanceRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ILogger<CreateIssuanceCommandHandler> _logger;

    public CreateIssuanceCommandHandler(
        IIssuanceRepository issuanceRepository,
        IWalletRepository walletRepository,
        ILogger<CreateIssuanceCommandHandler> logger)
    {
        _issuanceRepository = issuanceRepository;
        _walletRepository = walletRepository;
        _logger = logger;
    }

    public async Task<IssuanceDto> HandleAsync(CreateIssuanceCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating issuance request for wallet {WalletId}", command.WalletId);

        // Verify wallet exists
        var wallet = await _walletRepository.GetByIdAsync(command.WalletId, cancellationToken);
        if (wallet == null)
        {
            throw new InvalidOperationException($"Wallet {command.WalletId} not found");
        }

        // Create issuance entity
        var issuance = new Issuance(
            Guid.Parse(wallet.TenantId),
            command.CredentialType,
            command.WalletId,
            command.RequesterId ?? "system",
            command.Claims,
            command.ExpiryDate);

        // Add metadata if provided
        if (command.Metadata != null)
        {
            foreach (var kvp in command.Metadata)
            {
                issuance.AddMetadata(kvp.Key, kvp.Value);
            }
        }

        // Save to repository
        var created = await _issuanceRepository.AddAsync(issuance, cancellationToken);

        _logger.LogInformation("Created issuance request {IssuanceId} with status {Status}",
            created.Id, created.Status);

        // Map to DTO manually since the DTO structure is different
        var dto = new IssuanceDto
        {
            Id = created.Id,
            CredentialType = created.CredentialType,
            RequesterId = created.RequesterId,
            WalletId = created.WalletId,
            Status = created.Status,
            CreatedAt = created.CreatedAt,
            ApprovedAt = created.ApprovedAt,
            RejectedAt = created.RejectedAt,
            CompletedAt = created.CompletedAt,
            ApprovedBy = created.ApprovedBy,
            RejectedBy = created.RejectedBy,
            CompletedBy = created.CompletedBy,
            RejectionReason = created.RejectionReason,
            CredentialId = created.CredentialId,
            CredentialData = created.Claims.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            AdditionalData = created.Metadata.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
        };

        return dto;
    }
}