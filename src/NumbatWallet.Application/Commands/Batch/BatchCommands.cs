using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Batch;

/// <summary>
/// Command for batch credential issuance
/// </summary>
public record BatchIssueCredentialsCommand(
    List<BatchIssueCredentialItem> Credentials,
    string IssuerId) : ICommand<BatchOperationResultDto<CredentialDto>>;

/// <summary>
/// Item for batch credential issuance
/// </summary>
public class BatchIssueCredentialItem
{
    public string BatchItemId { get; set; } = Guid.NewGuid().ToString();
    public string HolderId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Claims { get; set; } = new();
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// Command for batch credential verification
/// </summary>
public record BatchVerifyCredentialsCommand(
    List<BatchVerifyCredentialItem> Credentials) : ICommand<BatchOperationResultDto<VerificationResultDto>>;

/// <summary>
/// Item for batch credential verification
/// </summary>
public class BatchVerifyCredentialItem
{
    public string BatchItemId { get; set; } = Guid.NewGuid().ToString();
    public string CredentialId { get; set; } = string.Empty;
    public string? CredentialData { get; set; }
}

/// <summary>
/// Command for batch credential revocation
/// </summary>
public record BatchRevokeCredentialsCommand(
    List<BatchRevokeCredentialItem> Credentials,
    string RevokerId) : ICommand<BatchOperationResultDto<bool>>;

/// <summary>
/// Item for batch credential revocation
/// </summary>
public class BatchRevokeCredentialItem
{
    public string BatchItemId { get; set; } = Guid.NewGuid().ToString();
    public string CredentialId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Command for batch issuance approval
/// </summary>
public record BatchApproveIssuancesCommand(
    List<Guid> IssuanceIds,
    string ApproverId) : ICommand<BatchOperationResultDto<IssuanceDto>>;

/// <summary>
/// Batch operation result
/// </summary>
public class BatchOperationResultDto<T>
{
    public int TotalItems { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BatchOperationItemResult<T>> Results { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// Individual batch operation item result
/// </summary>
public class BatchOperationItemResult<T>
{
    public string? ItemId { get; set; }
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
}