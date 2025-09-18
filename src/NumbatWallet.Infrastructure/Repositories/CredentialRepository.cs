using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Infrastructure.Repositories;

public class CredentialRepository : RepositoryBase<Credential, Guid>, ICredentialRepository
{
    public CredentialRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<Credential?> GetByDidAsync(string did, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.CredentialDid == did, cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.WalletId == walletId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetByIssuerIdAsync(Guid issuerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.IssuerId == issuerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetByTypeAsync(string credentialType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.CredentialType == credentialType)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetActiveCredentialsAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.WalletId == walletId && c.Status == CredentialStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetExpiringCredentialsAsync(int daysBeforeExpiry, CancellationToken cancellationToken = default)
    {
        var expiryThreshold = DateTimeOffset.UtcNow.AddDays(daysBeforeExpiry);

        return await _dbSet
            .Where(c => c.Status == CredentialStatus.Active &&
                       c.ExpiresAt.HasValue &&
                       c.ExpiresAt.Value <= expiryThreshold &&
                       c.ExpiresAt.Value > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetRevokedCredentialsAsync(Guid issuerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.IssuerId == issuerId && c.Status == CredentialStatus.Revoked)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DidExistsAsync(string did, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(c => c.CredentialDid == did, cancellationToken);
    }

    public async Task<Dictionary<CredentialStatus, int>> GetStatisticsAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var statistics = await _dbSet
            .Where(c => c.WalletId == walletId)
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        // Ensure all statuses are represented
        foreach (var status in Enum.GetValues<CredentialStatus>())
        {
            if (!statistics.ContainsKey(status))
            {
                statistics[status] = 0;
            }
        }

        return statistics;
    }
}