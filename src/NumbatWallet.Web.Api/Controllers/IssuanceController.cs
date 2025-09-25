using NumbatWallet.Application.Commands.Issuances;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Application.Queries.Issuances;
using NumbatWallet.Web.Api.Security;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
[Produces("application/json")]
public class IssuanceController : ControllerBase
{
    private readonly ICommandHandler<CreateIssuanceCommand, IssuanceDto> _createIssuanceHandler;
    private readonly ICommandHandler<ApproveIssuanceCommand, IssuanceDto> _approveIssuanceHandler;
    private readonly ICommandHandler<RejectIssuanceCommand, IssuanceDto> _rejectIssuanceHandler;
    private readonly ICommandHandler<CompleteIssuanceCommand, IssuanceDto> _completeIssuanceHandler;
    private readonly ICommandHandler<CancelIssuanceCommand, bool> _cancelIssuanceHandler;
    private readonly IQueryHandler<GetIssuanceByIdQuery, IssuanceDto?> _getIssuanceByIdHandler;
    private readonly IQueryHandler<GetIssuancesByStatusQuery, IEnumerable<IssuanceDto>> _getIssuancesByStatusHandler;
    private readonly ICredentialService _credentialService;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<IssuanceController> _logger;

    public IssuanceController(
        ICommandHandler<CreateIssuanceCommand, IssuanceDto> createIssuanceHandler,
        ICommandHandler<ApproveIssuanceCommand, IssuanceDto> approveIssuanceHandler,
        ICommandHandler<RejectIssuanceCommand, IssuanceDto> rejectIssuanceHandler,
        ICommandHandler<CompleteIssuanceCommand, IssuanceDto> completeIssuanceHandler,
        ICommandHandler<CancelIssuanceCommand, bool> cancelIssuanceHandler,
        IQueryHandler<GetIssuanceByIdQuery, IssuanceDto?> getIssuanceByIdHandler,
        IQueryHandler<GetIssuancesByStatusQuery, IEnumerable<IssuanceDto>> getIssuancesByStatusHandler,
        ICredentialService credentialService,
        ISecurityAuditService auditService,
        ILogger<IssuanceController> logger)
    {
        _createIssuanceHandler = createIssuanceHandler;
        _approveIssuanceHandler = approveIssuanceHandler;
        _rejectIssuanceHandler = rejectIssuanceHandler;
        _completeIssuanceHandler = completeIssuanceHandler;
        _cancelIssuanceHandler = cancelIssuanceHandler;
        _getIssuanceByIdHandler = getIssuanceByIdHandler;
        _getIssuancesByStatusHandler = getIssuancesByStatusHandler;
        _credentialService = credentialService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new issuance request
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IssuanceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIssuance([FromBody] CreateIssuanceRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Creating issuance request for credential type {CredentialType} by user {UserId}",
            request.CredentialType, userId);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataModification,
            $"Issuance request created for {request.CredentialType}");

        // Convert additional data to claims if present
        var claims = request.AdditionalData?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>();

        // Add required documents to metadata if present
        var metadata = new Dictionary<string, string>();
        if (request.RequiredDocuments != null && request.RequiredDocuments.Any())
        {
            metadata["required_documents"] = string.Join(",", request.RequiredDocuments);
        }

        var command = new CreateIssuanceCommand(
            request.CredentialType,
            request.WalletId,
            request.RequesterId ?? userId,
            claims,
            null, // ExpiryDate - not provided in request DTO
            metadata);

        var result = await _createIssuanceHandler.HandleAsync(command);

