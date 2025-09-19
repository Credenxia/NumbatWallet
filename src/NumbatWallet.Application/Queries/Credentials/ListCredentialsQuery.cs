using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Queries.Credentials;

public record ListCredentialsQuery : IQuery<PagedResult<CredentialSummaryDto>>
{
    public required string WalletId { get; init; }
    public PaginationParams Pagination { get; init; } = new();
    public SortingParams? Sorting { get; init; }
    public CredentialFilter? Filter { get; init; }
}

public record PaginationParams
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int Skip => (Page - 1) * PageSize;
}

public record SortingParams
{
    public string SortBy { get; init; } = "CreatedAt";
    public bool Ascending { get; init; } = false;
}

public record CredentialFilter
{
    public CredentialStatus[]? Statuses { get; init; }
    public string[]? Types { get; init; }
    public Guid[]? IssuerIds { get; init; }
    public bool IncludeExpired { get; init; } = false;
    public bool IncludeRevoked { get; init; } = false;
    public DateTimeOffset? IssuedAfter { get; init; }
    public DateTimeOffset? IssuedBefore { get; init; }
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

public record CredentialSummaryDto
{
    public required string Id { get; init; }
    public required string WalletId { get; init; }
    public required string IssuerId { get; init; }
    public required string IssuerName { get; init; }
    public required string Type { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset IssuedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool IsExpired { get; init; }
    public bool IsRevoked { get; init; }
}

public class ListCredentialsQueryHandler : IQueryHandler<ListCredentialsQuery, PagedResult<CredentialSummaryDto>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly ILogger<ListCredentialsQueryHandler> _logger;

    public ListCredentialsQueryHandler(
        IWalletRepository walletRepository,
        ICredentialRepository credentialRepository,
        IIssuerRepository issuerRepository,
        ILogger<ListCredentialsQueryHandler> logger)
    {
        _walletRepository = walletRepository;
        _credentialRepository = credentialRepository;
        _issuerRepository = issuerRepository;
        _logger = logger;
    }

    public async Task<PagedResult<CredentialSummaryDto>> HandleAsync(
        ListCredentialsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing ListCredentialsQuery for wallet {WalletId}", query.WalletId);

        // Get wallet
        var walletId = Guid.Parse(query.WalletId);
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);
        if (wallet == null)
        {
            throw new EntityNotFoundException("Wallet", query.WalletId);
        }

        // Get credentials for wallet
        var allCredentials = await _credentialRepository.GetByWalletIdAsync(wallet.Id, cancellationToken);

        // Apply filters
        var filteredCredentials = ApplyManualFilters(allCredentials, query.Filter);

        // Apply sorting
        if (query.Sorting != null)
        {
            filteredCredentials = query.Sorting.SortBy?.ToLowerInvariant() switch
            {
                "issuedat" => query.Sorting.Ascending
                    ? filteredCredentials.OrderBy(c => c.IssuedAt)
                    : filteredCredentials.OrderByDescending(c => c.IssuedAt),
                "type" => query.Sorting.Ascending
                    ? filteredCredentials.OrderBy(c => c.CredentialType)
                    : filteredCredentials.OrderByDescending(c => c.CredentialType),
                _ => filteredCredentials.OrderByDescending(c => c.CreatedAt)
            };
        }

        // Materialize the query to avoid multiple enumeration
        var credentialsList = filteredCredentials.ToList();

        // Get total count
        var totalCount = credentialsList.Count;

        // Apply pagination
        var credentials = credentialsList
            .Skip(query.Pagination.Skip)
            .Take(query.Pagination.PageSize)
            .ToList();

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

    private static IEnumerable<Domain.Aggregates.Credential> ApplyManualFilters(
        IEnumerable<Domain.Aggregates.Credential> credentials,
        CredentialFilter? filter)
    {
        if (filter == null)
        {
            return credentials;
        }

        if (filter.Statuses != null && filter.Statuses.Length > 0)
        {
            credentials = credentials.Where(c => filter.Statuses.Contains(c.Status));
        }

        if (filter.Types != null && filter.Types.Length > 0)
        {
            credentials = credentials.Where(c => filter.Types.Contains(c.CredentialType));
        }

        if (filter.IssuerIds != null && filter.IssuerIds.Length > 0)
        {
            credentials = credentials.Where(c => filter.IssuerIds.Contains(c.IssuerId));
        }

        if (filter.IncludeExpired == false)
        {
            credentials = credentials.Where(c => !c.IsExpired());
        }

        if (filter.IncludeRevoked == false)
        {
            credentials = credentials.Where(c => c.Status != CredentialStatus.Revoked);
        }

        if (filter.IssuedAfter.HasValue)
        {
            credentials = credentials.Where(c => c.IssuedAt >= filter.IssuedAfter.Value);
        }

        if (filter.IssuedBefore.HasValue)
        {
            credentials = credentials.Where(c => c.IssuedAt <= filter.IssuedBefore.Value);
        }

        return credentials;
    }

    private static Expression<Func<Domain.Aggregates.Credential, object>> GetSortExpression(string sortBy)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "issuedat" => c => c.IssuedAt,
            "type" => c => c.CredentialType,
            "status" => c => c.Status,
            _ => c => c.CreatedAt
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
            WalletId = wallet.Id.ToString(),
            IssuerId = credential.IssuerId.ToString(),
            IssuerName = issuer?.Name ?? "Unknown Issuer",
            Type = credential.CredentialType,
            Status = credential.Status.ToString(),
            IssuedAt = credential.IssuedAt,
            ExpiresAt = credential.ExpiresAt,
            IsExpired = credential.IsExpired(),
            IsRevoked = credential.Status == CredentialStatus.Revoked
        };
    }
}