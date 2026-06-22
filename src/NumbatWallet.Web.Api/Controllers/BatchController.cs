using NumbatWallet.Application.Commands.Batch;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Web.Api.Security;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
[Produces("application/json")]
public class BatchController : ControllerBase
{
    private readonly ICommandHandler<BatchIssueCredentialsCommand, BatchOperationResultDto<CredentialDto>> _batchIssueHandler;
    private readonly ICommandHandler<BatchVerifyCredentialsCommand, BatchOperationResultDto<VerificationResultDto>> _batchVerifyHandler;
    private readonly ICommandHandler<BatchRevokeCredentialsCommand, BatchOperationResultDto<bool>> _batchRevokeHandler;
    private readonly ICommandHandler<BatchApproveIssuancesCommand, BatchOperationResultDto<IssuanceDto>> _batchApproveHandler;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<BatchController> _logger;
    private readonly ICacheService _cacheService;

    public BatchController(
        ICommandHandler<BatchIssueCredentialsCommand, BatchOperationResultDto<CredentialDto>> batchIssueHandler,
        ICommandHandler<BatchVerifyCredentialsCommand, BatchOperationResultDto<VerificationResultDto>> batchVerifyHandler,
        ICommandHandler<BatchRevokeCredentialsCommand, BatchOperationResultDto<bool>> batchRevokeHandler,
        ICommandHandler<BatchApproveIssuancesCommand, BatchOperationResultDto<IssuanceDto>> batchApproveHandler,
        ISecurityAuditService auditService,
        ILogger<BatchController> logger,
        ICacheService cacheService)
    {
        _batchIssueHandler = batchIssueHandler;
        _batchVerifyHandler = batchVerifyHandler;
        _batchRevokeHandler = batchRevokeHandler;
        _batchApproveHandler = batchApproveHandler;
        _auditService = auditService;
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Issue multiple credentials in a single batch operation
    /// </summary>
    [HttpPost("credentials/issue")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Issuer,Admin")]
    [ProducesResponseType(typeof(BatchOperationResultDto<CredentialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchIssueCredentials([FromBody] BatchIssueCredentialsRequestDto request)
    {
        if (request.Credentials.Count > 100)
        {
            return BadRequest("Batch size cannot exceed 100 credentials");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Batch issuing {Count} credentials by user {UserId}",
            request.Credentials.Count, userId);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataModification,
            $"Batch credential issuance: {request.Credentials.Count} items");

        // Convert request to batch command
        var batchItems = request.Credentials.Select(c => new BatchIssueCredentialItem
        {
            BatchItemId = c.BatchItemId ?? Guid.NewGuid().ToString(),
            HolderId = c.HolderId,
            Type = c.Type,
            Claims = c.Claims,
            ExpiryDate = c.ExpiryDate
        }).ToList();

        var command = new BatchIssueCredentialsCommand(
            Credentials: batchItems,
            IssuerId: userId ?? "system");

        // Execute batch issuance
        var response = await _batchIssueHandler.HandleAsync(command);

        return Ok(response);
    }

    /// <summary>
    /// Verify multiple credentials in a single batch operation
    /// </summary>
    [HttpPost("credentials/verify")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(BatchOperationResultDto<VerificationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchVerifyCredentials([FromBody] BatchVerifyCredentialsRequestDto request)
    {
        if (request.Credentials.Count > 100)
        {
            return BadRequest("Batch size cannot exceed 100 credentials");
        }

        _logger.LogInformation("Batch verifying {Count} credentials", request.Credentials.Count);

        // Convert request to batch command
        var batchItems = request.Credentials.Select(c => new BatchVerifyCredentialItem
        {
            BatchItemId = c.BatchItemId ?? Guid.NewGuid().ToString(),
            CredentialId = c.CredentialId,
            CredentialData = c.CredentialData
        }).ToList();

        var command = new BatchVerifyCredentialsCommand(batchItems);

        // Execute batch verification
        var response = await _batchVerifyHandler.HandleAsync(command);

        return Ok(response);
    }

    /// <summary>
    /// Revoke multiple credentials in a single batch operation
    /// </summary>
    [HttpPost("credentials/revoke")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Issuer,Admin")]
    [ProducesResponseType(typeof(BatchOperationResultDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchRevokeCredentials([FromBody] BatchRevokeCredentialsRequestDto request)
    {
        if (request.Credentials.Count > 100)
        {
            return BadRequest("Batch size cannot exceed 100 credentials");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogWarning("Batch revoking {Count} credentials by user {UserId}",
            request.Credentials.Count, userId);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataDeletion,
            $"Batch credential revocation: {request.Credentials.Count} items");

        // Convert request to batch command
        var batchItems = request.Credentials.Select(c => new BatchRevokeCredentialItem
        {
            BatchItemId = c.CredentialId, // Use credential ID as batch item ID
            CredentialId = c.CredentialId,
            Reason = c.Reason
        }).ToList();

        var command = new BatchRevokeCredentialsCommand(
            Credentials: batchItems,
            RevokerId: userId ?? "system");

        // Execute batch revocation
        var response = await _batchRevokeHandler.HandleAsync(command);

        return Ok(response);
    }

    /// <summary>
    /// Process multiple issuance approvals in batch
    /// </summary>
    [HttpPost("issuances/approve")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Issuer,Admin")]
    [ProducesResponseType(typeof(BatchOperationResultDto<IssuanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchApproveIssuances([FromBody] BatchApproveIssuancesRequestDto request)
    {
        if (request.Issuances.Count > 50)
        {
            return BadRequest("Batch size cannot exceed 50 issuances");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Batch approving {Count} issuances by user {UserId}",
            request.Issuances.Count, userId);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataModification,
            $"Batch issuance approval: {request.Issuances.Count} items");

        // Convert request to batch command
        var issuanceIds = request.Issuances.Select(i => i.IssuanceId).ToList();

        var command = new BatchApproveIssuancesCommand(
            IssuanceIds: issuanceIds,
            ApproverId: userId ?? "system");

        // Execute batch approval
        var response = await _batchApproveHandler.HandleAsync(command);

        return Ok(response);
    }

    /// <summary>
    /// Get batch operation status
    /// </summary>
    [HttpGet("status/{batchId:guid}")]
    [ProducesResponseType(typeof(BatchOperationStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchStatus(Guid batchId)
    {
        // Get status from cache
        var status = await _cacheService.GetAsync<BatchOperationStatusDto>(
            $"batch:{batchId}");

        if (status == null)
        {
            return NotFound($"Batch operation {batchId} not found");
        }

        return Ok(status);
    }
}

public class BatchOperationStatusDto
{
    public Guid BatchId { get; set; }
    public string Status { get; set; } = "Processing";
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
}

// Request DTOs
public class BatchIssueCredentialsRequestDto
{
    public List<BatchIssueCredentialItemDto> Credentials { get; set; } = new();
}

public class BatchIssueCredentialItemDto
{
    public string? BatchItemId { get; set; }
    public required string HolderId { get; set; }
    public required string Type { get; set; }
    public Dictionary<string, object> Claims { get; set; } = new();
    public DateTime? ExpiryDate { get; set; }
}

public class BatchVerifyCredentialsRequestDto
{
    public List<BatchVerifyCredentialItemDto> Credentials { get; set; } = new();
}

public class BatchVerifyCredentialItemDto
{
    public string? BatchItemId { get; set; }
    public required string CredentialId { get; set; }
    public string? CredentialData { get; set; }
    public VerificationOptionsDto? Options { get; set; }
}

public class BatchRevokeCredentialsRequestDto
{
    public List<BatchRevokeCredentialItemDto> Credentials { get; set; } = new();
    public required string Reason { get; set; }
}

public class BatchRevokeCredentialItemDto
{
    public required string CredentialId { get; set; }
    public required string Reason { get; set; }
}

public class BatchApproveIssuancesRequestDto
{
    public List<BatchApproveIssuanceItemDto> Issuances { get; set; } = new();
}

public class BatchApproveIssuanceItemDto
{
    public Guid IssuanceId { get; set; }
    public string? Comments { get; set; }
}