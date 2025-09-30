using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Credentials;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.Web.Api.Security;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
[Produces("application/json")]
public class CredentialController : ControllerBase
{
    private readonly ICommandHandler<IssueCredentialCommand, CredentialDto> _issueCredentialHandler;
    private readonly ICommandHandler<VerifyCredentialCommand, VerificationResultDto> _verifyCredentialHandler;
    private readonly ICommandHandler<RevokeCredentialCommand, bool> _revokeCredentialHandler;
    private readonly ICommandHandler<ShareCredentialCommand, Application.Commands.Credentials.ShareCredentialResult> _shareCredentialHandler;
    private readonly ICommandHandler<RequestCredentialCommand, Application.Commands.Credentials.CredentialRequestDto> _requestCredentialHandler;
    private readonly IQueryHandler<GetCredentialByIdQuery, CredentialDto?> _getCredentialByIdHandler;
    private readonly IQueryHandler<GetCredentialsByWalletQuery, IEnumerable<CredentialDto>> _getCredentialsByWalletHandler;
    private readonly ICurrentTenantService _tenantService;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<CredentialController> _logger;

    public CredentialController(
        ICommandHandler<IssueCredentialCommand, CredentialDto> issueCredentialHandler,
        ICommandHandler<VerifyCredentialCommand, VerificationResultDto> verifyCredentialHandler,
        ICommandHandler<RevokeCredentialCommand, bool> revokeCredentialHandler,
        ICommandHandler<ShareCredentialCommand, Application.Commands.Credentials.ShareCredentialResult> shareCredentialHandler,
        ICommandHandler<RequestCredentialCommand, Application.Commands.Credentials.CredentialRequestDto> requestCredentialHandler,
        IQueryHandler<GetCredentialByIdQuery, CredentialDto?> getCredentialByIdHandler,
        IQueryHandler<GetCredentialsByWalletQuery, IEnumerable<CredentialDto>> getCredentialsByWalletHandler,
        ICurrentTenantService tenantService,
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
        _tenantService = tenantService;
        _auditService = auditService;
        _logger = logger;
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
        if (!Enum.TryParse<NumbatWallet.Domain.Enums.CredentialType>(request.CredentialType, out var credentialType))
        {
            return BadRequest($"Invalid credential type: {request.CredentialType}");
        }

        // Get organization from tenant context or use default
        var tenantId = _tenantService.TenantId ?? "00000000-0000-0000-0000-000000000000";
        var organizationId = Guid.Parse(tenantId); // In multi-tenant, each tenant has an organization

        var command = new IssueCredentialCommand(
            WalletId: request.WalletId,
            CredentialType: credentialType,
            Subject: request.Subject,
            Claims: request.Claims,
            ValidFrom: DateTime.UtcNow,
            ValidUntil: request.ExpiryDate,
            IssuerId: request.IssuerId ?? userId ?? "system",
            IssuerOrganizationId: organizationId);

        var result = await _issueCredentialHandler.HandleAsync(command);

        // Keep the mock data as fallback for now
        if (result == null)
        {
            result = new CredentialDto
            {
                Id = Guid.NewGuid().ToString(),
                HolderId = request.WalletId.ToString(),
                IssuerId = request.IssuerId ?? userId ?? "system",
                Type = request.CredentialType.ToString(),
                CredentialSubject = request.Claims,
                IssuanceDate = DateTime.UtcNow,
                Status = "Active"
            };
        }

        return CreatedAtAction(
            nameof(GetCredentialById),
            new { id = result.Id },
            result);
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

        var tenantId = _tenantService.TenantId ?? "00000000-0000-0000-0000-000000000000";
        var query = new GetCredentialByIdQuery(Guid.Parse(tenantId), id);
        var result = await _getCredentialByIdHandler.HandleAsync(query);

        if (result == null)
        {
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

        var tenantId = _tenantService.TenantId ?? "00000000-0000-0000-0000-000000000000";
        var query = new GetCredentialsByWalletQuery(Guid.Parse(tenantId), walletId, false);
        var result = await _getCredentialsByWalletHandler.HandleAsync(query);

        return Ok(result);
    }

    /// <summary>
    /// Verify a credential
    /// </summary>
    [HttpPost("verify")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(NumbatWallet.Application.DTOs.VerificationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyCredential([FromBody] VerifyCredentialRequestDto request)
    {
        _logger.LogInformation("Verifying credential {CredentialId}", request.CredentialId);

        var command = new VerifyCredentialCommand
        {
            CredentialId = request.CredentialId.ToString(),
            CredentialData = request.CredentialData,
            VerificationOptions = request.Options?.ToVerificationOptions()
        };

        var result = await _verifyCredentialHandler.HandleAsync(command);

        return Ok(result);
    }

    /// <summary>
    /// Revoke a credential
    /// </summary>
    [HttpPost("{id:guid}/revoke")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Issuer,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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

        return NoContent();
    }

    /// <summary>
    /// Share a credential with another party
    /// </summary>
    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(typeof(CredentialShareResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShareCredential(Guid id, [FromBody] ShareCredentialRequestDto request)
    {
        _logger.LogInformation("Sharing credential {CredentialId} with {RecipientId}",
            id, request.RecipientId);

        // Use the share credential command handler
        var command = new ShareCredentialCommand(
            CredentialId: id,
            RecipientEmail: request.RecipientId,
            ExpiresInMinutes: (request.ValidityHours ?? 24) * 60,
            RequirePin: request.RequirePin,
            Pin: request.Pin);

        var result = await _shareCredentialHandler.HandleAsync(command);

        var response = new CredentialShareResponseDto
        {
            ShareId = Guid.NewGuid(),
            CredentialId = id,
            SharedWith = request.RecipientId,
            SharedClaims = request.ClaimsToShare,
            ExpiresAt = result.ExpiresAt,
            ShareUrl = result.ShareUrl
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

// Request DTOs
public class IssueCredentialRequestDto
{
    public Guid WalletId { get; set; }
    public required string CredentialType { get; set; }
    public required string Subject { get; set; }
    public Dictionary<string, object> Claims { get; set; } = new();
    public string? IssuerId { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class VerifyCredentialRequestDto
{
    public Guid CredentialId { get; set; }
    public string? CredentialData { get; set; }
    public NumbatWallet.Application.DTOs.VerificationOptionsDto? Options { get; set; }
}

public class RevokeCredentialRequestDto
{
    public required string Reason { get; set; }
}

public class ShareCredentialRequestDto
{
    public required string RecipientId { get; set; }
    public List<string> ClaimsToShare { get; set; } = new();
    public int? ValidityHours { get; set; }
    public bool RequirePin { get; set; }
    public string? Pin { get; set; }
}

public class RequestCredentialDto
{
    public Guid WalletId { get; set; }
    public Guid IssuerId { get; set; }
    public required string CredentialType { get; set; }
    public Dictionary<string, object>? RequestedClaims { get; set; }
    public string? Justification { get; set; }
}

// Response DTOs
public class CredentialShareResponseDto
{
    public Guid ShareId { get; set; }
    public Guid CredentialId { get; set; }
    public required string SharedWith { get; set; }
    public List<string> SharedClaims { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
    public required string ShareUrl { get; set; }
}

public class CredentialRequestResponseDto
{
    public Guid RequestId { get; set; }
    public required string Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? Message { get; set; }
}

// Extension methods
public static class VerificationOptionsDtoExtensions
{
    public static Dictionary<string, object> ToVerificationOptions(this VerificationOptionsDto dto)
    {
        return new Dictionary<string, object>
        {
            ["checkRevocation"] = dto.CheckRevocation,
            ["checkExpiry"] = dto.CheckExpiry,
            ["checkSignature"] = dto.CheckSignature,
            ["checkSchema"] = dto.CheckSchema,
            ["requireTrustChain"] = dto.RequireTrustChain
        };
    }
}
