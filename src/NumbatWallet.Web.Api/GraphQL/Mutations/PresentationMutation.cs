using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.Commands.Presentations;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Web.Api.Security;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.GraphQL.Mutations;

/// <summary>
/// GraphQL mutations for credential presentations (present → verify flow).
/// Presentation tokens are W3C JWT-VPs embedding the presented credential as a JWT-VC;
/// the API surface treats them as opaque strings.
/// </summary>
[ExtendObjectType("Mutation")]
public class PresentationMutation
{
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<PresentationMutation> _logger;

    // NOTE: command handlers are scoped (they depend on the scoped DbContext) and MUST be
    // resolved per-resolver via [Service] parameters, NOT injected into this constructor —
    // HotChocolate object types are singletons, so constructor-injecting a scoped handler
    // captures (and then reuses) a DbContext that is disposed after the first request.
    public PresentationMutation(
        ISecurityAuditService auditService,
        ILogger<PresentationMutation> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Present a credential to a verifier, returning a signed presentation token and
    /// verification URL. Claims can be selectively disclosed via selectiveClaims.
    /// </summary>
    [GraphQLDescription("Present a credential to a verifier with optional selective disclosure")]
    [Authorize]
    public async Task<PresentationResult> PresentCredential(
        PresentCredentialInput input,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ICommandHandler<PresentCredentialCommand, PresentationResult> presentCredentialHandler)
    {
        var httpContext = httpContextAccessor.HttpContext;

        _logger.LogInformation("Presenting credential {CredentialId} to verifier {VerifierId}",
            input.CredentialId, input.VerifierId);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataAccess,
                $"Credential presented: {input.CredentialId} to {input.VerifierId}");
        }

        var command = new PresentCredentialCommand(
            CredentialId: input.CredentialId,
            VerifierId: input.VerifierId,
            Purpose: input.Purpose,
            SelectiveDisclosure: input.SelectiveClaims,
            Nonce: input.Nonce);

        return await presentCredentialHandler.HandleAsync(command);
    }

    /// <summary>
    /// EXPERIMENTAL (minimal OID4VP subset): create a presentation request stating the
    /// credential type and claims a verifier requires. Returns a DIF Presentation Exchange v2
    /// presentation_definition, a request URI and the one-time nonce the holder's VP must carry.
    /// </summary>
    [GraphQLDescription("EXPERIMENTAL: Create an OID4VP-style presentation request (DIF Presentation Exchange v2 definition + one-time nonce)")]
    [Authorize]
    public async Task<PresentationRequestResult> CreatePresentationRequest(
        CreatePresentationRequestInput input,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ICommandHandler<CreatePresentationRequestCommand, PresentationRequestResult> createRequestHandler)
    {
        _logger.LogInformation(
            "Creating presentation request for verifier {VerifierId} (type {CredentialType})",
            input.VerifierId, input.RequestedCredentialType);

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataAccess,
                $"Presentation request created for verifier {input.VerifierId}");
        }

        var command = new CreatePresentationRequestCommand(
            VerifierId: input.VerifierId,
            Purpose: input.Purpose,
            RequestedCredentialType: input.RequestedCredentialType,
            RequestedClaims: input.RequestedClaims);

        return await createRequestHandler.HandleAsync(command);
    }

    /// <summary>
    /// EXPERIMENTAL (minimal OID4VP subset): submit a VP-JWT against a pending presentation
    /// request. One-shot: replays are rejected. Anonymous by design (mirrors verifyPresentation):
    /// the submitter is the wallet/holder side of the exchange holding only the token.
    /// </summary>
    [GraphQLDescription("EXPERIMENTAL: Submit a VP-JWT against an OID4VP-style presentation request (one-shot; replays rejected)")]
    [AllowAnonymous]
    public async Task<PresentationVerificationResultDto> SubmitPresentation(
        Guid requestId,
        string vpToken,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ICommandHandler<SubmitPresentationCommand, PresentationVerificationResultDto> submitPresentationHandler)
    {
        _logger.LogInformation("Submitting presentation for request {RequestId}", requestId);

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataAccess,
                $"Presentation submitted for request {requestId}");
        }

        var command = new SubmitPresentationCommand(requestId, vpToken);
        return await submitPresentationHandler.HandleAsync(command);
    }

    /// <summary>
    /// Verify a presentation token. Anonymous by design (mirrors verifyCredential):
    /// verifiers are external parties holding only the token.
    /// </summary>
    [GraphQLDescription("Verify a presentation token issued by presentCredential")]
    [AllowAnonymous]
    public async Task<PresentationVerificationResultDto> VerifyPresentation(
        string token,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ICommandHandler<VerifyPresentationCommand, PresentationVerificationResultDto> verifyPresentationHandler)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var verifierId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Verifying presentation token");

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataAccess,
                "Presentation verification attempted");
        }

        var command = new VerifyPresentationCommand(token, verifierId);
        return await verifyPresentationHandler.HandleAsync(command);
    }
}

/// <summary>
/// Input for the presentCredential mutation.
/// </summary>
public class PresentCredentialInput
{
    public required Guid CredentialId { get; set; }
    public required string VerifierId { get; set; }
    public required string Purpose { get; set; }

    [GraphQLDescription("Names of the claims to disclose; null/empty discloses all claims")]
    public List<string>? SelectiveClaims { get; set; }

    [GraphQLDescription("EXPERIMENTAL: nonce from an OID4VP presentation request; the VP-JWT will carry it so submitPresentation can bind the token to the request. Omit for ad-hoc presentations (a random nonce is generated).")]
    public string? Nonce { get; set; }
}

/// <summary>
/// Input for the createPresentationRequest mutation (EXPERIMENTAL minimal OID4VP subset).
/// </summary>
public class CreatePresentationRequestInput
{
    public required string VerifierId { get; set; }
    public required string Purpose { get; set; }

    [GraphQLDescription("Credential type the verifier requires, e.g. ProofOfAge")]
    public required string RequestedCredentialType { get; set; }

    [GraphQLDescription("Claim names the holder must disclose")]
    public required List<string> RequestedClaims { get; set; }
}
