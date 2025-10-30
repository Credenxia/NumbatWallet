using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Issuances.Handlers;

public class GetIssuancesByStatusHandler : IQueryHandler<GetIssuancesByStatusQuery, IEnumerable<IssuanceDto>>
{
    private readonly IIssuanceRepository _issuanceRepository;
    private readonly ILogger<GetIssuancesByStatusHandler> _logger;

    public GetIssuancesByStatusHandler(
        IIssuanceRepository issuanceRepository,
        ILogger<GetIssuancesByStatusHandler> logger)
    {
        _issuanceRepository = issuanceRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<IssuanceDto>> HandleAsync(GetIssuancesByStatusQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting issuances with status {Status}", query.Status);

        // Get issuances by status
        var issuances = await _issuanceRepository.FindAsync(
            i => i.Status == query.Status,
            cancellationToken);

        // Apply date filters if provided
        if (query.FromDate.HasValue)
        {
            issuances = issuances.Where(i => i.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            issuances = issuances.Where(i => i.CreatedAt <= query.ToDate.Value);
        }

        // Apply pagination if provided
        var issuanceList = issuances.ToList();

        if (query.PageNumber.HasValue && query.PageSize.HasValue)
        {
            var skip = (query.PageNumber.Value - 1) * query.PageSize.Value;
            issuanceList = issuanceList
                .Skip(skip)
                .Take(query.PageSize.Value)
                .ToList();
        }
        else if (query.PageSize.HasValue)
        {
            issuanceList = issuanceList
                .Take(query.PageSize.Value)
                .ToList();
        }

        // Map to DTOs
        var dtos = issuanceList.Select(issuance => new IssuanceDto
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
        }).ToList();

        _logger.LogInformation("Found {Count} issuances with status {Status}",
            dtos.Count, query.Status);

        return dtos;
    }
}