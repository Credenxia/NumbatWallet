using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.Commands.Issuances;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Web.Api.GraphQL.Schema;
using NumbatWallet.Web.Api.Security;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.GraphQL.Mutations;

/// <summary>
/// GraphQL mutations for credential operations
/// </summary>
[ExtendObjectType("Mutation")]
public class CredentialMutation
{
    private readonly ICommandHandler<VerifyCredentialCommand, VerificationResultDto> _verifyCredentialHandler;
    private readonly ICommandHandler<RevokeCredentialCommand, bool> _revokeCredentialHandler;
    private readonly ICommandHandler<CreateIssuanceCommand, IssuanceDto> _createIssuanceHandler;
    private readonly ICommandHandler<ApproveIssuanceCommand, IssuanceDto> _approveIssuanceHandler;
    private readonly ICommandHandler<RejectIssuanceCommand, IssuanceDto> _rejectIssuanceHandler;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<CredentialMutation> _logger;

    public CredentialMutation(
        ICommandHandler<VerifyCredentialCommand, VerificationResultDto> verifyCredentialHandler,
        ICommandHandler<RevokeCredentialCommand, bool> revokeCredentialHandler,
        ICommandHandler<CreateIssuanceCommand, IssuanceDto> createIssuanceHandler,
        ICommandHandler<ApproveIssuanceCommand, IssuanceDto> approveIssuanceHandler,
        ICommandHandler<RejectIssuanceCommand, IssuanceDto> rejectIssuanceHandler,
        ISecurityAuditService auditService,
        ILogger<CredentialMutation> logger)
    {
        _verifyCredentialHandler = verifyCredentialHandler;
        _revokeCredentialHandler = revokeCredentialHandler;
        _createIssuanceHandler = createIssuanceHandler;
        _approveIssuanceHandler = approveIssuanceHandler;
        _rejectIssuanceHandler = rejectIssuanceHandler;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Verify a credential
    /// </summary>
    [GraphQLDescription("Verify a verifiable credential")]
    [HotChocolate.Authorization.AllowAnonymous]
    public async Task<VerificationResultDto> VerifyCredential(
        VerifyCredentialInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        _logger.LogInformation("Verifying credential {CredentialId}", input.CredentialId);

        var verificationOptions = new Dictionary<string, object>
        {
            ["CheckExpiry"] = input.CheckExpiry,
            ["CheckRevocation"] = input.CheckRevocation,
            ["CheckSignature"] = input.CheckSignature,
            ["CheckSchema"] = input.CheckSchema ?? true,
            ["RequireTrustChain"] = input.RequireTrustChain ?? false
        };

        var command = new VerifyCredentialCommand
        {
            CredentialId = input.CredentialId,
            CredentialData = input.CredentialData,
            VerificationOptions = verificationOptions
        };

        var result = await _verifyCredentialHandler.HandleAsync(command);
        return result;
    }

    /// <summary>
    /// Revoke a credential
    /// </summary>
    [GraphQLDescription("Revoke an issued credential")]
    [HotChocolate.Authorization.Authorize(Roles = new[] { "Issuer", "Admin" })]
    public async Task<bool> RevokeCredential(
        RevokeCredentialInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogWarning("Revoking credential {CredentialId} for reason: {Reason}",
            input.CredentialId, input.Reason);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataDeletion,
                $"Credential revoked: {input.CredentialId}");
        }

        var command = new RevokeCredentialCommand(
            CredentialId: input.CredentialId,
            Reason: input.Reason,
            RevokerId: userId ?? "system");

        var result = await _revokeCredentialHandler.HandleAsync(command);
        return result;
    }

    /// <summary>
    /// Create an issuance request
    /// </summary>
    [GraphQLDescription("Create a new issuance request")]
    [HotChocolate.Authorization.Authorize]
    public async Task<IssuanceDto> CreateIssuance(
        CreateIssuanceInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Creating issuance request for {CredentialType}",
            input.CredentialType);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataModification,
                $"Issuance request created: {input.CredentialType}");
        }

        var additionalData = string.IsNullOrEmpty(input.AdditionalDataJson)
            ? new Dictionary<string, object>()
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(input.AdditionalDataJson)
              ?? new Dictionary<string, object>();

        var command = new CreateIssuanceCommand(
            CredentialType: input.CredentialType,
            WalletId: input.WalletId,
            RequesterId: userId ?? "system",
            Claims: additionalData,
            ExpiryDate: null,
            Metadata: input.RequiredDocuments != null
                ? new Dictionary<string, string> { ["required_documents"] = string.Join(",", input.RequiredDocuments) }
                : new Dictionary<string, string>());

        var issuance = await _createIssuanceHandler.HandleAsync(command);
        return issuance;
    }

    /// <summary>
    /// Approve an issuance request
    /// </summary>
    [GraphQLDescription("Approve a pending issuance request")]
    [HotChocolate.Authorization.Authorize(Roles = new[] { "Issuer", "Admin" })]
    public async Task<IssuanceDto> ApproveIssuance(
        ApproveIssuanceInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Approving issuance {IssuanceId}", input.IssuanceId);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataModification,
                $"Issuance approved: {input.IssuanceId}");
        }

        var command = new ApproveIssuanceCommand(
            IssuanceId: input.IssuanceId,
            ApprovedBy: userId ?? "system",
            Comments: input.Comments);

        var issuance = await _approveIssuanceHandler.HandleAsync(command);
        return issuance;
    }

    /// <summary>
    /// Reject an issuance request
    /// </summary>
    [GraphQLDescription("Reject a pending issuance request")]
    [HotChocolate.Authorization.Authorize(Roles = new[] { "Issuer", "Admin" })]
    public async Task<IssuanceDto> RejectIssuance(
        RejectIssuanceInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogWarning("Rejecting issuance {IssuanceId} for reason: {Reason}",
            input.IssuanceId, input.Reason);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataModification,
                $"Issuance rejected: {input.IssuanceId}");
        }

        var command = new RejectIssuanceCommand(
            IssuanceId: input.IssuanceId,
            RejectedBy: userId ?? "system",
            Reason: input.Reason,
            Comments: null);

        var issuance = await _rejectIssuanceHandler.HandleAsync(command);
        return issuance;
    }
}