using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Credentials;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Application.Specifications;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Application.Handlers.Credentials;

public class SearchCredentialsHandler : IQueryHandler<SearchCredentialsQuery, PagedResultDto<CredentialDto>>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly ILogger<SearchCredentialsHandler> _logger;

    public SearchCredentialsHandler(
        ICredentialRepository credentialRepository,
        ILogger<SearchCredentialsHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _logger = logger;
    }

    public async Task<PagedResultDto<CredentialDto>> HandleAsync(SearchCredentialsQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching credentials for tenant {TenantId} with term {SearchTerm}", query.TenantId, query.SearchTerm);

        var specification = BuildSpecification(query);

        var totalCount = await _credentialRepository.CountAsync(specification, cancellationToken);

        var skip = (query.PageNumber - 1) * query.PageSize;
        var credentials = await _credentialRepository.GetPagedAsync(
            specification,
            skip,
            query.PageSize,
            GetSortExpression(query.SortBy),
            query.SortDescending,
            cancellationToken);

        var items = credentials.Select(c => new CredentialDto
        {
            Id = c.Id.ToString(),
            HolderId = c.WalletId.ToString(),
            IssuerId = c.IssuerId.ToString(),
            Type = c.CredentialType,
            CredentialSubject = new Dictionary<string, object>(c.Claims),
            IssuanceDate = c.IssuedAt.DateTime,
            ExpirationDate = c.ExpiresAt?.DateTime,
            Status = c.Status.ToString(),
            Proof = null,
            Metadata = null,
            IsRevoked = c.Status == CredentialStatus.Revoked,
            RevocationDate = c.RevokedAt?.DateTime,
            RevocationReason = c.RevocationReason
        });

        return new PagedResultDto<CredentialDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    private CredentialSearchSpecification BuildSpecification(SearchCredentialsQuery query)
    {
        CredentialStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<CredentialStatus>(query.Status, true, out var parsedStatus))
            {
                status = parsedStatus;
            }
        }

        return new CredentialSearchSpecification(
            query.TenantId,
            query.SearchTerm,
            query.CredentialType,
            status);
    }

    private Expression<Func<Credential, object>> GetSortExpression(string? sortBy)
    {
        return sortBy?.ToLower(System.Globalization.CultureInfo.InvariantCulture) switch
        {
            "type" => c => c.CredentialType,
            "status" => c => c.Status,
            "issuedat" => c => c.IssuedAt,
            "expiresat" => c => c.ExpiresAt ?? DateTimeOffset.MaxValue,
            _ => c => c.CreatedAt
        };
    }
}