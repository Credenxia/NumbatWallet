using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Presentations;

/// <summary>
/// EXPERIMENTAL (minimal OID4VP subset): a verifier creates a presentation request stating
/// the credential type and claims it needs. The holder later presents a VP-JWT carrying the
/// request's nonce and submits it via <see cref="SubmitPresentationCommand"/>.
/// </summary>
public record CreatePresentationRequestCommand(
    string VerifierId,
    string Purpose,
    string RequestedCredentialType,
    List<string> RequestedClaims) : ICommand<PresentationRequestResult>;

/// <summary>
/// Result of creating a presentation request.
/// </summary>
/// <param name="RequestId">Identifier of the persisted request (one-shot fulfilment).</param>
/// <param name="PresentationDefinition">DIF Presentation Exchange v2 presentation_definition (JSON string).</param>
/// <param name="RequestUri">URI a wallet can resolve to retrieve this request (OID4VP request_uri style).</param>
/// <param name="Nonce">One-time nonce the submitted VP-JWT must carry.</param>
/// <param name="ExpiresAt">When the request stops accepting submissions.</param>
public record PresentationRequestResult(
    Guid RequestId,
    string PresentationDefinition,
    string RequestUri,
    string Nonce,
    DateTimeOffset ExpiresAt);

/// <summary>
/// EXPERIMENTAL (minimal OID4VP subset): submits a VP-JWT against a pending presentation
/// request. Verifies the token (full Phase-1 W3C VP verification) AND that it satisfies the
/// request: nonce match, audience match, requested credential type, and all requested claims
/// disclosed. Fulfilment is one-shot — replays are rejected.
/// All failures are normal outcomes (IsValid=false with a precise reason), never exceptions.
/// </summary>
public record SubmitPresentationCommand(
    Guid RequestId,
    string VpToken) : ICommand<PresentationVerificationResultDto>;
