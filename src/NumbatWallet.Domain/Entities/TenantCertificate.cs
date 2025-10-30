using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Guards;

namespace NumbatWallet.Domain.Entities;

/// <summary>
/// Represents an X.509 certificate associated with a tenant for mTLS authentication
/// </summary>
public class TenantCertificate : Entity<Guid>
{
    public Guid TenantId { get; private set; }
    public string CertificateData { get; private set; } // Base64 encoded certificate
    public string Thumbprint { get; private set; }
    public string SubjectDn { get; private set; }
    public string IssuerDn { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public CertificatePurpose Purpose { get; private set; }
    public CertificateTrustLevel TrustLevel { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevocationReason { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public int UsageCount { get; private set; }
    public string? SerialNumber { get; init; }
    public DateTimeOffset NotBefore { get; init; }
    public DateTimeOffset NotAfter { get; init; }
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsBlocked { get; init; }

    private TenantCertificate() : base(Guid.Empty)
    {
        // EF Core constructor
        CertificateData = string.Empty;
        Thumbprint = string.Empty;
        SubjectDn = string.Empty;
        IssuerDn = string.Empty;
    }

    public TenantCertificate(
        Guid tenantId,
        string certificateData,
        string thumbprint,
        string subjectDn,
        string issuerDn,
        DateTimeOffset validFrom,
        DateTimeOffset validTo,
        CertificatePurpose purpose)
        : base(Guid.NewGuid())
    {
        Guard.AgainstEmptyGuid(tenantId, nameof(tenantId));
        Guard.AgainstNullOrWhiteSpace(certificateData, nameof(certificateData));
        Guard.AgainstNullOrWhiteSpace(thumbprint, nameof(thumbprint));
        Guard.AgainstNullOrWhiteSpace(subjectDn, nameof(subjectDn));
        Guard.AgainstNullOrWhiteSpace(issuerDn, nameof(issuerDn));

        if (validTo <= validFrom)
        {
            throw new ArgumentException("Certificate expiry must be after the start date.");
        }

        TenantId = tenantId;
        CertificateData = certificateData;
        Thumbprint = thumbprint.ToUpperInvariant();
        SubjectDn = subjectDn;
        IssuerDn = issuerDn;
        ValidFrom = validFrom;
        ValidTo = validTo;
        Purpose = purpose;
        IsActive = true;
        TrustLevel = CertificateTrustLevel.Low;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (IsExpired())
        {
            throw new InvalidOperationException("Cannot activate an expired certificate");
        }

        if (RevokedAt.HasValue)
        {
            throw new InvalidOperationException("Cannot activate a revoked certificate");
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Revoke(string reason)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));

        if (RevokedAt.HasValue)
        {
            throw new InvalidOperationException("Certificate is already revoked");
        }

        RevokedAt = DateTimeOffset.UtcNow;
        RevocationReason = reason;
        IsActive = false;
    }

    public void UpdateTrustLevel(CertificateTrustLevel trustLevel)
    {
        if (RevokedAt.HasValue)
        {
            throw new InvalidOperationException("Cannot update trust level of a revoked certificate");
        }

        TrustLevel = trustLevel;
    }

    public bool IsExpired()
    {
        return DateTimeOffset.UtcNow > ValidTo;
    }

    public bool IsValidAt(DateTimeOffset dateTime)
    {
        return dateTime >= ValidFrom && dateTime <= ValidTo && !RevokedAt.HasValue;
    }

    public bool CanBeUsedForPurpose(CertificatePurpose purpose)
    {
        return Purpose == purpose || Purpose == CertificatePurpose.All;
    }

    public void UpdateLastUsed()
    {
        LastUsedAt = DateTimeOffset.UtcNow;
        UsageCount++;
    }
}

public enum CertificatePurpose
{
    Authentication,
    Signing,
    Encryption,
    All
}

public enum CertificateTrustLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Full = 3
}
