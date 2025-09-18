using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.ValueObjects;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Models;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Infrastructure.Repositories;

public class PersonRepository : RepositoryBase<Person, Guid>, IPersonRepository
{
    public PersonRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<Person?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.Email.Value == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<Person?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.PhoneNumber.Value == phoneNumber, cancellationToken);
    }

    public async Task<Person?> GetByMobileNumberAsync(string mobileNumber, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.MobileNumber == mobileNumber, cancellationToken);
    }

    public async Task<Person?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.ExternalId == externalId, cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetByStatusAsync(PersonStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<Person?> GetWithWalletsAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Wallets)
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
    }

    public async Task<Person?> GetWithActiveWalletAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Wallets.Where(w => w.Status == WalletStatus.Active))
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetByVerificationStatusAsync(bool isVerified, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.IsVerified == isVerified)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetRecentlyCreatedAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.CreatedAt >= since)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLowerInvariant();

        return await DbSet
            .Where(p => p.FirstName.ToLowerInvariant().Contains(lowerSearchTerm) ||
                       p.LastName.ToLowerInvariant().Contains(lowerSearchTerm) ||
                       p.Email.Value.Contains(lowerSearchTerm) ||
                       (p.PhoneNumber.Value != null && p.PhoneNumber.Value.Contains(lowerSearchTerm)))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResponse<Person>> GetPagedAsync(
        PagedRequest request,
        ISpecification<Person>? specification = null,
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

        return new PagedResponse<Person>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<int> CountByStatusAsync(PersonStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(p => p.Status == status, cancellationToken);
    }

    public async Task<int> CountVerifiedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(p => p.IsVerified, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetPersonStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var statistics = new Dictionary<string, int>();

        // Count by status
        var statusCounts = await DbSet
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (var statusCount in statusCounts)
        {
            statistics[$"Status_{statusCount.Status}"] = statusCount.Count;
        }

        // Count verified
        statistics["Verified"] = await CountVerifiedAsync(cancellationToken);
        statistics["Unverified"] = await DbSet.CountAsync(p => !p.IsVerified, cancellationToken);

        // Total count
        statistics["Total"] = await DbSet.CountAsync(cancellationToken);

        return statistics;
    }

    public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var query = DbSet.Where(p => p.Email.Value == normalizedEmail);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsMobileNumberUniqueAsync(string mobileNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(p => p.MobileNumber == mobileNumber);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsExternalIdUniqueAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return !await DbSet
            .AnyAsync(p => p.ExternalId == externalId, cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetBatchAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetByExternalIdsAsync(IEnumerable<string> externalIds, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => externalIds.Contains(p.ExternalId))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await DbSet
            .AnyAsync(p => p.Email.Value == normalizedEmail, cancellationToken);
    }

    public async Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(p => p.PhoneNumber.Value == phoneNumber, cancellationToken);
    }

    public async Task<bool> ExternalIdExistsAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(p => p.ExternalId == externalId, cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetVerifiedPeopleAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.IsVerified)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetVerifiedPersonsAsync(CancellationToken cancellationToken = default)
    {
        return await GetVerifiedPeopleAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetUnverifiedPeopleAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => !p.IsVerified)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<PersonStatus, int>> GetStatisticsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var statistics = await DbSet
            .Where(p => p.TenantId == tenantId)
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        // Ensure all statuses are represented
        foreach (var status in Enum.GetValues<PersonStatus>())
        {
            if (!statistics.ContainsKey(status))
            {
                statistics[status] = 0;
            }
        }

        return statistics;
    }
}