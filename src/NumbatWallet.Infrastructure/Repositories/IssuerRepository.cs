using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Infrastructure.Repositories;

public class IssuerRepository : RepositoryBase<Issuer, Guid>, IIssuerRepository
{
    public IssuerRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<Issuer?> GetByDidAsync(string did, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(i => i.IssuerDid == did, cancellationToken);
    }

    public async Task<Issuer?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(i => i.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> GetActiveIssuersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.Status == IssuerStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> GetTrustedIssuersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.IsTrusted && i.Status == IssuerStatus.Active)
            .OrderByDescending(i => i.TrustLevel)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> GetByCredentialTypeAsync(string credentialType, CancellationToken cancellationToken = default)
    {
        // This needs to query the SupportedCredentialTypes collection
        return await _dbSet
            .Where(i => i.SupportedCredentialTypes.Any(ct => ct.TypeName == credentialType))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> GetByJurisdictionAsync(string jurisdiction, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.Jurisdiction == jurisdiction && i.Status == IssuerStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DidExistsAsync(string did, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(i => i.IssuerDid == did, cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(i => i.Code == code, cancellationToken);
    }

    public async Task<Dictionary<IssuerStatus, int>> GetStatisticsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var statistics = await _dbSet
            .Where(i => i.TenantId == tenantId)
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        // Ensure all statuses are represented
        foreach (var status in Enum.GetValues<IssuerStatus>())
        {
            if (!statistics.ContainsKey(status))
            {
                statistics[status] = 0;
            }
        }

        return statistics;
    }
}