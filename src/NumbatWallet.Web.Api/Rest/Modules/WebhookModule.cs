using Carter;
using Microsoft.AspNetCore.Mvc;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Web.Api.Rest.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NumbatWallet.Web.Api.Rest.Modules;

/// <summary>
/// External webhook handlers for notifications and callbacks
/// </summary>
public class WebhookModule : RestEndpointBase
{
    public override string RoutePrefix => "/webhooks";

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix)
            .WithTags("Webhooks")
            .AllowAnonymous(); // Webhooks use signature verification instead

        // ServiceWA callback endpoint
        group.MapPost("/servicewa/callback", HandleServiceWACallback)
            .WithName("ServiceWACallback")
            .WithOpenApi()
            .Accepts<ServiceWACallbackRequest>("application/json")
            .Produces(200)
            .Produces(401);

        // Issuer notification endpoint
        group.MapPost("/issuer/notification", HandleIssuerNotification)
            .WithName("IssuerNotification")
            .WithOpenApi()
            .Accepts<IssuerNotificationRequest>("application/json")
            .Produces(200)
            .Produces(401);

        // Credential status update webhook
        group.MapPost("/credential/status-update", HandleCredentialStatusUpdate)
            .WithName("CredentialStatusUpdate")
            .WithOpenApi()
            .Accepts<CredentialStatusUpdateRequest>("application/json")
            .Produces(200)
            .Produces(401);

        // Verification result callback
        group.MapPost("/verification/callback", HandleVerificationCallback)
            .WithName("VerificationCallback")
            .WithOpenApi()
            .Accepts<VerificationCallbackRequest>("application/json")
            .Produces(200)
            .Produces(401);
    }

    private static async Task<IResult> HandleServiceWACallback(
        ServiceWACallbackRequest request,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IWalletService walletService,
        ILogger<WebhookModule> logger,
        CancellationToken cancellationToken)
    {
        // Verify webhook signature
        if (!VerifyWebhookSignature(httpContextAccessor.HttpContext!, configuration, "ServiceWA"))
        {
            logger.LogWarning("Invalid ServiceWA webhook signature");
            return Results.Unauthorized();
        }

        try
        {
            logger.LogInformation("Processing ServiceWA callback for user {UserId}", request.UserId);

            // Process the callback based on the event type
            switch (request.EventType)
            {
                case "USER_VERIFIED":
                    // Handle user verification completion
                    await HandleUserVerified(request, walletService, cancellationToken);
                    break;

                case "IDENTITY_UPDATED":
                    // Handle identity attribute updates
                    await HandleIdentityUpdated(request, walletService, cancellationToken);
                    break;

                case "SESSION_EXPIRED":
                    // Handle session expiration
                    await HandleSessionExpired(request, walletService, cancellationToken);
                    break;

                default:
                    logger.LogWarning("Unknown ServiceWA event type: {EventType}", request.EventType);
                    break;
            }

            return Results.Ok(new { success = true, processed = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing ServiceWA callback");
            return HandleError($"Failed to process callback: {ex.Message}", 500);
        }
    }

    private static async Task<IResult> HandleIssuerNotification(
        IssuerNotificationRequest request,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ICredentialService credentialService,
        ILogger<WebhookModule> logger,
        CancellationToken cancellationToken)
    {
        // Verify webhook signature
        if (!VerifyWebhookSignature(httpContextAccessor.HttpContext!, configuration, "Issuer"))
        {
            logger.LogWarning("Invalid Issuer webhook signature");
            return Results.Unauthorized();
        }

        try
        {
            logger.LogInformation("Processing Issuer notification for credential {CredentialId}", request.CredentialId);

            switch (request.NotificationType)
            {
                case "CREDENTIAL_ISSUED":
                    // Notify user about new credential
                    await NotifyCredentialIssued(request, credentialService, cancellationToken);
                    break;

                case "CREDENTIAL_REVOKED":
                    // Handle credential revocation
                    await NotifyCredentialRevoked(request, credentialService, cancellationToken);
                    break;

                case "CREDENTIAL_EXPIRING":
                    // Send expiration warning
                    await NotifyCredentialExpiring(request, credentialService, cancellationToken);
                    break;

                default:
                    logger.LogWarning("Unknown Issuer notification type: {NotificationType}", request.NotificationType);
                    break;
            }

            return Results.Ok(new { success = true, processed = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Issuer notification");
            return HandleError($"Failed to process notification: {ex.Message}", 500);
        }
    }

    private static async Task<IResult> HandleCredentialStatusUpdate(
        CredentialStatusUpdateRequest request,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ICredentialService credentialService,
        ILogger<WebhookModule> logger,
        CancellationToken cancellationToken)
    {
        // Verify webhook signature
        if (!VerifyWebhookSignature(httpContextAccessor.HttpContext!, configuration, "StatusUpdate"))
        {
            logger.LogWarning("Invalid status update webhook signature");
            return Results.Unauthorized();
        }

        try
        {
            logger.LogInformation("Processing status update for credential {CredentialId}", request.CredentialId);

            if (!Guid.TryParse(request.CredentialId, out var credentialId))
            {
                return HandleError("Invalid credential ID format", 400);
            }

            // Update credential status
            await credentialService.UpdateStatusAsync(
                credentialId,
                request.NewStatus,
                request.Reason,
                cancellationToken);

            return Results.Ok(new
            {
                success = true,
                credentialId = request.CredentialId,
                status = request.NewStatus,
                updatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing credential status update");
            return HandleError($"Failed to update status: {ex.Message}", 500);
        }
    }

    private static async Task<IResult> HandleVerificationCallback(
        VerificationCallbackRequest request,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ICredentialService credentialService,
        ILogger<WebhookModule> logger,
        CancellationToken cancellationToken)
    {
        // Verify webhook signature
        if (!VerifyWebhookSignature(httpContextAccessor.HttpContext!, configuration, "Verification"))
        {
            logger.LogWarning("Invalid verification webhook signature");
            return Results.Unauthorized();
        }

        try
        {
            logger.LogInformation("Processing verification callback for session {SessionId}", request.SessionId);

            // Store verification result
            await StoreVerificationResult(request, credentialService, cancellationToken);

            return Results.Ok(new
            {
                success = true,
                sessionId = request.SessionId,
                processedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing verification callback");
            return HandleError($"Failed to process verification: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Verify webhook signature using HMAC-SHA256
    /// </summary>
    private static bool VerifyWebhookSignature(HttpContext context, IConfiguration configuration, string webhookType)
    {
        var signature = context.Request.Headers["X-Webhook-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(signature))
        {
            return false;
        }

        var secret = configuration[$"Webhooks:{webhookType}:Secret"];
        if (string.IsNullOrEmpty(secret))
        {
            return false;
        }

        // Read request body
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = reader.ReadToEndAsync().GetAwaiter().GetResult();
        context.Request.Body.Position = 0;

        // Calculate expected signature
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var expectedSignature = Convert.ToBase64String(computedHash);

        return signature == expectedSignature;
    }

    // Helper methods
    private static async Task HandleUserVerified(ServiceWACallbackRequest request, IWalletService walletService, CancellationToken cancellationToken)
    {
        // Implementation would update user verification status
        await Task.CompletedTask;
    }

    private static async Task HandleIdentityUpdated(ServiceWACallbackRequest request, IWalletService walletService, CancellationToken cancellationToken)
    {
        // Implementation would update user identity attributes
        await Task.CompletedTask;
    }

    private static async Task HandleSessionExpired(ServiceWACallbackRequest request, IWalletService walletService, CancellationToken cancellationToken)
    {
        // Implementation would handle session cleanup
        await Task.CompletedTask;
    }

    private static async Task NotifyCredentialIssued(IssuerNotificationRequest request, ICredentialService credentialService, CancellationToken cancellationToken)
    {
        // Implementation would send notification to user
        await Task.CompletedTask;
    }

    private static async Task NotifyCredentialRevoked(IssuerNotificationRequest request, ICredentialService credentialService, CancellationToken cancellationToken)
    {
        // Implementation would handle revocation notification
        await Task.CompletedTask;
    }

    private static async Task NotifyCredentialExpiring(IssuerNotificationRequest request, ICredentialService credentialService, CancellationToken cancellationToken)
    {
        // Implementation would send expiration warning
        await Task.CompletedTask;
    }

    private static async Task StoreVerificationResult(VerificationCallbackRequest request, ICredentialService credentialService, CancellationToken cancellationToken)
    {
        // Implementation would store verification result
        await Task.CompletedTask;
    }
}

// Webhook request DTOs
public record ServiceWACallbackRequest(
    string UserId,
    string EventType,
    Dictionary<string, object> EventData,
    DateTime Timestamp);

public record IssuerNotificationRequest(
    string CredentialId,
    string NotificationType,
    string IssuerId,
    Dictionary<string, object> Details,
    DateTime Timestamp);

public record CredentialStatusUpdateRequest(
    string CredentialId,
    string NewStatus,
    string Reason,
    string UpdatedBy,
    DateTime Timestamp);

public record VerificationCallbackRequest(
    string SessionId,
    string CredentialId,
    bool IsValid,
    Dictionary<string, object> VerificationResult,
    DateTime Timestamp);