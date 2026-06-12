using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.Commands.Presentations;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Web.Api.Security;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.GraphQL.Mutations;

/// <summary>
/// GraphQL mutations for credential presentations (present → verify flow).
/// POA scope: signed-JWT presentation tokens, not W3C VP / OID4VP envelopes.
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
            SelectiveDisclosure: input.SelectiveClaims);

        return await presentCredentialHandler.HandleAsync(command);
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
}
