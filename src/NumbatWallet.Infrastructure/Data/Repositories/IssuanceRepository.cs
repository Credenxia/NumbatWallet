using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Issuance entity
/// </summary>
public class IssuanceRepository : RepositoryBase<Issuance, Guid>, IIssuanceRepository
{
    public IssuanceRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get issuances by wallet ID
    /// </summary>
    public async Task<IReadOnlyList<Issuance>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var result = await DbSet
            .Where(i => i.WalletId == walletId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }

    /// <summary>
    /// Get issuances by status
    /// </summary>
    public async Task<IReadOnlyList<Issuance>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var result = await DbSet
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }

    /// <summary>
    /// Get pending issuances
    /// </summary>
    public async Task<IReadOnlyList<Issuance>> GetPendingIssuancesAsync(string? assignedTo = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(i => i.Status == IssuanceStatus.Pending);

        // If assignedTo is provided, filter by it (would need to add AssignedTo property to Issuance entity)
        // For now, just return all pending

        var result = await query
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }

    /// <summary>
    /// Get issuances by credential type
    /// </summary>
    public async Task<IReadOnlyList<Issuance>> GetByCredentialTypeAsync(string credentialType, CancellationToken cancellationToken = default)
    {
        var result = await DbSet
            .Where(i => i.CredentialType == credentialType)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }

    /// <summary>
    /// Get issuance by credential ID
    /// </summary>
    public async Task<Issuance?> GetByCredentialIdAsync(Guid credentialId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(i => i.CredentialId == credentialId, cancellationToken);
    }

    /// <summary>
    /// Get expiring issuances
    /// </summary>
    public async Task<IReadOnlyList<Issuance>> GetExpiringIssuancesAsync(DateTime expiryThreshold, CancellationToken cancellationToken = default)
    {
        var result = await DbSet
            .Where(i => i.Status == IssuanceStatus.Pending &&
                       i.ExpiryDate != null &&
                       i.ExpiryDate.Value <= expiryThreshold)
            .ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }

    /// <summary>
    /// Override to include related data when needed
    /// </summary>
    public override async Task<Issuance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    /// <summary>
    /// Save changes to the database
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }
}