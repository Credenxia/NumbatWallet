using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Domain.Specifications;

public class ActiveIssuerSpecification : Specification<Issuer>
{
    public ActiveIssuerSpecification()
    {
        AddCriteria(i => i.Status == IssuerStatus.Active);
    }
}

public class TrustedIssuerSpecification : Specification<Issuer>
{
    public TrustedIssuerSpecification()
    {
        AddCriteria(i => i.IsTrusted && i.Status == IssuerStatus.Active);
        ApplyOrderByDescending(i => i.TrustLevel);
    }
}

public class IssuerByDidSpecification : Specification<Issuer>
{
    public IssuerByDidSpecification(string did)
    {
        AddCriteria(i => i.IssuerDid == did);
    }
}

public class IssuerByCredentialTypeSpecification : Specification<Issuer>
{
    public IssuerByCredentialTypeSpecification(string credentialType)
    {
        // This would query the SupportedCredentialTypes collection
        AddCriteria(i => i.SupportedCredentialTypes.Any(ct => ct.TypeName == credentialType));
        AddCriteria(i => i.Status == IssuerStatus.Active && i.IsTrusted);
    }
}

public class IssuerWithExpiringCertificateSpecification : Specification<Issuer>
{
    public IssuerWithExpiringCertificateSpecification(int daysBeforeExpiry)
    {
        var expiryThreshold = DateTimeOffset.UtcNow.AddDays(daysBeforeExpiry);
        AddCriteria(i => i.CertificateExpiresAt.HasValue &&
                         i.CertificateExpiresAt.Value <= expiryThreshold &&
                         i.CertificateExpiresAt.Value > DateTimeOffset.UtcNow);

        ApplyOrderBy(i => i.CertificateExpiresAt!.Value);
    }
}

public class IssuerByJurisdictionSpecification : Specification<Issuer>
{
    public IssuerByJurisdictionSpecification(string jurisdiction)
    {
        AddCriteria(i => i.Jurisdiction == jurisdiction);
        AddCriteria(i => i.Status == IssuerStatus.Active);
    }
}

public class IssuerWithRevocationRegistrySpecification : Specification<Issuer>
{
    public IssuerWithRevocationRegistrySpecification(string credentialType)
    {
        AddCriteria(i => i.RevocationRegistries.Any(r =>
            r.CredentialType == credentialType &&
            r.IsActive));
    }
}

public class IssuerSearchSpecification : Specification<Issuer>
{
    public IssuerSearchSpecification(string searchTerm)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLowerInvariant();
            AddCriteria(i =>
                i.Name.ToLower().Contains(lowerSearchTerm) ||
                i.IssuerDid.ToLower().Contains(lowerSearchTerm) ||
                (i.Description != null && i.Description.ToLower().Contains(lowerSearchTerm)));
        }

        ApplyOrderBy(i => i.Name);
    }
}

public class IssuerByTrustLevelSpecification : Specification<Issuer>
{
    public IssuerByTrustLevelSpecification(int minimumTrustLevel)
    {
        AddCriteria(i => i.IsTrusted && i.TrustLevel >= minimumTrustLevel);
        ApplyOrderByDescending(i => i.TrustLevel);
    }
}

public class IssuerRequiringReviewSpecification : Specification<Issuer>
{
    public IssuerRequiringReviewSpecification()
    {
        AddCriteria(i =>
            i.Status == IssuerStatus.PendingApproval ||
            (i.CertificateExpiresAt.HasValue &&
             i.CertificateExpiresAt.Value <= DateTimeOffset.UtcNow.AddDays(30)) ||
            !i.IsTrusted);

        ApplyOrderBy(i => i.CreatedAt);
    }
}