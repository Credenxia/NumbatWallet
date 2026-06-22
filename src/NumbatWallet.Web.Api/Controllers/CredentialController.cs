using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Credentials;
using NumbatWallet.Application.Queries.Wallets;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.Web.Api.Security;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/v1/credentials")] // plural alias for SDK clients (mirrors WalletController)
[Microsoft.AspNetCore.Authorization.Authorize]
[Produces("application/json")]
public class CredentialController : ControllerBase
{
    private readonly ICommandHandler<IssueCredentialCommand, CredentialDto> _issueCredentialHandler;
    private readonly ICommandHandler<VerifyCredentialCommand, VerificationResultDto> _verifyCredentialHandler;
    private readonly ICommandHandler<RevokeCredentialCommand, bool> _revokeCredentialHandler;
    private readonly ICommandHandler<ShareCredentialCommand, ShareCredentialResult> _shareCredentialHandler;
    private readonly ICommandHandler<RequestCredentialCommand, CredentialRequestDto> _requestCredentialHandler;
    private readonly IQueryHandler<GetCredentialByIdQuery, CredentialDto?> _getCredentialByIdHandler;
    private readonly IQueryHandler<GetCredentialsByWalletQuery, IEnumerable<CredentialDto>> _getCredentialsByWalletHandler;
    private readonly IQueryHandler<GetWalletByIdQuery, WalletDto?> _getWalletByIdHandler;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<CredentialController> _logger;

