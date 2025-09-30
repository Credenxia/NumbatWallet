using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.Data.Repositories;

namespace NumbatWallet.Infrastructure.Repositories;

public class CertificateAuthorityRepository : RepositoryBase<CertificateAuthority, Guid>, ICertificateAuthorityRepository
{
    public CertificateAuthorityRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<CertificateAuthority?> GetByThumbprintAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(ca => ca.Thumbprint == thumbprint.ToUpperInvariant(), cancellationToken);
    }

    public async Task<CertificateAuthority?> GetBySubjectDnAsync(string subjectDn, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(ca => ca.SubjectDn == subjectDn, cancellationToken);
    }

    public async Task<IEnumerable<CertificateAuthority>> GetTrustedAuthoritiesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(ca => ca.IsTrusted)
            .OrderBy(ca => ca.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CertificateAuthority>> GetByTrustLevelAsync(CertificateTrustLevel minTrustLevel, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(ca => ca.IsTrusted && ca.TrustLevel >= minTrustLevel)
            .OrderBy(ca => ca.TrustLevel)
            .ThenBy(ca => ca.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsAuthorityTrustedAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(ca => ca.Thumbprint == thumbprint.ToUpperInvariant() && ca.IsTrusted, cancellationToken);
    }
}
