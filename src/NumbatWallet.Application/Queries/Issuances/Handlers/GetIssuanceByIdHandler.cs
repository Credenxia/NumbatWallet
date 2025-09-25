using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Issuances.Handlers;

public class GetIssuanceByIdHandler : IQueryHandler<GetIssuanceByIdQuery, IssuanceDto?>
{
    private readonly IIssuanceRepository _issuanceRepository;
    private readonly ILogger<GetIssuanceByIdHandler> _logger;

    public GetIssuanceByIdHandler(
        IIssuanceRepository issuanceRepository,
        ILogger<GetIssuanceByIdHandler> logger)
    {
        _issuanceRepository = issuanceRepository;
        _logger = logger;
    }

    public async Task<IssuanceDto?> HandleAsync(GetIssuanceByIdQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting issuance {IssuanceId}", query.IssuanceId);

        var issuance = await _issuanceRepository.GetByIdAsync(query.IssuanceId, cancellationToken);

        if (issuance == null)
        {
            _logger.LogWarning("Issuance {IssuanceId} not found", query.IssuanceId);
            throw new InvalidOperationException($"Issuance {query.IssuanceId} not found");
        }

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