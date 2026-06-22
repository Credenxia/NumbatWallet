namespace NumbatWallet.Application.DTOs;

/// <summary>
/// Result of verifying a presentation token (verify-by-token flow).
/// Normal verification failures (bad signature, expired token, revoked credential, unknown
/// presentation) are expressed as IsValid=false with a Reason — they are not exceptions.
/// </summary>
public class PresentationVerificationResultDto
{
    public bool IsValid { get; set; }
    public string? Reason { get; set; }
    public Guid? PresentationId { get; set; }
    public Guid? CredentialId { get; set; }
    public string? CredentialType { get; set; }
    public Dictionary<string, object>? DisclosedClaims { get; set; }
    public DateTimeOffset? PresentedAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
}
