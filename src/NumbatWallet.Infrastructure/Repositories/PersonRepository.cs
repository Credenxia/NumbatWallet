using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Infrastructure.Repositories;

public class PersonRepository : RepositoryBase<Person, Guid>, IPersonRepository
{
    public PersonRepository(NumbatWalletDbContext context) : base(context)
    {
    }

    public async Task<Person?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Email == email, cancellationToken);
    }

    public async Task<Person?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.ExternalId == externalId, cancellationToken);
    }

    public async Task<Person?> GetByMobileNumberAsync(string mobileNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.MobileNumber == mobileNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetVerifiedPersonsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.EmailVerificationStatus == VerificationStatus.Verified)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetPaginatedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(lowerSearchTerm) ||
                p.LastName.ToLower().Contains(lowerSearchTerm) ||
                p.Email.ToLower().Contains(lowerSearchTerm));
        }

        return await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(p => p.Email == email, cancellationToken);
    }

    public async Task<bool> ExternalIdExistsAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(p => p.ExternalId == externalId, cancellationToken);
    }

    public async Task<Dictionary<PersonStatus, int>> GetStatisticsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var statistics = new Dictionary<PersonStatus, int>();

        // Count by status - note that Person doesn't have a Status property in current implementation
        // This would need to be adjusted based on actual Person entity structure

        var verifiedCount = await _dbSet
            .Where(p => p.TenantId == tenantId && p.EmailVerificationStatus == VerificationStatus.Verified)
            .CountAsync(cancellationToken);

        var pendingCount = await _dbSet
            .Where(p => p.TenantId == tenantId && p.EmailVerificationStatus == VerificationStatus.Pending)
            .CountAsync(cancellationToken);

        statistics[PersonStatus.Verified] = verifiedCount;
        statistics[PersonStatus.PendingVerification] = pendingCount;

        return statistics;
    }
}