using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Guards;

namespace NumbatWallet.Domain.Entities;

/// <summary>
/// Represents a trust store for managing certificate trust relationships
/// </summary>
public class CertificateTrustStore : Entity<Guid>
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private readonly List<Guid> _trustedCertificateIds = new();
    private readonly List<Guid> _trustedAuthorityIds = new();
    private readonly List<string> _revokedThumbprints = new();

    public IReadOnlyList<Guid> TrustedCertificateIds => _trustedCertificateIds.AsReadOnly();
    public IReadOnlyList<Guid> TrustedAuthorityIds => _trustedAuthorityIds.AsReadOnly();
    public IReadOnlyList<string> RevokedThumbprints => _revokedThumbprints.AsReadOnly();

    private CertificateTrustStore() : base(Guid.Empty)
    {
        // EF Core constructor
        Name = string.Empty;
        Description = string.Empty;
    }

    public CertificateTrustStore(Guid tenantId, string name, string description)
        : base(Guid.NewGuid())
    {
        Guard.AgainstEmptyGuid(tenantId, nameof(tenantId));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(description, nameof(description));

        TenantId = tenantId;
        Name = name;
        Description = description;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void AddTrustedCertificate(Guid certificateId)
    {
        Guard.AgainstEmptyGuid(certificateId, nameof(certificateId));

        if (_trustedCertificateIds.Contains(certificateId))
        {
            return; // Already trusted
        }

        _trustedCertificateIds.Add(certificateId);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveTrustedCertificate(Guid certificateId)
    {
        if (_trustedCertificateIds.Remove(certificateId))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void AddTrustedAuthority(Guid authorityId)
    {
        Guard.AgainstEmptyGuid(authorityId, nameof(authorityId));

        if (_trustedAuthorityIds.Contains(authorityId))
        {
            return; // Already trusted
        }

        _trustedAuthorityIds.Add(authorityId);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveTrustedAuthority(Guid authorityId)
    {
        if (_trustedAuthorityIds.Remove(authorityId))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void RevokeCertificate(string thumbprint)
    {
        Guard.AgainstNullOrWhiteSpace(thumbprint, nameof(thumbprint));

        var normalizedThumbprint = thumbprint.ToUpperInvariant();
        if (_revokedThumbprints.Contains(normalizedThumbprint))
        {
            return; // Already revoked
        }

        _revokedThumbprints.Add(normalizedThumbprint);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UnrevokeCertificate(string thumbprint)
    {
        Guard.AgainstNullOrWhiteSpace(thumbprint, nameof(thumbprint));

        var normalizedThumbprint = thumbprint.ToUpperInvariant();
        if (_revokedThumbprints.Remove(normalizedThumbprint))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public bool IsCertificateRevoked(string thumbprint)
    {
        return _revokedThumbprints.Contains(thumbprint.ToUpperInvariant());
    }

    public bool IsCertificateTrusted(Guid certificateId)
    {
        return _trustedCertificateIds.Contains(certificateId);
    }

    public bool IsAuthorityTrusted(Guid authorityId)
    {
        return _trustedAuthorityIds.Contains(authorityId);
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
