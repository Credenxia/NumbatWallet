using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data.Interceptors;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Models;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Infrastructure.Data.Repositories;

public class PersonRepository : RepositoryBase<Person, Guid>, IPersonRepository
{
    private readonly IHmacSearchTokenService _searchTokenService;

    public PersonRepository(NumbatWalletDbContext context, IHmacSearchTokenService searchTokenService)
        : base(context)
    {
        _searchTokenService = searchTokenService;
    }

    public async Task<Person?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(p => p.ExternalId == externalId, cancellationToken);
    }

    // Email/Phone are encrypted at rest (non-deterministic AES-GCM ciphertext), so equality on
    // the column can never match. Exact-match lookups compute the deterministic HMAC search
    // token of the input and query the token shadow columns populated by SearchTokenInterceptor.
    public async Task<Person?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var token = await _searchTokenService.GenerateEmailTokenAsync(email);
        if (token is null)
        {
            return null;
        }

        return await DbSet.FirstOrDefaultAsync(
            p => EF.Property<string?>(p, SearchTokenInterceptor.EmailSearchTokenProperty) == token,
            cancellationToken);
    }

    public async Task<Person?> GetByMobileNumberAsync(string mobileNumber, CancellationToken cancellationToken = default)
    {
        var token = await _searchTokenService.GeneratePhoneTokenAsync(mobileNumber);
        if (token is null)
        {
            return null;
        }

        return await DbSet.FirstOrDefaultAsync(
            p => EF.Property<string?>(p, SearchTokenInterceptor.PhoneSearchTokenProperty) == token,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetByStatusAsync(PersonStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(p => p.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetVerifiedPersonsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(p => p.EmailVerificationStatus == VerificationStatus.Verified &&
                                       p.PhoneVerificationStatus == VerificationStatus.Verified).ToListAsync(cancellationToken);
    }

    public async Task<Person?> GetWithWalletsAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include("Wallets")
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
    }

    public async Task<Person?> GetWithActiveWalletAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include("Wallets")
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetByVerificationStatusAsync(bool isVerified, CancellationToken cancellationToken = default)
    {
        if (isVerified)
        {
            return await DbSet.Where(p => p.EmailVerificationStatus == VerificationStatus.Verified &&
                                           p.PhoneVerificationStatus == VerificationStatus.Verified).ToListAsync(cancellationToken);
        }
        else
        {
            return await DbSet.Where(p => p.EmailVerificationStatus != VerificationStatus.Verified ||
                                           p.PhoneVerificationStatus != VerificationStatus.Verified).ToListAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Person>> GetRecentlyCreatedAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(p => p.CreatedAt >= since).ToListAsync(cancellationToken);
    }

    // NOTE: substring search over FirstName/LastName/Email cannot match once field encryption is
    // enabled (the columns hold ciphertext JSON) — a pre-existing limitation of this fuzzy search,
    // now also true for email. Exact lookups must use GetByEmailAsync/GetByMobileNumberAsync.
    public async Task<IReadOnlyList<Person>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLowerInvariant();
        return await DbSet
            .Where(p => p.FirstName.ToLowerInvariant().Contains(lowerSearchTerm) ||
                       p.LastName.ToLowerInvariant().Contains(lowerSearchTerm) ||
                       p.Email.Value.ToLowerInvariant().Contains(lowerSearchTerm))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResponse<Person>> GetPagedAsync(
        PagedRequest request,
        ISpecification<Person>? specification = null,
        CancellationToken cancellationToken = default)
    {
        var query = specification != null ? ApplySpecification(specification) : DbSet.AsQueryable();

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<Person>(items, totalItems, request.PageNumber, request.PageSize);
    }

    public async Task<int> CountByStatusAsync(PersonStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(p => p.Status == status, cancellationToken);
    }

    public async Task<int> CountVerifiedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(p => p.EmailVerificationStatus == VerificationStatus.Verified &&
                                           p.PhoneVerificationStatus == VerificationStatus.Verified, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetPersonStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new Dictionary<string, int>
        {
            ["Total"] = await DbSet.CountAsync(cancellationToken),
            ["Verified"] = await CountVerifiedAsync(cancellationToken),
            ["Active"] = await CountByStatusAsync(PersonStatus.Active, cancellationToken),
            ["Suspended"] = await CountByStatusAsync(PersonStatus.Suspended, cancellationToken)
        };
        return stats;
    }

    public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var token = await _searchTokenService.GenerateEmailTokenAsync(email);
        if (token is null)
        {
            return true;
        }

        var query = DbSet.Where(
            p => EF.Property<string?>(p, SearchTokenInterceptor.EmailSearchTokenProperty) == token);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsMobileNumberUniqueAsync(string mobileNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var token = await _searchTokenService.GeneratePhoneTokenAsync(mobileNumber);
        if (token is null)
        {
            return true;
        }

        var query = DbSet.Where(
            p => EF.Property<string?>(p, SearchTokenInterceptor.PhoneSearchTokenProperty) == token);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsExternalIdUniqueAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return !await DbSet.AnyAsync(p => p.ExternalId == externalId, cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetBatchAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetByExternalIdsAsync(IEnumerable<string> externalIds, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(p => externalIds.Contains(p.ExternalId)).ToListAsync(cancellationToken);
    }
}
