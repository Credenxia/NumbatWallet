using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Models;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Infrastructure.Repositories;

public class IssuerRepository : RepositoryBase<Issuer, Guid>, IIssuerRepository
{
    public IssuerRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<Issuer?> GetByDidAsync(string did, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(i => i.IssuerDid == did, cancellationToken);
    }

    public async Task<Issuer?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(i => i.Code == code, cancellationToken);
    }

    public async Task<Issuer?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(i => i.ExternalId == externalId, cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> GetByStatusAsync(IssuerStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> GetActiveIssuersAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.Status == IssuerStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> GetTrustedIssuersAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.IsTrusted && i.Status == IssuerStatus.Active)
            .OrderByDescending(i => i.TrustLevel)
            .ToListAsync(cancellationToken);
    }

    public async Task<Issuer?> GetWithCredentialsAsync(Guid issuerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include("Credentials")
            .FirstOrDefaultAsync(i => i.Id == issuerId, cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> GetByCredentialTypeAsync(string credentialType, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.SupportedCredentialTypes.Any(ct => ct.TypeName == credentialType))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> GetByJurisdictionAsync(string jurisdiction, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.Jurisdiction == jurisdiction && i.Status == IssuerStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> GetExpiringCertificatesAsync(int daysBeforeExpiry, CancellationToken cancellationToken = default)
    {
        var expiryThreshold = DateTimeOffset.UtcNow.AddDays(daysBeforeExpiry);

        return await DbSet
            .Where(i => i.CertificateExpiresAt.HasValue &&
                       i.CertificateExpiresAt.Value <= expiryThreshold &&
                       i.CertificateExpiresAt.Value > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLowerInvariant();

        return await DbSet
            .Where(i => i.Name.ToLowerInvariant().Contains(lowerSearchTerm) ||
                       i.Code.ToLowerInvariant().Contains(lowerSearchTerm) ||
                       (i.Description != null && i.Description.ToLowerInvariant().Contains(lowerSearchTerm)))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResponse<Issuer>> GetPagedAsync(
        PagedRequest request,
        ISpecification<Issuer>? specification = null,
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

        return new PagedResponse<Issuer>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<int> CountByStatusAsync(IssuerStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(i => i.Status == status, cancellationToken);
    }

    public async Task<int> CountTrustedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(i => i.IsTrusted, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetIssuerStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var statistics = await DbSet
            .GroupBy(i => i.Status.ToString())
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        return statistics;
    }

    public async Task<Dictionary<string, int>> GetCredentialTypeStatisticsAsync(CancellationToken cancellationToken = default)
    {
        // This would require a more complex query to flatten the credential types
        var issuers = await DbSet.Include("SupportedCredentialTypes").ToListAsync(cancellationToken);

        var statistics = issuers
            .SelectMany(i => i.SupportedCredentialTypes)
            .GroupBy(ct => ct.TypeName)
            .ToDictionary(g => g.Key, g => g.Count());

        return statistics;
    }

    public async Task<bool> IsDidUniqueAsync(string did, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(i => i.IssuerDid == did);

        if (excludeId.HasValue)
        {
            query = query.Where(i => i.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsExternalIdUniqueAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return !await DbSet
            .AnyAsync(i => i.ExternalId == externalId, cancellationToken);
    }

    public async Task<bool> CanIssueCredentialTypeAsync(Guid issuerId, string credentialType, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(i => i.Id == issuerId &&
                          i.Status == IssuerStatus.Active &&
                          i.SupportedCredentialTypes.Any(ct => ct.TypeName == credentialType),
                     cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> GetBatchAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => ids.Contains(i.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Issuer>> GetByDidsAsync(IEnumerable<string> dids, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => dids.Contains(i.IssuerDid))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsTrustedIssuerAsync(string did, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(i => i.IssuerDid == did && i.IsTrusted, cancellationToken);
    }

    public async Task UpdateTrustStatusAsync(Guid issuerId, bool isTrusted, CancellationToken cancellationToken = default)
    {
        var issuer = await DbSet.FirstOrDefaultAsync(i => i.Id == issuerId, cancellationToken);

        if (issuer != null)
        {
            if (isTrusted)
            {
                issuer.MarkAsTrusted(10); // Default trust level
            }
            else
            {
                issuer.RemoveTrust();
            }

            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> DidExistsAsync(string did, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(i => i.IssuerDid == did, cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(i => i.Code == code, cancellationToken);
    }

    public async Task<Dictionary<IssuerStatus, int>> GetStatisticsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var statistics = await DbSet
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