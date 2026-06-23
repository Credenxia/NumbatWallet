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

    // Field encryption makes FirstName/LastName/Email/Phone non-queryable (the columns hold
    // AES-GCM ciphertext, so EF cannot translate Contains/equality over them — doing so threw a
    // query-translation error → HTTP 500). Search therefore runs ONLY over fields that are
    // actually queryable:
    //   * email term  → exact match on the deterministic EmailSearchToken (HMAC) shadow column
    //   * phone term  → exact match on the PhoneSearchToken (HMAC) shadow column
    //   * ExternalId  → exact match on the plaintext ExternalId column
    // A name-only term matches none of these and returns an empty result rather than erroring.
    // Substring/name search is unsupported under field encryption (use email/phone exact lookups).
    public async Task<IReadOnlyList<Person>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Array.Empty<Person>();
        }

        var trimmed = searchTerm.Trim();

        // Email term: resolve via the deterministic email search token.
        if (trimmed.Contains('@', StringComparison.Ordinal))
        {
            var emailToken = await _searchTokenService.GenerateEmailTokenAsync(trimmed);
            if (emailToken is null)
            {
                return Array.Empty<Person>();
            }

            return await DbSet
                .Where(p => EF.Property<string?>(p, SearchTokenInterceptor.EmailSearchTokenProperty) == emailToken)
                .ToListAsync(cancellationToken);
        }

        // Phone term: digits-only (optionally a leading '+', spaces, dashes, parens).
        if (LooksLikePhoneNumber(trimmed))
        {
            var phoneToken = await _searchTokenService.GeneratePhoneTokenAsync(trimmed);
            if (phoneToken is not null)
            {
                var byPhone = await DbSet
                    .Where(p => EF.Property<string?>(p, SearchTokenInterceptor.PhoneSearchTokenProperty) == phoneToken)
                    .ToListAsync(cancellationToken);

                if (byPhone.Count > 0)
                {
                    return byPhone;
                }
            }
        }

        // Fall back to the queryable plaintext ExternalId (exact match).
        return await DbSet
            .Where(p => p.ExternalId == trimmed)
            .ToListAsync(cancellationToken);
    }

    private static bool LooksLikePhoneNumber(string value)
    {
        var hasDigit = false;
        foreach (var c in value)
        {
            if (char.IsDigit(c))
            {
                hasDigit = true;
            }
            else if (c is not ('+' or ' ' or '-' or '(' or ')'))
            {
                return false;
            }
        }

        return hasDigit;
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
