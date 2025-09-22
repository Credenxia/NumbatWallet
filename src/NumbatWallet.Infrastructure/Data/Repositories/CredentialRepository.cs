using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Infrastructure.Data.Repositories;

public class CredentialRepository : RepositoryBase<Credential, Guid>, ICredentialRepository
{
    public CredentialRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Credential>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(c => c.WalletId == walletId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Credential>> GetByIssuerIdAsync(Guid issuerId, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(c => c.IssuerId == issuerId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Credential>> GetActiveCredentialsAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.WalletId == walletId && c.Status == CredentialStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Credential>> GetExpiredCredentialsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Status == CredentialStatus.Expired ||
                       (c.ExpiresAt.HasValue && c.ExpiresAt.Value <= DateTimeOffset.UtcNow))
            .ToListAsync(cancellationToken);
    }

    public async Task<Credential?> GetByWalletAndTypeAsync(Guid walletId, string credentialType, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.WalletId == walletId && c.CredentialType == credentialType, cancellationToken);
    }

    public async Task<IEnumerable<Credential>> GetExpiringCredentialsAsync(int daysBeforeExpiry, CancellationToken cancellationToken = default)
    {
        var expiryDate = DateTimeOffset.UtcNow.AddDays(daysBeforeExpiry);
        return await DbSet
            .Where(c => c.ExpiresAt != null && c.ExpiresAt <= expiryDate && c.Status == CredentialStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasCredentialAsync(Guid walletId, string schemaId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(c => c.WalletId == walletId && c.SchemaId == schemaId, cancellationToken);
    }
}
