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

namespace NumbatWallet.Application.Queries.Credentials;

public record ListCredentialsQuery : IQuery<PagedResult<CredentialSummaryDto>>
{
    public required Guid WalletId { get; init; }
    public CredentialFilter? Filter { get; init; }
    public PaginationParams Pagination { get; init; } = new PaginationParams();
    public SortingParams? Sorting { get; init; }
}

public record CredentialFilter
{
    public CredentialStatus[]? Statuses { get; init; }
    public string[]? Types { get; init; }
    public Guid[]? IssuerIds { get; init; }
    public bool? IncludeExpired { get; init; }
    public bool? IncludeRevoked { get; init; }
    public DateTimeOffset? IssuedAfter { get; init; }
    public DateTimeOffset? IssuedBefore { get; init; }
}

public record PaginationParams
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public int Skip => (Page - 1) * PageSize;
}

public record SortingParams
{
    public string SortBy { get; init; } = "IssuedAt";
    public bool Ascending { get; init; } = false;
}

public record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public class ListCredentialsQueryHandler : IQueryHandler<ListCredentialsQuery, PagedResult<CredentialSummaryDto>>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly ILogger<ListCredentialsQueryHandler> _logger;

    public ListCredentialsQueryHandler(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        IIssuerRepository issuerRepository,
        ILogger<ListCredentialsQueryHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _issuerRepository = issuerRepository;
        _logger = logger;
    }

    public async Task<PagedResult<CredentialSummaryDto>> HandleAsync(
        ListCredentialsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Listing credentials for wallet {WalletId} with pagination {Page}/{PageSize}",
            query.WalletId, query.Pagination.Page, query.Pagination.PageSize);

        // Verify wallet exists
        var wallet = await _walletRepository.GetByIdAsync(query.WalletId, cancellationToken);
        if (wallet == null)
        {
            return new PagedResult<CredentialSummaryDto>
            {
                Items = Array.Empty<CredentialSummaryDto>(),
                TotalCount = 0,
                Page = query.Pagination.Page,
                PageSize = query.Pagination.PageSize
            };
        }

        // Build specification
        var spec = new CredentialByWalletSpecification(query.WalletId, query.Filter?.IncludeRevoked ?? false);

        // Apply filters
        if (query.Filter != null)
        {
            spec = ApplyFilters(spec, query.Filter);
        }

        // Get total count
        var totalCount = await _credentialRepository.CountAsync(spec, cancellationToken);

        // Apply sorting and pagination
        spec.ApplyPaging(query.Pagination.Skip, query.Pagination.PageSize);

        if (query.Sorting != null)
        {
            if (query.Sorting.Ascending)
                spec.ApplyOrderBy(GetSortExpression(query.Sorting.SortBy));
            else
                spec.ApplyOrderByDescending(GetSortExpression(query.Sorting.SortBy));
        }

        // Get credentials
        var credentials = await _credentialRepository.FindAsync(spec, cancellationToken);

        // Get issuer details for mapping
        var issuerIds = credentials.Select(c => c.IssuerId).Distinct().ToArray();
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
        var items = credentials.Select(c => MapToSummaryDto(c, wallet, issuers.GetValueOrDefault(c.IssuerId))).ToList();

        return new PagedResult<CredentialSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Pagination.Page,
            PageSize = query.Pagination.PageSize
        };
    }

    private static CredentialByWalletSpecification ApplyFilters(
        CredentialByWalletSpecification spec,
        CredentialFilter filter)
    {
        if (filter.Statuses != null && filter.Statuses.Length > 0)
        {
            spec.AddCriteria(c => filter.Statuses.Contains(c.Status));
        }

        if (filter.Types != null && filter.Types.Length > 0)
        {
            spec.AddCriteria(c => filter.Types.Contains(c.CredentialType));
        }

        if (filter.IssuerIds != null && filter.IssuerIds.Length > 0)
        {
            spec.AddCriteria(c => filter.IssuerIds.Contains(c.IssuerId));
        }

        if (filter.IncludeExpired == false)
        {
            spec.AddCriteria(c => !c.ExpiresAt.HasValue || c.ExpiresAt > DateTimeOffset.UtcNow);
        }

        if (filter.IncludeRevoked == false)
        {
            spec.AddCriteria(c => !c.RevokedAt.HasValue);
        }

        if (filter.IssuedAfter.HasValue)
        {
            spec.AddCriteria(c => c.IssuedAt >= filter.IssuedAfter.Value);
        }

        if (filter.IssuedBefore.HasValue)
        {
            spec.AddCriteria(c => c.IssuedAt <= filter.IssuedBefore.Value);
        }

        return spec;
    }

    private static System.Linq.Expressions.Expression<Func<Domain.Aggregates.Credential, object>> GetSortExpression(string sortBy)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "type" => c => c.CredentialType,
            "status" => c => c.Status,
            "issuer" => c => c.IssuerId,
            "expiresat" => c => c.ExpiresAt ?? DateTimeOffset.MaxValue,
            _ => c => c.IssuedAt
        };
    }

    private static CredentialSummaryDto MapToSummaryDto(
        Domain.Aggregates.Credential credential,
        Domain.Aggregates.Wallet wallet,
        Domain.Aggregates.Issuer? issuer)
    {
        return new CredentialSummaryDto
        {
            Id = credential.Id.ToString(),
            Type = credential.CredentialType,
            HolderName = wallet.Name,
            IssuerName = issuer?.Name ?? "Unknown Issuer",
            IssuanceDate = credential.IssuedAt.DateTime,
            ExpirationDate = credential.ExpiresAt?.DateTime,
            Status = credential.Status.ToString(),
            IsExpired = credential.ExpiresAt.HasValue && credential.ExpiresAt.Value < DateTimeOffset.UtcNow,
            IsRevoked = credential.RevokedAt.HasValue
        };
    }
}