using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Repositories;
using NumbatWallet.Domain.Specifications;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Application.Queries.Credentials;

public record SearchCredentialsQuery : IQuery<PagedResult<CredentialSummaryDto>>
{
    public required string SearchTerm { get; init; }
    public string[]? Types { get; init; }
    public CredentialStatus[]? Statuses { get; init; }
    public PaginationParams Pagination { get; init; } = new PaginationParams();
}

public class SearchCredentialsQueryHandler : IQueryHandler<SearchCredentialsQuery, PagedResult<CredentialSummaryDto>>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly ILogger<SearchCredentialsQueryHandler> _logger;

    public SearchCredentialsQueryHandler(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        IIssuerRepository issuerRepository,
        ILogger<SearchCredentialsQueryHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _issuerRepository = issuerRepository;
        _logger = logger;
    }

    public async Task<PagedResult<CredentialSummaryDto>> HandleAsync(
        SearchCredentialsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Searching credentials with term '{SearchTerm}', page {Page}/{PageSize}",
            query.SearchTerm, query.Pagination.Page, query.Pagination.PageSize);

        // Build search specification
        var spec = new SearchCredentialsSpecification(query.SearchTerm);

        // Apply type filter
        if (query.Types != null && query.Types.Length > 0)
        {
            spec.AddCriteria(c => query.Types.Contains(c.CredentialType));
        }

        // Apply status filter
        if (query.Statuses != null && query.Statuses.Length > 0)
        {
            spec.AddCriteria(c => query.Statuses.Contains(c.Status));
        }

        // Get total count
        var totalCount = await _credentialRepository.CountAsync(spec, cancellationToken);

        // Apply pagination
        spec.ApplyPaging(query.Pagination.Skip, query.Pagination.PageSize);
        spec.ApplyOrderByDescending(c => c.IssuedAt);

        // Get credentials
        var credentials = await _credentialRepository.FindAsync(spec, cancellationToken);

        // Get wallet and issuer details for mapping
        var walletIds = credentials.Select(c => c.WalletId).Distinct().ToArray();
        var issuerIds = credentials.Select(c => c.IssuerId).Distinct().ToArray();

        var wallets = new Dictionary<Guid, Domain.Aggregates.Wallet>();
        foreach (var walletId in walletIds)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);
            if (wallet != null)
            {
                wallets[walletId] = wallet;
            }
        }

        var issuers = new Dictionary<Guid, Domain.Aggregates.Issuer>();
        foreach (var issuerId in issuerIds)
        {
            var issuer = await _issuerRepository.GetByIdAsync(issuerId, cancellationToken);
            if (issuer != null)
            {
                issuers[issuerId] = issuer;
            }
        }

        // Map to DTOs
        var items = credentials.Select(c => MapToSummaryDto(
            c,
            wallets.GetValueOrDefault(c.WalletId),
            issuers.GetValueOrDefault(c.IssuerId)
        )).ToList();

        return new PagedResult<CredentialSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Pagination.Page,
            PageSize = query.Pagination.PageSize
        };
    }

    private static CredentialSummaryDto MapToSummaryDto(
        Domain.Aggregates.Credential credential,
        Domain.Aggregates.Wallet? wallet,
        Domain.Aggregates.Issuer? issuer)
    {
        return new CredentialSummaryDto
        {
            Id = credential.Id.ToString(),
            Type = credential.CredentialType,
            HolderName = wallet?.Name ?? "Unknown Holder",
            IssuerName = issuer?.Name ?? "Unknown Issuer",
            IssuanceDate = credential.IssuedAt.DateTime,
            ExpirationDate = credential.ExpiresAt?.DateTime,
            Status = credential.Status.ToString(),
            IsExpired = credential.ExpiresAt.HasValue && credential.ExpiresAt.Value < DateTimeOffset.UtcNow,
            IsRevoked = credential.RevokedAt.HasValue
        };
    }
}

// Custom specification for searching
public class SearchCredentialsSpecification : Specification<Domain.Aggregates.Credential>
{
    public SearchCredentialsSpecification(string searchTerm)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            AddCriteria(c =>
                c.CredentialType.ToLower().Contains(term) ||
                c.CredentialId.ToLower().Contains(term) ||
                c.SchemaId.ToLower().Contains(term) ||
                c.CredentialData.ToLower().Contains(term));
        }

        AddInclude(c => c.Wallet!);
        AddInclude(c => c.Issuer!);
    }
}