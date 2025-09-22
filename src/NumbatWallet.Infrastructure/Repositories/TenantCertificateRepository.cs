using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.Data.Repositories;

namespace NumbatWallet.Infrastructure.Repositories;

public class TenantCertificateRepository : RepositoryBase<TenantCertificate, Guid>, ITenantCertificateRepository
{
    public TenantCertificateRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<TenantCertificate?> GetByThumbprintAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.Thumbprint == thumbprint.ToUpperInvariant(), cancellationToken);
    }

    public async Task<IEnumerable<TenantCertificate>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TenantCertificate>> GetActiveCertificatesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.TenantId == tenantId && c.IsActive && !c.RevokedAt.HasValue)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TenantCertificate>> GetExpiringCertificatesAsync(int daysBeforeExpiry, CancellationToken cancellationToken = default)
    {
        var expiryDate = DateTimeOffset.UtcNow.AddDays(daysBeforeExpiry);
        return await DbSet
            .Where(c => c.ValidTo <= expiryDate && c.ValidTo > DateTimeOffset.UtcNow && c.IsActive && !c.RevokedAt.HasValue)
            .OrderBy(c => c.ValidTo)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ThumbprintExistsAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(c => c.Thumbprint == thumbprint.ToUpperInvariant(), cancellationToken);
    }

    public async Task<IEnumerable<TenantCertificate>> GetByPurposeAsync(CertificatePurpose purpose, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Purpose == purpose || c.Purpose == CertificatePurpose.All)
            .Where(c => c.IsActive && !c.RevokedAt.HasValue)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TenantCertificate>> GetBySubjectDnAsync(string subjectDn, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.SubjectDn == subjectDn)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}