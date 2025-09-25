using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Commands.Issuances.Handlers;

public class ApproveIssuanceCommandHandler : ICommandHandler<ApproveIssuanceCommand, IssuanceDto>
{
    private readonly IIssuanceRepository _issuanceRepository;
    private readonly ILogger<ApproveIssuanceCommandHandler> _logger;

    public ApproveIssuanceCommandHandler(
        IIssuanceRepository issuanceRepository,
        ILogger<ApproveIssuanceCommandHandler> logger)
    {
        _issuanceRepository = issuanceRepository;
        _logger = logger;
    }

    public async Task<IssuanceDto> HandleAsync(ApproveIssuanceCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Approving issuance request {IssuanceId}", command.IssuanceId);

        // Get the issuance
        var issuance = await _issuanceRepository.GetByIdAsync(command.IssuanceId, cancellationToken);
        if (issuance == null)
        {
            throw new InvalidOperationException($"Issuance {command.IssuanceId} not found");
        }

        // Approve the issuance
        issuance.Approve(command.ApprovedBy, command.Comments);

        // Update in repository
        await _issuanceRepository.UpdateAsync(issuance, cancellationToken);

        _logger.LogInformation("Approved issuance request {IssuanceId} with status {Status}",
            issuance.Id, issuance.Status);

        // Map to DTO
        var dto = new IssuanceDto
        {
            Id = issuance.Id,
            CredentialType = issuance.CredentialType,
            RequesterId = issuance.RequesterId,
            WalletId = issuance.WalletId,
            Status = issuance.Status,
            CreatedAt = issuance.CreatedAt,
            ApprovedAt = issuance.ApprovedAt,
            RejectedAt = issuance.RejectedAt,
            CompletedAt = issuance.CompletedAt,
            ApprovedBy = issuance.ApprovedBy,
            RejectedBy = issuance.RejectedBy,
            CompletedBy = issuance.CompletedBy,
            RejectionReason = issuance.RejectionReason,
            CredentialId = issuance.CredentialId,
            CredentialData = issuance.Claims.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            AdditionalData = issuance.Metadata.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
        };

        return dto;
    }
}