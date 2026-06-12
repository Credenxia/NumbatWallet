using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Presentations;

/// <summary>
/// Verifies a presentation TOKEN previously issued by PresentCredentialCommand (the token's
/// <c>jti</c> identifies the persisted Presentation record).
/// Reconciliation note: this token-based flow supersedes the unhandled draft commands in
/// Commands/Verifications/VerificationCommands.cs (CreatePresentationCommand /
/// VerifyCredentialCommand there have no handlers) for the POA presentation lifecycle.
/// </summary>
/// <param name="PresentationToken">The signed JWT presentation token handed to the verifier.</param>
/// <param name="VerifierId">Optional identifier of the verifying party (for auditing).</param>
public record VerifyPresentationCommand(
    string PresentationToken,
    string? VerifierId = null) : ICommand<PresentationVerificationResultDto>;
