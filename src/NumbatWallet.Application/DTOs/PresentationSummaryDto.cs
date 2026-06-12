namespace NumbatWallet.Application.DTOs;

/// <summary>
/// Read-model summary of a presentation. Exposes the NAMES of disclosed claims only —
/// never the claim values — so wallet history views don't leak presented PII.
/// </summary>
public class PresentationSummaryDto
{
    public Guid Id { get; set; }
    public Guid CredentialId { get; set; }
    public Guid WalletId { get; set; }
    public string VerifierId { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset PresentedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public int VerificationCount { get; set; }
    public List<string> DisclosedClaimNames { get; set; } = new();
}
