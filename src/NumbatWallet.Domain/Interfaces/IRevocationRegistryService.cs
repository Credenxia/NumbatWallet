using System.Security.Cryptography.X509Certificates;

namespace NumbatWallet.Domain.Interfaces;

/// <summary>
/// Service for managing certificate revocation lists and OCSP responses
/// </summary>
public interface IRevocationRegistryService
{
    /// <summary>
    /// Add a certificate to the revocation registry
    /// </summary>
    Task<RevocationEntry> RevokeCertificateAsync(
        string serialNumber,
        RevocationReason reason,
        string comment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a certificate is revoked via CRL
    /// </summary>
    Task<RevocationStatus> CheckRevocationStatusAsync(
        string serialNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check certificate status via OCSP
    /// </summary>
    Task<OcspResponse> CheckOcspStatusAsync(
        X509Certificate2 certificate,
        X509Certificate2 issuerCertificate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a Certificate Revocation List (CRL)
    /// </summary>
    Task<byte[]> GenerateCrlAsync(
        X509Certificate2 caCertificate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish CRL to distribution points
    /// </summary>
    Task<bool> PublishCrlAsync(
        byte[] crlData,
        IEnumerable<string> distributionPoints,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download and validate CRL from distribution point
    /// </summary>
    Task<CrlInfo> DownloadCrlAsync(
        string distributionPointUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate OCSP response for a certificate
    /// </summary>
    Task<byte[]> GenerateOcspResponseAsync(
        X509Certificate2 certificate,
        OcspResponseStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all revoked certificates
    /// </summary>
    Task<IEnumerable<RevocationEntry>> GetRevokedCertificatesAsync(
        DateTime? since = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove expired entries from revocation registry
    /// </summary>
    Task<int> PruneExpiredEntriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get CRL distribution points from certificate
    /// </summary>
    IEnumerable<string> GetCrlDistributionPoints(X509Certificate2 certificate);

    /// <summary>
    /// Get OCSP responder URL from certificate
    /// </summary>
    string? GetOcspResponderUrl(X509Certificate2 certificate);
}

/// <summary>
/// Certificate revocation reasons
/// </summary>
public enum RevocationReason
{
    Unspecified = 0,
    KeyCompromise = 1,
    CaCompromise = 2,
    AffiliationChanged = 3,
    Superseded = 4,
    CessationOfOperation = 5,
    CertificateHold = 6,
    RemoveFromCrl = 8,
    PrivilegeWithdrawn = 9,
    AaCompromise = 10
}

/// <summary>
/// Revocation status information
/// </summary>
public class RevocationStatus
{
    public bool IsRevoked { get; set; }
    public DateTime? RevocationDate { get; set; }
    public RevocationReason? Reason { get; set; }
    public string? Comment { get; set; }
    public DateTime CheckedAt { get; set; }
    public RevocationCheckSource Source { get; set; }
}

/// <summary>
/// Source of revocation check
/// </summary>
public enum RevocationCheckSource
{
    LocalRegistry,
    CRL,
    OCSP,
    Cache
}

/// <summary>
/// OCSP response status
/// </summary>
public enum OcspResponseStatus
{
    Good,
    Revoked,
    Unknown,
    Unauthorized,
    MalformedRequest,
    InternalError,
    TryLater
}

/// <summary>
/// OCSP response information
/// </summary>
public class OcspResponse
{
    public string CertificateSerialNumber { get; set; } = string.Empty;
    public OcspResponseStatus Status { get; set; }
    public DateTime ProducedAt { get; set; }
    public DateTime? ThisUpdate { get; set; }
    public DateTime? NextUpdate { get; set; }
    public RevocationReason? RevocationReason { get; set; }
    public DateTime? RevocationTime { get; set; }
    public byte[] ResponseData { get; set; } = Array.Empty<byte>();
    public string ResponderUrl { get; set; } = string.Empty;
}

/// <summary>
/// Revocation entry in the registry
/// </summary>
public class RevocationEntry
{
    public Guid Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string? Thumbprint { get; set; }
    public DateTime RevocationDate { get; set; }
    public RevocationReason Reason { get; set; }
    public string? Comment { get; set; }
    public string? RevokedBy { get; set; }
    public DateTime? InvalidityDate { get; set; }
    public bool IsHold { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// CRL information
/// </summary>
public class CrlInfo
{
    public byte[] RawData { get; set; } = Array.Empty<byte>();
    public DateTime EffectiveDate { get; set; }
    public DateTime NextUpdate { get; set; }
    public string IssuerName { get; set; } = string.Empty;
    public int Version { get; set; }
    public List<CrlEntry> RevokedCertificates { get; set; } = new();
    public bool IsValid { get; set; }
    public string? SignatureAlgorithm { get; set; }
}

/// <summary>
/// CRL entry for a single certificate
/// </summary>
public class CrlEntry
{
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime RevocationDate { get; set; }
    public RevocationReason Reason { get; set; }
}
