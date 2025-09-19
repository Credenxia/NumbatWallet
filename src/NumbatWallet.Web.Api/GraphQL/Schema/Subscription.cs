using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Events;
using System.Runtime.CompilerServices;

namespace NumbatWallet.Web.Api.GraphQL.Schema;

public class Subscription
{
    // Credential Subscriptions
    [Authorize]
    [Subscribe]
    [Topic("credential-issued-{walletId}")]
    public CredentialDto OnCredentialIssued(
        [EventMessage] CredentialDto credential,
        Guid walletId)
    {
        return credential;
    }

    [Authorize]
    [Subscribe]
    [Topic("credential-revoked-{walletId}")]
    public CredentialRevokedEvent OnCredentialRevoked(
        [EventMessage] CredentialRevokedEvent revokedEvent,
        Guid walletId)
    {
        return revokedEvent;
    }

    [Authorize]
    [Subscribe]
    [Topic("credential-expired-{walletId}")]
    public CredentialExpiredEvent OnCredentialExpired(
        [EventMessage] CredentialExpiredEvent expiredEvent,
        Guid walletId)
    {
        return expiredEvent;
    }

    // Wallet Subscriptions
    [Authorize]
    [Subscribe]
    [Topic("wallet-updated-{walletId}")]
    public WalletDto OnWalletUpdated(
        [EventMessage] WalletDto wallet,
        Guid walletId)
    {
        return wallet;
    }

    [Authorize]
    [Subscribe]
    [Topic("wallet-backed-up-{walletId}")]
    public WalletBackupEvent OnWalletBackedUp(
        [EventMessage] WalletBackupEvent backupEvent,
        Guid walletId)
    {
        return backupEvent;
    }

    // System-wide Subscriptions (Admin only)
    [Authorize(Roles = new[] { "Admin", "Officer" })]
    [Subscribe]
    [Topic("system-health-status")]
    public SystemHealthUpdate OnSystemHealthUpdate(
        [EventMessage] SystemHealthUpdate healthUpdate)
    {
        return healthUpdate;
    }

    [Authorize(Roles = new[] { "Admin", "Officer" })]
    [Subscribe]
    [Topic("issuance-statistics")]
    public IssuanceStatisticsUpdate OnIssuanceStatisticsUpdate(
        [EventMessage] IssuanceStatisticsUpdate statisticsUpdate)
    {
        return statisticsUpdate;
    }

    [Authorize(Roles = new[] { "Admin" })]
    [Subscribe]
    [Topic("security-alert")]
    public SecurityAlert OnSecurityAlert(
        [EventMessage] SecurityAlert alert)
    {
        return alert;
    }

    // Person/Organization Subscriptions
    [Authorize(Roles = new[] { "Admin", "Officer" })]
    [Subscribe]
    [Topic("person-created")]
    public PersonDto OnPersonCreated(
        [EventMessage] PersonDto person)
    {
        return person;
    }

    [Authorize(Roles = new[] { "Admin", "Officer" })]
    [Subscribe]
    [Topic("organization-updated")]
    public OrganizationDto OnOrganizationUpdated(
        [EventMessage] OrganizationDto organization)
    {
        return organization;
    }

    // User-specific Subscriptions
    [Authorize]
    [Subscribe]
    public async IAsyncEnumerable<NotificationEvent> OnUserNotification(
        [Service] ITopicEventReceiver eventReceiver,
        [Service] IHttpContextAccessor httpContextAccessor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            yield break;
        }

        var stream = await eventReceiver.SubscribeAsync<NotificationEvent>(
            $"user-notification-{userId}",
            cancellationToken);

        await foreach (var notification in stream.ReadEventsAsync().WithCancellation(cancellationToken))
        {
            yield return notification;
        }
    }

    // Bulk Operation Subscriptions
    [Authorize(Roles = new[] { "Admin", "Officer" })]
    [Subscribe]
    [Topic("bulk-operation-progress")]
    public BulkOperationProgress OnBulkOperationProgress(
        [EventMessage] BulkOperationProgress progress)
    {
        return progress;
    }
}

// Event Types
public class CredentialRevokedEvent
{
    public Guid CredentialId { get; set; }
    public Guid WalletId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime RevokedAt { get; set; }
    public string RevokedBy { get; set; } = string.Empty;
}

public class CredentialExpiredEvent
{
    public Guid CredentialId { get; set; }
    public Guid WalletId { get; set; }
    public string CredentialType { get; set; } = string.Empty;
    public DateTime ExpiredAt { get; set; }
}

public class WalletBackupEvent
{
    public Guid WalletId { get; set; }
    public DateTime BackedUpAt { get; set; }
    public string BackupLocation { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
}

public class SystemHealthUpdate
{
    public string Status { get; set; } = "Healthy";
    public Dictionary<string, ComponentStatus> Components { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double SystemLoad { get; set; }
    public long MemoryUsageMB { get; set; }
}

public class ComponentStatus
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Healthy";
    public TimeSpan ResponseTime { get; set; }
    public string? Message { get; set; }
}

public class IssuanceStatisticsUpdate
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int IssuedInLastHour { get; set; }
    public int RevokedInLastHour { get; set; }
    public int ActiveCredentials { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
}

public class SecurityAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Severity { get; set; } = "Info"; // Info, Warning, Critical
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? AffectedResource { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Details { get; set; } = new();
}

public class NotificationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public class BulkOperationProgress
{
    public Guid OperationId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double PercentComplete => TotalItems > 0 ? (ProcessedItems * 100.0 / TotalItems) : 0;
    public string Status { get; set; } = "InProgress"; // InProgress, Completed, Failed, Cancelled
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> Errors { get; set; } = new();
}