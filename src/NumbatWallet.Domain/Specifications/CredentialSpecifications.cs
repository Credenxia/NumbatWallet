using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Domain.Specifications;

public class ActiveCredentialSpecification : Specification<Credential>
{
    public ActiveCredentialSpecification()
    {
        AddCriteria(c => c.Status == CredentialStatus.Active);
    }
}

public class CredentialByWalletSpecification : Specification<Credential>
{
    public CredentialByWalletSpecification(Guid walletId, bool includeRevoked = false)
    {
        AddCriteria(c => c.WalletId == walletId);

        if (!includeRevoked)
        {
            AddCriteria(c => c.Status != CredentialStatus.Revoked);
        }

        AddInclude(c => c.Issuer);
        ApplyOrderByDescending(c => c.IssuedAt);
    }
}

public class CredentialByIssuerSpecification : Specification<Credential>
{
    public CredentialByIssuerSpecification(Guid issuerId)
    {
        AddCriteria(c => c.IssuerId == issuerId);
        AddInclude(c => c.Wallet);
    }
}

public class CredentialByTypeSpecification : Specification<Credential>
{
    public CredentialByTypeSpecification(string credentialType, bool activeOnly = true)
    {
        AddCriteria(c => c.CredentialType == credentialType);

        if (activeOnly)
        {
            AddCriteria(c => c.Status == CredentialStatus.Active);
        }
    }
}

public class ExpiredCredentialSpecification : Specification<Credential>
{
    public ExpiredCredentialSpecification()
    {
        var now = DateTimeOffset.UtcNow;
        AddCriteria(c => c.Status == CredentialStatus.Expired ||
                         (c.ExpiresAt.HasValue && c.ExpiresAt.Value <= now));
    }
}

public class ExpiringCredentialSpecification : Specification<Credential>
{
    public ExpiringCredentialSpecification(int daysBeforeExpiry)
    {
        var expiryThreshold = DateTimeOffset.UtcNow.AddDays(daysBeforeExpiry);
        AddCriteria(c => c.Status == CredentialStatus.Active &&
                         c.ExpiresAt.HasValue &&
                         c.ExpiresAt.Value <= expiryThreshold &&
                         c.ExpiresAt.Value > DateTimeOffset.UtcNow);

        ApplyOrderBy(c => c.ExpiresAt!.Value);
    }
}

public class RevokedCredentialSpecification : Specification<Credential>
{
    public RevokedCredentialSpecification()
    {
        AddCriteria(c => c.Status == CredentialStatus.Revoked);
        AddInclude(c => c.Issuer);
    }
}

public class CredentialWithClaimSpecification : Specification<Credential>
{
    public CredentialWithClaimSpecification(string claimKey, object claimValue)
    {
        // This would need custom SQL/LINQ for JSON queries
        // Simplified version - actual implementation would query JSON
        AddCriteria(c => c.Claims.ContainsKey(claimKey));
    }
}

public class VerifiableCredentialSpecification : Specification<Credential>
{
    public VerifiableCredentialSpecification()
    {
        AddCriteria(c => c.Status == CredentialStatus.Active &&
                         (c.ExpiresAt == null || c.ExpiresAt.Value > DateTimeOffset.UtcNow));
        AddInclude(c => c.Issuer);
    }
}