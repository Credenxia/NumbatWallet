namespace NumbatWallet.Application.DTOs;

public class CertificateDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Thumbprint { get; set; } = string.Empty;
    public string SubjectDn { get; set; } = string.Empty;
    public string IssuerDn { get; set; } = string.Empty;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidTo { get; set; }
    public bool IsActive { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string TrustLevel { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public int UsageCount { get; set; }
    public string? SerialNumber { get; set; }
    public bool IsExpired { get; set; }
    public bool IsRevoked { get; set; }
    public int DaysUntilExpiry { get; set; }
}

public class CertificateUploadDto
{
    public required string CertificateData { get; set; } // Base64 or PEM
    public required string Purpose { get; set; }
    public string? TrustLevel { get; set; }
}

public class CertificateStatisticsDto
{
    public int TotalCertificates { get; set; }
    public int ActiveCertificates { get; set; }
    public int ExpiredCertificates { get; set; }
    public int RevokedCertificates { get; set; }
    public int ExpiringIn30Days { get; set; }
    public int ExpiringIn90Days { get; set; }
}
