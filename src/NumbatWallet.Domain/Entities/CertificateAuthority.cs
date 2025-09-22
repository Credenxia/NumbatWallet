using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Guards;

namespace NumbatWallet.Domain.Entities;

/// <summary>
/// Represents a trusted Certificate Authority (CA) for certificate chain validation
/// </summary>
public class CertificateAuthority : Entity<Guid>
{
    public string Name { get; private set; }
    public string CertificateData { get; private set; } // Base64 encoded CA certificate
    public string Thumbprint { get; private set; }
    public string SubjectDn { get; private set; }
    public bool IsTrusted { get; private set; }
    public CertificateTrustLevel TrustLevel { get; private set; }
    public string? CrlUrl { get; private set; } // Certificate Revocation List URL
    public string? OcspUrl { get; private set; } // OCSP responder URL
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset ValidTo { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastValidatedAt { get; private set; }

    private CertificateAuthority() : base(Guid.Empty)
    {
        // EF Core constructor
        Name = string.Empty;
        CertificateData = string.Empty;
        Thumbprint = string.Empty;
        SubjectDn = string.Empty;
    }

    public CertificateAuthority(
        string name,
        string certificateData,
        string thumbprint,
        string subjectDn,
        DateTimeOffset validFrom,
        DateTimeOffset validTo)
        : base(Guid.NewGuid())
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(certificateData, nameof(certificateData));
        Guard.AgainstNullOrWhiteSpace(thumbprint, nameof(thumbprint));
        Guard.AgainstNullOrWhiteSpace(subjectDn, nameof(subjectDn));

        if (validTo <= validFrom)
        {
            throw new ArgumentException("Certificate expiry must be after the start date.");
        }

        Name = name;
        CertificateData = certificateData;
        Thumbprint = thumbprint.ToUpperInvariant();
        SubjectDn = subjectDn;
        ValidFrom = validFrom;
        ValidTo = validTo;
        IsTrusted = false; // Requires explicit trust
        TrustLevel = CertificateTrustLevel.Low;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void SetTrusted(CertificateTrustLevel trustLevel)
    {
        if (IsExpired())
        {
            throw new InvalidOperationException("Cannot trust an expired CA certificate");
        }

        IsTrusted = true;
        TrustLevel = trustLevel;
    }

    public void Revoke()
    {
        IsTrusted = false;
        TrustLevel = CertificateTrustLevel.Low;
    }

    public void SetCrlUrl(string crlUrl)
    {
        Guard.AgainstNullOrWhiteSpace(crlUrl, nameof(crlUrl));
        CrlUrl = crlUrl;
    }

    public void SetOcspUrl(string ocspUrl)
    {
        Guard.AgainstNullOrWhiteSpace(ocspUrl, nameof(ocspUrl));
        OcspUrl = ocspUrl;
    }

    public void MarkAsValidated()
    {
        LastValidatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsExpired()
    {
        return DateTimeOffset.UtcNow > ValidTo;
    }

    public bool RequiresRevalidation(TimeSpan validationInterval)
    {
        if (!LastValidatedAt.HasValue)
        {
            return true;
        }

        return DateTimeOffset.UtcNow - LastValidatedAt.Value > validationInterval;
    }
}