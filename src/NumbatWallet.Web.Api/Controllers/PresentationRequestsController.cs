using NumbatWallet.Application.Commands.Presentations;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Presentations;

namespace NumbatWallet.Web.Api.Controllers;

/// <summary>
/// EXPERIMENTAL (minimal OID4VP subset): REST surface for the presentation-request exchange
/// so non-GraphQL verifiers can integrate.
/// <list type="bullet">
/// <item>POST /api/v1/presentations/requests — create a request (auth required).</item>
/// <item>GET /api/v1/presentations/requests/{id} — resolve a request (the request_uri target;
/// anonymous, mirrors how wallets dereference OID4VP request URIs).</item>
/// <item>POST /api/v1/presentations/requests/{id}/submit — submit a VP-JWT (anonymous,
/// mirrors verifyPresentation; one-shot, replays rejected).</item>
/// </list>
/// </summary>
[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v1/presentations/requests")]
[Microsoft.AspNetCore.Authorization.Authorize]
[Produces("application/json")]
public class PresentationRequestsController : ControllerBase
{
    private readonly ICommandHandler<CreatePresentationRequestCommand, PresentationRequestResult> _createRequestHandler;
    private readonly ICommandHandler<SubmitPresentationCommand, PresentationVerificationResultDto> _submitPresentationHandler;
    private readonly IQueryHandler<GetPresentationRequestByIdQuery, PresentationRequestDto?> _getRequestByIdHandler;
    private readonly ILogger<PresentationRequestsController> _logger;

    public PresentationRequestsController(
        ICommandHandler<CreatePresentationRequestCommand, PresentationRequestResult> createRequestHandler,
        ICommandHandler<SubmitPresentationCommand, PresentationVerificationResultDto> submitPresentationHandler,
        IQueryHandler<GetPresentationRequestByIdQuery, PresentationRequestDto?> getRequestByIdHandler,
        ILogger<PresentationRequestsController> logger)
    {
        _createRequestHandler = createRequestHandler;
        _submitPresentationHandler = submitPresentationHandler;
        _getRequestByIdHandler = getRequestByIdHandler;
        _logger = logger;
    }

    /// <summary>
    /// Create a presentation request (verifier side). Returns the DIF Presentation Exchange v2
    /// presentation_definition, the request URI and the one-time nonce the holder's VP must carry.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PresentationRequestResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRequest([FromBody] CreatePresentationRequestRequestDto request)
    {
        _logger.LogInformation(
            "Creating presentation request for verifier {VerifierId} (type {CredentialType})",
            request.VerifierId, request.RequestedCredentialType);

        var command = new CreatePresentationRequestCommand(
            VerifierId: request.VerifierId,
            Purpose: request.Purpose,
            RequestedCredentialType: request.RequestedCredentialType,
            RequestedClaims: request.RequestedClaims);

        var result = await _createRequestHandler.HandleAsync(command);

        return CreatedAtAction(nameof(GetRequest), new { id = result.RequestId }, result);
    }

    /// <summary>
    /// Resolve a presentation request — the target of the request URI handed to wallets.
    /// Anonymous by design: the id is an unguessable capability, and the wallet needs the
    /// definition + nonce to build its presentation.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    // One-shot lifecycle state must always be live — never serve a cached Pending after fulfilment.
    [Microsoft.AspNetCore.OutputCaching.OutputCache(NoStore = true)]
    [ProducesResponseType(typeof(PresentationRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequest(Guid id)
    {
        var dto = await _getRequestByIdHandler.HandleAsync(new GetPresentationRequestByIdQuery(id));
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// Submit a VP-JWT against a pending presentation request (holder/wallet side).
    /// One-shot: the first valid submission fulfils the request, replays are rejected.
    /// All failures return 200 with IsValid=false and a precise reason (mirrors verifyPresentation).
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(PresentationVerificationResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitPresentation(Guid id, [FromBody] SubmitPresentationRequestDto request)
    {
        _logger.LogInformation("Submitting presentation for request {RequestId}", id);

        var result = await _submitPresentationHandler.HandleAsync(
            new SubmitPresentationCommand(id, request.VpToken));

        return Ok(result);
    }
}

/// <summary>Request body for creating a presentation request.</summary>
public class CreatePresentationRequestRequestDto
{
    public required string VerifierId { get; set; }
    public required string Purpose { get; set; }
    public required string RequestedCredentialType { get; set; }
    public required List<string> RequestedClaims { get; set; }
}

/// <summary>Request body for submitting a VP-JWT against a presentation request.</summary>
public class SubmitPresentationRequestDto
{
    public required string VpToken { get; set; }
}
