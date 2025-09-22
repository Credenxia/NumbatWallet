using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.Data.Repositories;

namespace NumbatWallet.Infrastructure.Repositories;

public class CertificateTrustStoreRepository : RepositoryBase<CertificateTrustStore, Guid>, ICertificateTrustStoreRepository
{
    public CertificateTrustStoreRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<CertificateTrustStore?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(ts => ts.TenantId == tenantId, cancellationToken);
    }

    public async Task<CertificateTrustStore?> GetActiveByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(ts => ts.TenantId == tenantId && ts.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<CertificateTrustStore>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(ts => ts.IsActive)
            .OrderBy(ts => ts.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsCertificateRevokedAsync(string thumbprint, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var trustStore = await GetActiveByTenantIdAsync(tenantId, cancellationToken);
        if (trustStore == null)
        {
            return false;
        }

        return trustStore.IsCertificateRevoked(thumbprint);
    }
}