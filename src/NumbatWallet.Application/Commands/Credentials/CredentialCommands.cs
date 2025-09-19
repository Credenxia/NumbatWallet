using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Enums;

namespace NumbatWallet.Application.Commands.Credentials;

public record IssueCredentialCommand(
    Guid WalletId,
    CredentialType CredentialType,
    string Subject,
    Dictionary<string, object> Claims,
    DateTime ValidFrom,
    DateTime? ValidUntil,
    string IssuerId,
    Guid IssuerOrganizationId) : ICommand<CredentialDto>;

public record BulkIssueCredentialsCommand(
    List<Guid> WalletIds,
    CredentialType CredentialType,
    Dictionary<string, object> Template,
    string IssuerId,
    Guid IssuerOrganizationId,
    DateTime ValidFrom,
    DateTime? ValidUntil) : ICommand<BulkIssueResult>;

public record RevokeCredentialCommand(
    Guid CredentialId,
    string Reason,
    string RevokerId) : ICommand<bool>;

public record ShareCredentialCommand(
    Guid CredentialId,
    string RecipientEmail,
    int ExpiresInMinutes,
    bool RequirePin,
    string? Pin) : ICommand<ShareCredentialResult>;

public record PresentCredentialCommand(
    Guid CredentialId,
    string VerifierId,
    string Purpose,
    List<string>? SelectiveDisclosure) : ICommand<PresentationResult>;

public record ShareCredentialResult(
    string ShareUrl,
    string ShareCode,
    DateTime ExpiresAt);

public record PresentationResult(
    string PresentationToken,
    string VerificationUrl,
    DateTime PresentedAt,
    Dictionary<string, object> DisclosedClaims);

public record BulkIssueResult(
    int TotalRequested,
    int SuccessCount,
    int FailureCount,
    List<Guid> IssuedCredentialIds,
    List<BulkIssueError> Errors);

public record BulkIssueError(
    Guid WalletId,
    string Error);