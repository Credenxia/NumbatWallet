using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Domain.Specifications;

public class ActivePersonSpecification : Specification<Person>
{
    public ActivePersonSpecification()
    {
        AddCriteria(p => p.Status == PersonStatus.Active || p.Status == PersonStatus.Verified);
    }
}

public class VerifiedPersonSpecification : Specification<Person>
{
    public VerifiedPersonSpecification()
    {
        AddCriteria(p => p.IsVerified);
    }
}

public class UnverifiedPersonSpecification : Specification<Person>
{
    public UnverifiedPersonSpecification()
    {
        AddCriteria(p => !p.IsVerified && p.Status == PersonStatus.PendingVerification);
    }
}

public class PersonByEmailSpecification : Specification<Person>
{
    public PersonByEmailSpecification(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        AddCriteria(p => p.Email.ToLower() == normalizedEmail);
    }
}

public class PersonByPhoneSpecification : Specification<Person>
{
    public PersonByPhoneSpecification(string phoneNumber)
    {
        AddCriteria(p => p.MobileNumber == phoneNumber);
    }
}

public class PersonByExternalIdSpecification : Specification<Person>
{
    public PersonByExternalIdSpecification(string externalId)
    {
        AddCriteria(p => p.ExternalId == externalId);
    }
}

public class PersonWithWalletSpecification : Specification<Person>
{
    public PersonWithWalletSpecification(Guid personId)
    {
        AddCriteria(p => p.Id == personId);
        AddInclude(p => p.Wallets);
        AddInclude("Wallets.Credentials");
    }
}

public class PersonSearchSpecification : Specification<Person>
{
    public PersonSearchSpecification(string searchTerm)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLowerInvariant();
            AddCriteria(p =>
                p.FirstName.ToLower().Contains(lowerSearchTerm) ||
                p.LastName.ToLower().Contains(lowerSearchTerm) ||
                p.Email.ToLower().Contains(lowerSearchTerm) ||
                (p.ExternalId != null && p.ExternalId.ToLower().Contains(lowerSearchTerm)));
        }

        ApplyOrderBy(p => p.LastName);
        ApplyOrderBy(p => p.FirstName);
    }
}

public class PersonCreatedBetweenSpecification : Specification<Person>
{
    public PersonCreatedBetweenSpecification(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        AddCriteria(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate);
        ApplyOrderByDescending(p => p.CreatedAt);
    }
}

public class EligibleForWalletSpecification : Specification<Person>
{
    public EligibleForWalletSpecification()
    {
        AddCriteria(p => p.IsVerified &&
                         (p.Status == PersonStatus.Active || p.Status == PersonStatus.Verified));
        AddInclude(p => p.Wallets);
    }
}