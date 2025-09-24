namespace NumbatWallet.Application.DTOs;

/// <summary>
/// Represents an issuance request for a credential
/// </summary>
public class IssuanceDto
{
    public Guid Id { get; set; }
    public required string CredentialType { get; set; }
    public required string RequesterId { get; set; }
    public Guid WalletId { get; set; }
    public required string Status { get; set; }
    public List<string> RequiredDocuments { get; set; } = new();
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public string? RejectedBy { get; set; }
    public string? CompletedBy { get; set; }
    public string? RejectionReason { get; set; }
    public string? Comments { get; set; }
    public Guid? CredentialId { get; set; }
    public Dictionary<string, object> CredentialData { get; set; } = new();
}