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

/// <summary>
/// Request DTO for creating an issuance
/// </summary>
public class CreateIssuanceRequestDto
{
    public required string CredentialType { get; set; }
    public Guid WalletId { get; set; }
    public string? RequesterId { get; set; }
    public Dictionary<string, object>? Claims { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Request DTO for approving an issuance
/// </summary>
public class ApproveIssuanceRequestDto
{
    public string? Comments { get; set; }
}

/// <summary>
/// Request DTO for rejecting an issuance
/// </summary>
public class RejectIssuanceRequestDto
{
    public required string Reason { get; set; }
    public string? Comments { get; set; }
}

/// <summary>
/// Request DTO for completing an issuance
/// </summary>
public class CompleteIssuanceRequestDto
{
    public Guid CredentialId { get; set; }
    public string? Comments { get; set; }
}