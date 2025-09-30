using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Issuances;

/// <summary>
/// Command to create a new issuance request
/// </summary>
public record CreateIssuanceCommand(
    string CredentialType,
    Guid WalletId,
    string? RequesterId,
    Dictionary<string, object>? Claims,
    DateTime? ExpiryDate,
    Dictionary<string, string>? Metadata) : ICommand<IssuanceDto>;

/// <summary>
/// Command to approve an issuance request
/// </summary>
public record ApproveIssuanceCommand(
    Guid IssuanceId,
    string ApprovedBy,
    string? Comments) : ICommand<IssuanceDto>;

/// <summary>
/// Command to reject an issuance request
/// </summary>
public record RejectIssuanceCommand(
    Guid IssuanceId,
    string RejectedBy,
    string Reason,
    string? Comments) : ICommand<IssuanceDto>;

/// <summary>
/// Command to complete an issuance (issue the credential)
/// </summary>
public record CompleteIssuanceCommand(
    Guid IssuanceId,
    string CompletedBy,
    Guid CredentialId,
    string? Comments) : ICommand<IssuanceDto>;

/// <summary>
/// Command to cancel an issuance request
/// </summary>
public record CancelIssuanceCommand(
    Guid IssuanceId,
    string CancelledBy,
    string Reason) : ICommand<bool>;