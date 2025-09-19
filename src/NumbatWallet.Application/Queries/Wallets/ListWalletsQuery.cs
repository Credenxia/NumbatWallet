using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Credentials;
using NumbatWallet.Domain.Repositories;
using NumbatWallet.Domain.Specifications;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Application.Queries.Wallets;

public record ListWalletsQuery : IQuery<PagedResult<WalletDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? PersonId { get; init; }
    public string? Status { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsSuspended { get; init; }
    public string? SortBy { get; init; } = "CreatedAt";
    public bool SortDescending { get; init; } = true;
}

public class ListWalletsQueryHandler : IQueryHandler<ListWalletsQuery, PagedResult<WalletDto>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly ILogger<ListWalletsQueryHandler> _logger;

    public ListWalletsQueryHandler(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        ICredentialRepository credentialRepository,
        ILogger<ListWalletsQueryHandler> logger)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _credentialRepository = credentialRepository;
        _logger = logger;
    }

    public async Task<PagedResult<WalletDto>> HandleAsync(
        ListWalletsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing wallets with filters - Page: {Page}, PageSize: {PageSize}",
            query.Page, query.PageSize);

        // Build specification
        ISpecification<Domain.Aggregates.Wallet>? specification = null;

        if (!string.IsNullOrWhiteSpace(query.PersonId))
        {
            var personId = Guid.Parse(query.PersonId);
            specification = new WalletByPersonSpecification(personId);
        }

        if (query.IsActive.HasValue)
        {
            // Filter by active status directly in the results
        }

        // Get all matching wallets
        var wallets = specification != null
            ? await _walletRepository.FindAsync(specification, cancellationToken)
            : await _walletRepository.GetAllAsync(cancellationToken);

        // Apply additional filters
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            wallets = wallets.Where(w => w.Status.ToString().Equals(query.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (query.IsSuspended.HasValue)
        {
            wallets = wallets.Where(w => (w.Status == NumbatWallet.SharedKernel.Enums.WalletStatus.Suspended) == query.IsSuspended.Value);
        }

        // Apply sorting
        wallets = query.SortBy?.ToLowerInvariant() switch
        {
            "name" => query.SortDescending
                ? wallets.OrderByDescending(w => w.Name)
                : wallets.OrderBy(w => w.Name),
            "updatedat" => query.SortDescending
                ? wallets.OrderByDescending(w => w.CreatedAt)
                : wallets.OrderBy(w => w.CreatedAt),
            _ => query.SortDescending
                ? wallets.OrderByDescending(w => w.CreatedAt)
                : wallets.OrderBy(w => w.CreatedAt)
        };

        // Get total count before pagination
        var totalCount = wallets.Count();

        // Apply pagination
        var pagedWallets = wallets
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        // Get person details for all wallets
        var personIds = pagedWallets.Select(w => w.PersonId).Distinct();
        var persons = new Dictionary<Guid, Domain.Aggregates.Person>();
        foreach (var personId in personIds)
        {
            var person = await _personRepository.GetByIdAsync(personId, cancellationToken);
            if (person != null)
            {
                persons[personId] = person;
            }
        }

        // Map to DTOs
        var dtos = new List<WalletDto>();
        foreach (var wallet in pagedWallets)
        {
            var person = persons.GetValueOrDefault(wallet.PersonId);
            
            // Get credential count
            var credentials = await _credentialRepository.FindAsync(
                new CredentialByWalletSpecification(wallet.Id),
                cancellationToken);

            dtos.Add(new WalletDto
            {
                Id = wallet.Id.ToString(),
                PersonId = wallet.PersonId.ToString(),
                PersonName = person != null ? $"{person.FirstName} {person.LastName}" : "Unknown",
                Name = wallet.Name,
                Status = wallet.Status.ToString(),
                IsActive = wallet.Status == NumbatWallet.SharedKernel.Enums.WalletStatus.Active,
                IsSuspended = wallet.Status == NumbatWallet.SharedKernel.Enums.WalletStatus.Suspended,
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.CreatedAt,
                CredentialCount = credentials.Count(),
                Metadata = new Dictionary<string, string>()
            });
        }

        return new PagedResult<WalletDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}