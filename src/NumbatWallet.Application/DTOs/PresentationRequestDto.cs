namespace NumbatWallet.Application.DTOs;

/// <summary>
/// EXPERIMENTAL (minimal OID4VP subset): a presentation request as served to wallets that
/// resolve the request URI (OID4VP request_uri style) — what is required, the nonce the
/// VP-JWT must carry, and the request lifecycle state.
/// </summary>
public class PresentationRequestDto
{
    public Guid RequestId { get; set; }
    public string VerifierId { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string RequestedCredentialType { get; set; } = string.Empty;
    public List<string> RequestedClaims { get; set; } = new();

    /// <summary>DIF Presentation Exchange v2 presentation_definition (JSON string).</summary>
    public string PresentationDefinition { get; set; } = string.Empty;

    /// <summary>One-time nonce the submitted VP-JWT must carry.</summary>
    public string Nonce { get; set; } = string.Empty;

    /// <summary>Pending, Fulfilled or Expired.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? FulfilledAt { get; set; }
}
