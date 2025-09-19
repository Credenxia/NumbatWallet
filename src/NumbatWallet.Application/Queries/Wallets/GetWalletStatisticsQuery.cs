using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Repositories;

namespace NumbatWallet.Application.Queries.Wallets;

public record GetWalletStatisticsQuery : IQuery<WalletStatisticsDto>
{
    public string? PersonId { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
}

public record WalletStatisticsDto
{
    public int TotalWallets { get; init; }
    public int ActiveWallets { get; init; }
    public int SuspendedWallets { get; init; }
    public int InactiveWallets { get; init; }
    public int TotalCredentials { get; init; }
    public int ActiveCredentials { get; init; }
    public int ExpiredCredentials { get; init; }
    public int RevokedCredentials { get; init; }
    public Dictionary<string, int> WalletsByStatus { get; init; } = new();
    public Dictionary<string, int> CredentialsByType { get; init; } = new();
    public DateTimeOffset GeneratedAt { get; init; }
}

public class GetWalletStatisticsQueryHandler : IQueryHandler<GetWalletStatisticsQuery, WalletStatisticsDto>
{
    private readonly IWalletRepository _walletRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly ILogger<GetWalletStatisticsQueryHandler> _logger;

    public GetWalletStatisticsQueryHandler(
        IWalletRepository walletRepository,
        ICredentialRepository credentialRepository,
        ILogger<GetWalletStatisticsQueryHandler> logger)
    {
        _walletRepository = walletRepository;
        _credentialRepository = credentialRepository;
        _logger = logger;
    }

    public async Task<WalletStatisticsDto> HandleAsync(
        GetWalletStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating wallet statistics");

        // Get wallets based on filters
        IEnumerable<Domain.Aggregates.Wallet> wallets;
        
        if (!string.IsNullOrWhiteSpace(query.PersonId))
        {
            var personId = Guid.Parse(query.PersonId);
            wallets = await _walletRepository.FindAsync(
                new Domain.Specifications.WalletByPersonSpecification(personId),
                cancellationToken);
        }
        else
        {
            wallets = await _walletRepository.GetAllAsync(cancellationToken);
        }

        // Apply date filters
        if (query.StartDate.HasValue)
        {
            wallets = wallets.Where(w => w.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            wallets = wallets.Where(w => w.CreatedAt <= query.EndDate.Value);
        }

        var walletList = wallets.ToList();

        // Calculate wallet statistics
        var totalWallets = walletList.Count;
        var activeWallets = walletList.Count(w => w.Status == NumbatWallet.SharedKernel.Enums.WalletStatus.Active);
        var suspendedWallets = walletList.Count(w => w.Status == NumbatWallet.SharedKernel.Enums.WalletStatus.Suspended);
        var inactiveWallets = walletList.Count(w => w.Status == NumbatWallet.SharedKernel.Enums.WalletStatus.Locked);

        // Group wallets by status
        var walletsByStatus = walletList
            .GroupBy(w => w.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Get credentials for all wallets
        var allCredentials = new List<Domain.Aggregates.Credential>();
        foreach (var wallet in walletList)
        {
            var credentials = await _credentialRepository.FindAsync(
                new Domain.Specifications.CredentialByWalletSpecification(wallet.Id),
                cancellationToken);
            allCredentials.AddRange(credentials);
        }

        // Calculate credential statistics
        var totalCredentials = allCredentials.Count;
        var activeCredentials = allCredentials.Count(c => c.Status == NumbatWallet.SharedKernel.Enums.CredentialStatus.Active && !c.IsExpired());
        var expiredCredentials = allCredentials.Count(c => c.IsExpired());
        var revokedCredentials = allCredentials.Count(c => c.Status == NumbatWallet.SharedKernel.Enums.CredentialStatus.Revoked);

        // Group credentials by type
        var credentialsByType = allCredentials
            .GroupBy(c => c.CredentialType)
            .ToDictionary(g => g.Key, g => g.Count());

        return new WalletStatisticsDto
        {
            TotalWallets = totalWallets,
            ActiveWallets = activeWallets,
            SuspendedWallets = suspendedWallets,
            InactiveWallets = inactiveWallets,
            TotalCredentials = totalCredentials,
            ActiveCredentials = activeCredentials,
            ExpiredCredentials = expiredCredentials,
            RevokedCredentials = revokedCredentials,
            WalletsByStatus = walletsByStatus,
            CredentialsByType = credentialsByType,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}