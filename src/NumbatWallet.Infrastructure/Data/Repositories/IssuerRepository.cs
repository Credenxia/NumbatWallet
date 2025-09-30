using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Data.Repositories;

public class IssuerRepository : RepositoryBase<Issuer, Guid>, IIssuerRepository
{
    public IssuerRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<Issuer?> GetByDidAsync(string issuerDid, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(i => i.IssuerDid == issuerDid, cancellationToken);
    }

    public async Task<IEnumerable<Issuer>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        // OrganizationId relationship not yet implemented in domain model
        // For now, return all issuers - this will be enhanced when Organization aggregate is linked
        return await DbSet.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Issuer>> GetTrustedIssuersAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(i => i.IsTrusted).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Issuer>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(i => i.TenantId == tenantId.ToString()).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Issuer>> GetActiveIssuersAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(i => i.IsActive).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Issuer>> GetIssuersBySupportedCredentialTypeAsync(string credentialType, CancellationToken cancellationToken = default)
    {
        // Load all issuers and filter in-memory using domain logic
        // EF Core cannot translate SupportsCredentialType method to SQL
        var allIssuers = await DbSet.Where(i => i.IsActive).ToListAsync(cancellationToken);
        return allIssuers.Where(i => i.SupportsCredentialType(credentialType));
    }

    public async Task<bool> IssuerExistsAsync(string issuerDid, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(i => i.IssuerDid == issuerDid, cancellationToken);
    }

    public async Task<bool> CanIssueCredentialTypeAsync(string issuerDid, string credentialType, CancellationToken cancellationToken = default)
    {
        var issuer = await DbSet.FirstOrDefaultAsync(i => i.IssuerDid == issuerDid, cancellationToken);
        return issuer != null && issuer.IsActive && issuer.SupportsCredentialType(credentialType);
    }
}
