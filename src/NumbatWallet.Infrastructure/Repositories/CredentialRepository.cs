using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Models;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Infrastructure.Repositories;

public class CredentialRepository : RepositoryBase<Credential, Guid>, ICredentialRepository
{
    public CredentialRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<Credential?> GetByDidAsync(string did, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.CredentialId == did, cancellationToken);
    }

    async Task<IEnumerable<Credential>> ICredentialRepository.GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(c => c.WalletId == walletId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.WalletId == walletId)
            .ToListAsync(cancellationToken);
    }

    async Task<IEnumerable<Credential>> ICredentialRepository.GetByIssuerIdAsync(Guid issuerId, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(c => c.IssuerId == issuerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetByIssuerIdAsync(Guid issuerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.IssuerId == issuerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetByTypeAsync(string credentialType, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.CredentialType == credentialType)
            .ToListAsync(cancellationToken);
    }

    async Task<IEnumerable<Credential>> ICredentialRepository.GetActiveCredentialsAsync(Guid walletId, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(c => c.WalletId == walletId && c.Status == CredentialStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetActiveCredentialsAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.WalletId == walletId && c.Status == CredentialStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Credential>> GetExpiredCredentialsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Status == CredentialStatus.Active &&
                       c.ExpiresAt.HasValue &&
                       c.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<Credential?> GetByWalletAndTypeAsync(Guid walletId, string credentialType, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.WalletId == walletId &&
                                     c.CredentialType == credentialType &&
                                     c.Status == CredentialStatus.Active,
                                cancellationToken);
    }

    async Task<IEnumerable<Credential>> ICredentialRepository.FindAsync(ISpecification<Credential> specification, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(specification.ToExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetExpiringCredentialsAsync(int daysBeforeExpiry, CancellationToken cancellationToken = default)
    {
        var expiryThreshold = DateTimeOffset.UtcNow.AddDays(daysBeforeExpiry);

        return await DbSet
            .Where(c => c.Status == CredentialStatus.Active &&
                       c.ExpiresAt.HasValue &&
                       c.ExpiresAt.Value <= expiryThreshold &&
                       c.ExpiresAt.Value > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetRevokedCredentialsAsync(Guid issuerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.IssuerId == issuerId && c.Status == CredentialStatus.Revoked)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DidExistsAsync(string did, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(c => c.CredentialId == did, cancellationToken);
    }

    public async Task<Dictionary<CredentialStatus, int>> GetStatisticsAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var statistics = await DbSet
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

    public async Task<IReadOnlyList<Credential>> GetByStatusAsync(CredentialStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<Credential?> GetByCredentialIdAsync(string credentialId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.CredentialId == credentialId, cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetRevokedCredentialsAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Status == CredentialStatus.Revoked && c.ModifiedAt >= since)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetBySchemaAsync(string schemaId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.SchemaId == schemaId)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResponse<Credential>> GetPagedAsync(
        PagedRequest request,
        ISpecification<Credential>? specification = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (specification != null)
        {
            query = query.Where(specification.ToExpression());
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<Credential>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<int> CountByStatusAsync(CredentialStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(c => c.Status == status, cancellationToken);
    }

    public async Task<int> CountByTypeAsync(string credentialType, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(c => c.CredentialType == credentialType, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetCredentialStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var statistics = await DbSet
            .GroupBy(c => c.CredentialType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);

        return statistics;
    }

    public async Task<bool> IsCredentialIdUniqueAsync(string credentialId, CancellationToken cancellationToken = default)
    {
        return !await DbSet
            .AnyAsync(c => c.CredentialId == credentialId, cancellationToken);
    }

    public async Task<bool> HasActiveCredentialOfTypeAsync(Guid walletId, string credentialType, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(c => c.WalletId == walletId &&
                          c.CredentialType == credentialType &&
                          c.Status == CredentialStatus.Active,
                     cancellationToken);
    }

    public async Task<IReadOnlyList<Credential>> GetBatchAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeExpiredCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var expiredCredentials = await DbSet
            .Where(c => c.Status == CredentialStatus.Active &&
                       c.ExpiresAt.HasValue &&
                       c.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var credential in expiredCredentials)
        {
            credential.Revoke("Expired");
        }

        await Context.SaveChangesAsync(cancellationToken);
    }
}