        return CreatedAtAction(
            nameof(GetIssuanceById),
            new { id = result.Id },
            result);
    }

    /// <summary>
    /// Get an issuance by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IssuanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIssuanceById(Guid id)
    {
        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataAccess,
            $"Issuance access: {id}");

        try
        {
            var query = new GetIssuanceByIdQuery(id);
            var result = await _getIssuanceByIdHandler.HandleAsync(query);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Get issuances by status
    /// </summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<IssuanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIssuancesByStatus(string status)
    {
        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataAccess,
            $"Issuance list access by status: {status}");

        var query = new GetIssuancesByStatusQuery(status, null, null, null, null);
        var result = await _getIssuancesByStatusHandler.HandleAsync(query);

        return Ok(result);
    }

    /// <summary>
    /// Approve an issuance request
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Issuer,Admin")]
    [ProducesResponseType(typeof(IssuanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveIssuance(Guid id, [FromBody] ApproveIssuanceRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Approving issuance {IssuanceId} by user {UserId}", id, userId);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataModification,
            $"Issuance approved: {id}");

        var command = new ApproveIssuanceCommand(
            id,
            userId ?? "system",
            request.Comments);

        try
        {
            var result = await _approveIssuanceHandler.HandleAsync(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Reject an issuance request
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Issuer,Admin")]
    [ProducesResponseType(typeof(IssuanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectIssuance(Guid id, [FromBody] RejectIssuanceRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogWarning("Rejecting issuance {IssuanceId} by user {UserId}", id, userId);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataModification,
            $"Issuance rejected: {id}");

        var command = new RejectIssuanceCommand(
            id,
            userId ?? "system",
            request.Reason,
            null); // Comments - not in DTO

        try
        {
            var result = await _rejectIssuanceHandler.HandleAsync(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Complete an issuance and issue the credential
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Issuer,Admin")]
    [ProducesResponseType(typeof(IssuanceCompletionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteIssuance(Guid id, [FromBody] CompleteIssuanceRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Completing issuance {IssuanceId} by user {UserId}", id, userId);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataModification,
            $"Issuance completed: {id}");

        // Issue the credential based on the issuance request
        var issueCredentialDto = new Application.DTOs.IssueCredentialDto
        {
            WalletId = request.WalletId ?? Guid.NewGuid(), // Wallet ID from request
            IssuerId = userId ?? "system",
            Type = request.CredentialType ?? "GenericCredential",
            Data = request.CredentialData ?? new Dictionary<string, object>()
        };

        var credential = await _credentialService.IssueCredentialAsync(issueCredentialDto);
        var credentialId = Guid.Parse(credential.Id);

        var command = new CompleteIssuanceCommand(
            id,
            userId ?? "system",
            credentialId,
            request.Comments);

        try
        {
            var result = await _completeIssuanceHandler.HandleAsync(command);

            var response = new IssuanceCompletionResponseDto
            {
                IssuanceId = result.Id,
                Status = result.Status,
                CredentialId = result.CredentialId,
                IssuedAt = result.CompletedAt ?? DateTime.UtcNow,
                Message = "Credential has been successfully issued."
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Upload documents for an issuance request
    /// </summary>
    [HttpPost("{id:guid}/documents")]
    [ProducesResponseType(typeof(DocumentUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadDocuments(Guid id, [FromForm] IFormFileCollection files)
    {
        _logger.LogInformation("Uploading {Count} documents for issuance {IssuanceId}",
            files.Count, id);

        // Verify issuance exists
        var query = new GetIssuanceByIdQuery(id);
        var issuance = await _getIssuanceByIdHandler.HandleAsync(query);
        if (issuance == null)
        {
            return NotFound($"Issuance {id} not found");
        }

        var uploadedDocuments = new List<UploadedDocumentDto>();
        var documentsPath = System.IO.Path.Combine("uploads", "issuances", id.ToString());
        System.IO.Directory.CreateDirectory(documentsPath);

        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                var documentId = Guid.NewGuid();
                var fileName = $"{documentId}_{System.IO.Path.GetFileName(file.FileName)}";
                var filePath = System.IO.Path.Combine(documentsPath, fileName);

                // Save file to disk (in production, use blob storage)
                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                uploadedDocuments.Add(new UploadedDocumentDto
                {
                    DocumentId = documentId,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Size = file.Length,
                    UploadedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Document {DocumentId} uploaded for issuance {IssuanceId}",
                    documentId, id);
            }
        }

        var response = new DocumentUploadResponseDto
        {
            IssuanceId = id,
            UploadedDocuments = uploadedDocuments,
            Message = $"Successfully uploaded {uploadedDocuments.Count} documents."
        };

        return Ok(response);
    }

    /// <summary>
    /// Get pending issuances for review
    /// </summary>
    [HttpGet("pending")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Issuer,Admin")]
    [ProducesResponseType(typeof(IEnumerable<IssuanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingIssuances([FromQuery] int? limit = 50)
    {
        var query = new GetIssuancesByStatusQuery("Pending", null, null, limit, null);
        var result = await _getIssuancesByStatusHandler.HandleAsync(query);

        if (limit.HasValue)
        {
            result = result.Take(limit.Value);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get issuance statistics
    /// </summary>
    [HttpGet("statistics")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IssuanceStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIssuanceStatistics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var endDate = to ?? DateTime.UtcNow;

        // Get all issuances in the date range
        var allIssuancesQuery = new GetIssuancesByStatusQuery(null, startDate, endDate, null, null);
        var allIssuances = (await _getIssuancesByStatusHandler.HandleAsync(allIssuancesQuery)).ToList();

        // Get pending issuances
        var pendingQuery = new GetIssuancesByStatusQuery("Pending", startDate, endDate, null, null);
        var pendingIssuances = (await _getIssuancesByStatusHandler.HandleAsync(pendingQuery)).ToList();

        // Get approved issuances
        var approvedQuery = new GetIssuancesByStatusQuery("Approved", startDate, endDate, null, null);
        var approvedIssuances = (await _getIssuancesByStatusHandler.HandleAsync(approvedQuery)).ToList();

        // Get rejected issuances
        var rejectedQuery = new GetIssuancesByStatusQuery("Rejected", startDate, endDate, null, null);
        var rejectedIssuances = (await _getIssuancesByStatusHandler.HandleAsync(rejectedQuery)).ToList();

        // Calculate average processing time
        var completedIssuances = allIssuances.Where(i => i.CompletedAt.HasValue && i.CreatedAt != default);
        var averageProcessingTime = completedIssuances.Any()
            ? TimeSpan.FromMinutes(completedIssuances.Average(i => (i.CompletedAt!.Value - i.CreatedAt).TotalMinutes))
            : TimeSpan.Zero;

        // Group by credential type
        var issuancesByType = allIssuances
            .GroupBy(i => i.CredentialType ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        var stats = new IssuanceStatisticsDto
        {
            TotalIssuances = allIssuances.Count,
            PendingIssuances = pendingIssuances.Count,
            ApprovedIssuances = approvedIssuances.Count,
            RejectedIssuances = rejectedIssuances.Count,
            AverageProcessingTime = averageProcessingTime,
            IssuancesByType = issuancesByType,
            Period = new DateRangeDto
            {
                From = startDate,
                To = endDate
            }
        };

        return Ok(stats);
    }
}

// Request DTOs
public class CreateIssuanceRequestDto
{
    public required string CredentialType { get; set; }
    public string? RequesterId { get; set; }
    public Guid WalletId { get; set; }
    public List<string>? RequiredDocuments { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}

public class ApproveIssuanceRequestDto
{
    public string? Comments { get; set; }
}

public class RejectIssuanceRequestDto
{
    public required string Reason { get; set; }
}

public class CompleteIssuanceRequestDto
{
    public Guid? WalletId { get; set; }
    public string? CredentialType { get; set; }
    public Dictionary<string, object>? CredentialData { get; set; }
    public string? Comments { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

// Response DTOs
public class IssuanceCompletionResponseDto
{
    public Guid IssuanceId { get; set; }
    public required string Status { get; set; }
    public Guid? CredentialId { get; set; }
    public DateTime IssuedAt { get; set; }
    public string? Message { get; set; }
}

public class DocumentUploadResponseDto
{
    public Guid IssuanceId { get; set; }
    public List<UploadedDocumentDto> UploadedDocuments { get; set; } = new();
    public string? Message { get; set; }
}

public class UploadedDocumentDto
{
    public Guid DocumentId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class IssuanceStatisticsDto
{
    public int TotalIssuances { get; set; }
    public int PendingIssuances { get; set; }
    public int ApprovedIssuances { get; set; }
    public int RejectedIssuances { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public Dictionary<string, int> IssuancesByType { get; set; } = new();
    public DateRangeDto? Period { get; set; }
}

public class DateRangeDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}