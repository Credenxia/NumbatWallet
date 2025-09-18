using System.Linq.Expressions;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Domain.Specifications;

public class ActiveWalletSpecification : Specification<Wallet>
{
    public ActiveWalletSpecification()
    {
        AddCriteria(w => w.Status == WalletStatus.Active);
    }
}

public class WalletByPersonSpecification : Specification<Wallet>
{
    public WalletByPersonSpecification(Guid personId)
    {
        AddCriteria(w => w.PersonId == personId);
        AddInclude(w => w.Credentials);
    }
}

public class WalletByTenantSpecification : Specification<Wallet>
{
    public WalletByTenantSpecification(string tenantId, bool includeInactive = false)
    {
        AddCriteria(w => w.TenantId == tenantId);

        if (!includeInactive)
        {
            AddCriteria(w => w.Status != WalletStatus.Inactive);
        }

        ApplyOrderByDescending(w => w.CreatedAt);
    }
}

public class ExpiredWalletSpecification : Specification<Wallet>
{
    public ExpiredWalletSpecification()
    {
        AddCriteria(w => w.Status == WalletStatus.Expired ||
                         (w.ExpiresAt.HasValue && w.ExpiresAt.Value <= DateTimeOffset.UtcNow));
    }
}

public class WalletWithCredentialsSpecification : Specification<Wallet>
{
    public WalletWithCredentialsSpecification(Guid walletId)
    {
        AddCriteria(w => w.Id == walletId);
        AddInclude(w => w.Credentials);
        AddInclude("Credentials.Issuer");
    }
}

public class WalletSearchSpecification : Specification<Wallet>
{
    public WalletSearchSpecification(string searchTerm)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLowerInvariant();
            AddCriteria(w =>
                w.WalletDid.ToLowerInvariant().Contains(lowerSearchTerm) ||
                w.ExternalId != null && w.ExternalId.ToLowerInvariant().Contains(lowerSearchTerm));
        }
    }
}

public class WalletByStatusSpecification : Specification<Wallet>
{
    public WalletByStatusSpecification(params WalletStatus[] statuses)
    {
        if (statuses.Any())
        {
            AddCriteria(w => statuses.Contains(w.Status));
        }
    }
}

public class WalletCreatedBetweenSpecification : Specification<Wallet>
{
    public WalletCreatedBetweenSpecification(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        AddCriteria(w => w.CreatedAt >= startDate && w.CreatedAt <= endDate);
        ApplyOrderByDescending(w => w.CreatedAt);
    }
}