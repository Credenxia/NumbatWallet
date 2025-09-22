using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.Domain.Entities;

/// <summary>
/// Certificate revocation entry in the registry
/// </summary>
public class CertificateRevocation : Entity<Guid>
{
    public string SerialNumber { get; private set; } = string.Empty;
    public string? Thumbprint { get; private set; }
    public DateTime RevocationDate { get; private set; }
    public int Reason { get; private set; }
    public string? Comment { get; private set; }
    public string? RevokedBy { get; private set; }
    public DateTime? InvalidityDate { get; private set; }
    public bool IsHold { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // For EF Core
    private CertificateRevocation() : base(Guid.Empty) { }

    public CertificateRevocation(
        string serialNumber,
        int reason,
        string? comment = null,
        string? revokedBy = null) : base(Guid.NewGuid())
    {
        SerialNumber = serialNumber ?? throw new ArgumentNullException(nameof(serialNumber));
        Reason = reason;
        Comment = comment;
        RevokedBy = revokedBy;
        RevocationDate = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        IsHold = reason == 6; // CertificateHold
    }

    public void SetThumbprint(string thumbprint)
    {
        Thumbprint = thumbprint;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetInvalidityDate(DateTime invalidityDate)
    {
        InvalidityDate = invalidityDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseHold()
    {
        if (!IsHold)
        {
            throw new InvalidOperationException("Certificate is not on hold");
        }

        IsHold = false;
        Reason = 8; // RemoveFromCrl
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateComment(string comment)
    {
        Comment = comment;
        UpdatedAt = DateTime.UtcNow;
    }
}