    public CredentialController(
        ICommandHandler<IssueCredentialCommand, CredentialDto> issueCredentialHandler,
        ICommandHandler<VerifyCredentialCommand, VerificationResultDto> verifyCredentialHandler,
        ICommandHandler<RevokeCredentialCommand, bool> revokeCredentialHandler,
        ICommandHandler<ShareCredentialCommand, ShareCredentialResult> shareCredentialHandler,
        ICommandHandler<RequestCredentialCommand, CredentialRequestDto> requestCredentialHandler,
        IQueryHandler<GetCredentialByIdQuery, CredentialDto?> getCredentialByIdHandler,
        IQueryHandler<GetCredentialsByWalletQuery, IEnumerable<CredentialDto>> getCredentialsByWalletHandler,
        IQueryHandler<GetWalletByIdQuery, WalletDto?> getWalletByIdHandler,
        ISecurityAuditService auditService,
        ILogger<CredentialController> logger)
    {
        _issueCredentialHandler = issueCredentialHandler;
        _verifyCredentialHandler = verifyCredentialHandler;
        _revokeCredentialHandler = revokeCredentialHandler;
        _shareCredentialHandler = shareCredentialHandler;
        _requestCredentialHandler = requestCredentialHandler;
        _getCredentialByIdHandler = getCredentialByIdHandler;
        _getCredentialsByWalletHandler = getCredentialsByWalletHandler;
        _getWalletByIdHandler = getWalletByIdHandler;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>The authenticated caller's person id (from the NameIdentifier claim), if any.</summary>
    private Guid? GetCallerPersonId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var g) ? g : null;
    }

    /// <summary>Roles allowed to access any holder's credentials (issuance/administration).</summary>
    private bool IsPrivileged()
        => User.IsInRole("Admin") || User.IsInRole("Officer") || User.IsInRole("Issuer") || User.IsInRole("TenantAdmin");

    /// <summary>True when the caller's person owns the given wallet (tenant filter already applied).</summary>
    private async Task<bool> CallerOwnsWalletAsync(Guid walletId)
    {
        var caller = GetCallerPersonId();
        if (caller is null)
        {
            return false;
        }

        var wallet = await _getWalletByIdHandler.HandleAsync(new GetWalletByIdQuery(walletId));
        return wallet is not null
            && string.Equals(wallet.PersonId, caller.Value.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ownership check for a credential whose holder is identified by <paramref name="holderId"/>.
    /// HolderId may be a wallet id or a person id, so both interpretations are accepted.
    /// </summary>
    private async Task<bool> CallerOwnsCredentialAsync(string? holderId)
    {
        var caller = GetCallerPersonId();
        if (caller is null || string.IsNullOrEmpty(holderId))
        {
            return false;
        }

        if (string.Equals(holderId, caller.Value.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return true; // holder is the caller's person id
        }

        return Guid.TryParse(holderId, out var walletId) && await CallerOwnsWalletAsync(walletId);
    }

    /// <summary>
    /// Issue a new verifiable credential
    /// </summary>
    [HttpPost("issue")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Issuer,Admin")]
    [ProducesResponseType(typeof(CredentialDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IssueCredential([FromBody] IssueCredentialRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Issuing credential for wallet {WalletId} by user {UserId}",
            request.WalletId, userId);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataModification,
            $"Credential issuance for wallet {request.WalletId}");

        // Parse credential type enum
        if (!Enum.TryParse<Domain.Enums.CredentialType>(request.CredentialType, out var credentialType))
        {
            return BadRequest($"Invalid credential type: {request.CredentialType}");
        }

        // The handler resolves the Issuer ENTITY by IssuerOrganizationId — the request's
        // IssuerId is that organisation's id. The command's IssuerId carries the issuing
        // PRINCIPAL (authenticated user) for provenance.
        var command = new IssueCredentialCommand(
            WalletId: request.WalletId,
            CredentialType: credentialType,
            Subject: request.Subject,
            Claims: request.Claims,
            ValidFrom: DateTime.UtcNow,
            ValidUntil: request.ExpiryDate,
            IssuerId: userId ?? "system",
            IssuerOrganizationId: request.IssuerId);

        var result = await _issueCredentialHandler.HandleAsync(command);

        // Return 201 Created with location header
        var locationUri = $"/api/v1/credential/{result.Id}";
        return Created(locationUri, result);
    }

    /// <summary>
    /// Get a specific credential by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CredentialDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCredentialById(Guid id)
    {
        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataAccess,
            $"Credential access: {id}");

        // Tenant filtering is handled by repository layer via ICurrentTenantService
        var query = new GetCredentialByIdQuery(Guid.Empty, id);
        var result = await _getCredentialByIdHandler.HandleAsync(query);

        if (result == null)
        {
            return NotFound($"Credential {id} not found");
        }

        // SECURITY: a non-privileged caller may only read credentials they hold.
        // 404 (not 403) so credential existence isn't disclosed to non-owners.
        if (!IsPrivileged() && !await CallerOwnsCredentialAsync(result.HolderId))
        {
            _logger.LogWarning("Blocked cross-holder credential access: caller {Caller} requested credential {CredentialId}",
                GetCallerPersonId(), id);
            return NotFound($"Credential {id} not found");
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all credentials for a wallet
    /// </summary>
    [HttpGet("wallet/{walletId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<CredentialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCredentialsByWallet(Guid walletId)
    {
        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataAccess,
            $"Wallet credentials access: {walletId}");

        // SECURITY: only privileged roles may list another holder's wallet credentials.
        if (!IsPrivileged() && !await CallerOwnsWalletAsync(walletId))
        {
            _logger.LogWarning("Blocked cross-holder credential listing: caller {Caller} requested wallet {WalletId}",
                GetCallerPersonId(), walletId);
            return Forbid();
        }

        // Extract tenant ID from JWT claims
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        var tenantId = !string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tid)
            ? tid
            : Guid.Empty;

        var query = new GetCredentialsByWalletQuery(tenantId, walletId);
        var result = await _getCredentialsByWalletHandler.HandleAsync(query);

        return Ok(result);
    }

    /// <summary>
    /// Verify a credential
    /// </summary>
    [HttpPost("verify")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(VerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyCredential([FromBody] VerifyCredentialRequestDto request)
    {
        _logger.LogInformation("Verifying credential data");

        var command = new VerifyCredentialCommand
        {
            CredentialId = string.Empty, // Application DTO doesn't have CredentialId on request
            CredentialData = request.CredentialData,
            VerificationOptions = request.VerificationOptions?.ToDictionary()
        };

        var result = await _verifyCredentialHandler.HandleAsync(command);

        return Ok(result);
    }

    /// <summary>
    /// Revoke a credential
    /// </summary>
    [HttpPost("{id:guid}/revoke")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Issuer,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeCredential(Guid id, [FromBody] RevokeCredentialRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogWarning("Revoking credential {CredentialId} by user {UserId}", id, userId);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataDeletion,
            $"Credential revocation: {id}");

        var command = new RevokeCredentialCommand(
            CredentialId: id,
            Reason: request.Reason,
            RevokerId: userId ?? "system");

        var result = await _revokeCredentialHandler.HandleAsync(command);

        if (!result)
        {
            return NotFound($"Credential {id} not found");
        }

        return Ok(new { message = "Credential revoked successfully", credentialId = id });
    }

    /// <summary>
    /// Share a credential with another party
    /// </summary>
    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(typeof(ShareCredentialResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShareCredential(Guid id, [FromBody] ShareCredentialRequestDto request)
    {
        _logger.LogInformation("Sharing credential {CredentialId} with {RecipientEmail}",
            id, request.RecipientEmail);

        // Use the share credential command handler
        var command = new ShareCredentialCommand(
            CredentialId: id,
            RecipientEmail: request.RecipientEmail,
            ExpiresInMinutes: request.ExpiryHours * 60,
            RequirePin: request.RequireAuthentication,
            Pin: null); // PIN would come from request if needed

        var result = await _shareCredentialHandler.HandleAsync(command);

        var response = new ShareCredentialResultDto
        {
            IsSuccess = true,
            ShareToken = null, // Would be generated by handler
            ShareUrl = result.ShareUrl,
            ExpiresAt = result.ExpiresAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Request a credential from an issuer
    /// </summary>
    [HttpPost("request")]
    [ProducesResponseType(typeof(CredentialRequestResponseDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RequestCredential([FromBody] RequestCredentialDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Credential request from user {UserId} to issuer {IssuerId}",
            userId, request.IssuerId);

        // Use the request credential command handler
        var command = new RequestCredentialCommand(
            WalletId: request.WalletId,
            IssuerId: request.IssuerId,
            CredentialType: request.CredentialType,
            RequestedClaims: request.RequestedClaims ?? new Dictionary<string, object>(),
            Justification: request.Justification);

        var result = await _requestCredentialHandler.HandleAsync(command);

        var response = new CredentialRequestResponseDto
        {
            RequestId = result.RequestId,
            Status = result.Status,
            RequestedAt = result.RequestedAt,
            Message = result.Message
        };

        return Accepted(response);
    }
}
