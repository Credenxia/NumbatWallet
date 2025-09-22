using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Domain.Services;

/// <summary>
/// Domain service for certificate validation logic
/// </summary>
public interface ICertificateValidationService
{
    /// <summary>
    /// Validates a certificate against trust store and CA chain
    /// </summary>
    Task<CertificateValidationResult> ValidateCertificateAsync(
        TenantCertificate certificate,
        CertificateTrustStore trustStore,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates certificate chain up to a trusted CA
    /// </summary>
    Task<bool> ValidateChainAsync(
        TenantCertificate certificate,
        IEnumerable<CertificateAuthority> authorities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if certificate is revoked via OCSP
    /// </summary>
    Task<bool> CheckOcspStatusAsync(
        TenantCertificate certificate,
        string? ocspUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if certificate is in CRL
    /// </summary>
    Task<bool> CheckCrlStatusAsync(
        TenantCertificate certificate,
        string? crlUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates certificate for specific purpose
    /// </summary>
    bool ValidateForPurpose(TenantCertificate certificate, CertificatePurpose purpose);
}

public class CertificateValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public CertificateTrustLevel TrustLevel { get; set; }
    public bool ChainValid { get; set; }
    public bool OcspValid { get; set; }
    public bool CrlValid { get; set; }
    public DateTimeOffset ValidatedAt { get; set; } = DateTimeOffset.UtcNow;
}