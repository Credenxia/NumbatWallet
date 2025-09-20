using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using NumbatWallet.Web.Admin.Services;

namespace NumbatWallet.Web.Admin.Hubs;

[Authorize]
public class DashboardHub : Hub
{
    private readonly IDashboardService _dashboardService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(
        IDashboardService dashboardService,
        ITenantService tenantService,
        ILogger<DashboardHub> logger)
    {
        _dashboardService = dashboardService;
        _tenantService = tenantService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("User {UserIdentifier} connected to dashboard hub", Context.UserIdentifier);

        // Get user's tenant
        var tenant = await _tenantService.GetCurrentTenantAsync();
        if (tenant != null)
        {
            // Join tenant-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenant.Id}");
        }

        // If user is master admin, join master group
        if (Context.User?.IsInRole("MasterAdmin") ?? false)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "master-admins");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("User {UserIdentifier} disconnected from dashboard hub", Context.UserIdentifier);

        // Remove from all groups (happens automatically but good to be explicit)
        var tenant = await _tenantService.GetCurrentTenantAsync();
        if (tenant != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant-{tenant.Id}");
        }

        if (Context.User?.IsInRole("MasterAdmin") ?? false)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "master-admins");
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Hub methods that clients can call
    public async Task RefreshMetrics()
    {
        // Use the standard GetMetricsAsync method
        var metrics = await _dashboardService.GetMetricsAsync();

        await Clients.Caller.SendAsync("MetricsUpdated", metrics);
    }

    public async Task SubscribeToWallet(string walletId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"wallet-{walletId}");
        _logger.LogInformation("User {UserIdentifier} subscribed to wallet {WalletId}", Context.UserIdentifier, walletId);
    }

    public async Task UnsubscribeFromWallet(string walletId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"wallet-{walletId}");
        _logger.LogInformation("User {UserIdentifier} unsubscribed from wallet {WalletId}", Context.UserIdentifier, walletId);
    }

    public async Task SubscribeToCredential(string credentialId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"credential-{credentialId}");
        _logger.LogInformation("User {UserIdentifier} subscribed to credential {CredentialId}", Context.UserIdentifier, credentialId);
    }

    public async Task UnsubscribeFromCredential(string credentialId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"credential-{credentialId}");
        _logger.LogInformation("User {UserIdentifier} unsubscribed from credential {CredentialId}", Context.UserIdentifier, credentialId);
    }

    // Server-side methods to push updates to clients
    public static class Updates
    {
        public static async Task NotifyWalletCreated(IHubContext<DashboardHub> hubContext, string tenantId, object walletInfo)
        {
            await hubContext.Clients.Group($"tenant-{tenantId}").SendAsync("WalletCreated", walletInfo);
            await hubContext.Clients.Group("master-admins").SendAsync("WalletCreated", walletInfo);
        }

        public static async Task NotifyCredentialIssued(IHubContext<DashboardHub> hubContext, string tenantId, string walletId, object credentialInfo)
        {
            await hubContext.Clients.Group($"tenant-{tenantId}").SendAsync("CredentialIssued", credentialInfo);
            await hubContext.Clients.Group($"wallet-{walletId}").SendAsync("CredentialIssued", credentialInfo);
            await hubContext.Clients.Group("master-admins").SendAsync("CredentialIssued", credentialInfo);
        }

        public static async Task NotifyVerificationCompleted(IHubContext<DashboardHub> hubContext, string tenantId, string credentialId, object verificationInfo)
        {
            await hubContext.Clients.Group($"tenant-{tenantId}").SendAsync("VerificationCompleted", verificationInfo);
            await hubContext.Clients.Group($"credential-{credentialId}").SendAsync("VerificationCompleted", verificationInfo);
            await hubContext.Clients.Group("master-admins").SendAsync("VerificationCompleted", verificationInfo);
        }

        public static async Task NotifyMetricsUpdate(IHubContext<DashboardHub> hubContext, string? tenantId = null)
        {
            if (tenantId != null)
            {
                await hubContext.Clients.Group($"tenant-{tenantId}").SendAsync("RefreshMetrics");
            }
            else
            {
                await hubContext.Clients.Group("master-admins").SendAsync("RefreshMetrics");
            }
        }

        public static async Task NotifySystemAlert(IHubContext<DashboardHub> hubContext, string message, string severity)
        {
            var alert = new
            {
                Message = message,
                Severity = severity,
                Timestamp = DateTime.UtcNow
            };

            await hubContext.Clients.All.SendAsync("SystemAlert", alert);
        }
    }
}

// SignalR Service for dependency injection
public interface IRealtimeNotificationService
{
    Task NotifyWalletCreated(string tenantId, Guid walletId, string userId);
    Task NotifyCredentialIssued(string tenantId, Guid walletId, Guid credentialId, string credentialType);
    Task NotifyVerificationCompleted(string tenantId, Guid credentialId, string verifierId);
    Task RefreshDashboardMetrics(string? tenantId = null);
    Task SendSystemAlert(string message, AlertSeverity severity);
}

public class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<RealtimeNotificationService> _logger;

    public RealtimeNotificationService(
        IHubContext<DashboardHub> hubContext,
        ILogger<RealtimeNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyWalletCreated(string tenantId, Guid walletId, string userId)
    {
        _logger.LogInformation("Notifying wallet created: {WalletId} for user {UserId}", walletId, userId);

        var walletInfo = new
        {
            WalletId = walletId,
            UserId = userId,
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow
        };

        await DashboardHub.Updates.NotifyWalletCreated(_hubContext, tenantId, walletInfo);
    }

    public async Task NotifyCredentialIssued(string tenantId, Guid walletId, Guid credentialId, string credentialType)
    {
        _logger.LogInformation("Notifying credential issued: {CredentialId} of type {CredentialType}", credentialId, credentialType);

        var credentialInfo = new
        {
            CredentialId = credentialId,
            WalletId = walletId,
            TenantId = tenantId,
            Type = credentialType,
            Timestamp = DateTime.UtcNow
        };

        await DashboardHub.Updates.NotifyCredentialIssued(_hubContext, tenantId, walletId.ToString(), credentialInfo);
    }

    public async Task NotifyVerificationCompleted(string tenantId, Guid credentialId, string verifierId)
    {
        _logger.LogInformation("Notifying verification completed for credential: {CredentialId}", credentialId);

        var verificationInfo = new
        {
            CredentialId = credentialId,
            VerifierId = verifierId,
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow
        };

        await DashboardHub.Updates.NotifyVerificationCompleted(_hubContext, tenantId, credentialId.ToString(), verificationInfo);
    }

    public async Task RefreshDashboardMetrics(string? tenantId = null)
    {
        _logger.LogInformation("Refreshing dashboard metrics for tenant: {TenantId}", tenantId ?? "all");
        await DashboardHub.Updates.NotifyMetricsUpdate(_hubContext, tenantId);
    }

    public async Task SendSystemAlert(string message, AlertSeverity severity)
    {
        _logger.LogWarning("System alert ({Severity}): {Message}", severity, message);
        await DashboardHub.Updates.NotifySystemAlert(_hubContext, message, severity.ToString());
    }
}

public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}