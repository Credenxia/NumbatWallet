using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Queries.Credentials;

public record SearchCredentialsQuery : IQuery<SearchCredentialsResult>
{
    public required string SearchText { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool IncludeRevoked { get; init; } = false;
    public bool IncludeExpired { get; init; } = false;
    public Guid? TenantId { get; init; }
}

public record SearchCredentialsResult
{
    public required IReadOnlyList<CredentialSearchResultDto> Results { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required string SearchText { get; init; }
}

public record CredentialSearchResultDto
{
    public required string Id { get; init; }
    public required string WalletId { get; init; }
    public required string HolderName { get; init; }
    public required string IssuerName { get; init; }
    public required string Type { get; init; }
    public required string Status { get; init; }
    public float RelevanceScore { get; init; }
    public string? MatchedField { get; init; }
}

public class SearchCredentialsQueryHandler : IQueryHandler<SearchCredentialsQuery, SearchCredentialsResult>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly ILogger<SearchCredentialsQueryHandler> _logger;

    public SearchCredentialsQueryHandler(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        IIssuerRepository issuerRepository,
        ILogger<SearchCredentialsQueryHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _issuerRepository = issuerRepository;
        _logger = logger;
    }

    public async Task<SearchCredentialsResult> HandleAsync(
        SearchCredentialsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching credentials with text: {SearchText}", query.SearchText);

        // Get all credentials (in production, this would use full-text search)
        var allCredentials = await _credentialRepository.GetAllAsync(cancellationToken);

        // Filter by search text
        var searchedCredentials = allCredentials
            .Where(c => MatchesSearchText(c, query.SearchText));

        // Apply additional filters
        if (!query.IncludeRevoked)
        {
            searchedCredentials = searchedCredentials.Where(c => c.Status != CredentialStatus.Revoked);
        }

        if (!query.IncludeExpired)
        {
            searchedCredentials = searchedCredentials.Where(c => !c.IsExpired());
        }

        // Order by relevance (simplified) and materialize to avoid multiple enumeration
        var credentialsList = searchedCredentials.OrderByDescending(c => c.CreatedAt).ToList();

        // Get total count
        var totalCount = credentialsList.Count;

        // Apply pagination
        var pagedCredentials = credentialsList
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        // Get related data for mapping
        var walletIds = pagedCredentials.Select(c => c.WalletId).Distinct();
        var issuerIds = pagedCredentials.Select(c => c.IssuerId).Distinct();

        var wallets = new Dictionary<Guid, Domain.Aggregates.Wallet>();
        var persons = new Dictionary<Guid, Domain.Aggregates.Person>();
        var issuers = new Dictionary<Guid, Domain.Aggregates.Issuer>();

        foreach (var walletId in walletIds)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);
            if (wallet != null)
            {
                wallets[walletId] = wallet;
                var person = await _personRepository.GetByIdAsync(wallet.PersonId, cancellationToken);
                if (person != null)
                {
                    persons[wallet.PersonId] = person;
                }
            }
        }

        foreach (var issuerId in issuerIds)
        {
            var issuer = await _issuerRepository.GetByIdAsync(issuerId, cancellationToken);
            if (issuer != null)
            {
                issuers[issuerId] = issuer;
            }
        }

        // Map to DTOs
        var results = pagedCredentials.Select(c =>
        {
            var wallet = wallets.GetValueOrDefault(c.WalletId);
            var person = wallet != null ? persons.GetValueOrDefault(wallet.PersonId) : null;
            var issuer = issuers.GetValueOrDefault(c.IssuerId);

            return new CredentialSearchResultDto
            {
                Id = c.Id.ToString(),
                WalletId = c.WalletId.ToString(),
                HolderName = person != null ? $"{person.FirstName} {person.LastName}" : "Unknown",
                IssuerName = issuer?.Name ?? "Unknown Issuer",
                Type = c.CredentialType,
                Status = c.Status.ToString(),
                RelevanceScore = CalculateRelevanceScore(c, query.SearchText),
                MatchedField = GetMatchedField(c, query.SearchText)
            };
        }).ToList();

        return new SearchCredentialsResult
        {
            Results = results,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            SearchText = query.SearchText
        };
    }

    private static bool MatchesSearchText(Domain.Aggregates.Credential credential, string searchText)
    {
        var lowerSearch = searchText.ToLowerInvariant();

        return credential.CredentialType.ToLowerInvariant().Contains(lowerSearch, StringComparison.InvariantCulture) ||
               credential.CredentialId.ToLowerInvariant().Contains(lowerSearch, StringComparison.InvariantCulture) ||
               credential.Id.ToString().ToLowerInvariant().Contains(lowerSearch, StringComparison.InvariantCulture) ||
               credential.Status.ToString().ToLowerInvariant().Contains(lowerSearch, StringComparison.InvariantCulture);
    }

    private static float CalculateRelevanceScore(Domain.Aggregates.Credential credential, string searchText)
    {
        float score = 0;

        // Exact match scores higher
        if (credential.CredentialId.Equals(searchText, StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
        }

        // Type match
        if (credential.CredentialType.Contains(searchText, StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
        }

        // Status match
        if (credential.Status.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase))
        {
            score += 3;
        }

        return score;
    }

    private static string? GetMatchedField(Domain.Aggregates.Credential credential, string searchText)
    {
        if (credential.CredentialId.Contains(searchText, StringComparison.OrdinalIgnoreCase))
        {
            return "CredentialId";
        }

        if (credential.CredentialType.Contains(searchText, StringComparison.OrdinalIgnoreCase))
        {
            return "Type";
        }

        if (credential.Status.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase))
        {
            return "Status";
        }

        return null;
    }
}