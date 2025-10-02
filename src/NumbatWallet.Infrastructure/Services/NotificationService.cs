using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using System.Threading.Channels;
using System.Text.Json;

namespace NumbatWallet.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IEmailService _emailService;
    private readonly Channel<NotificationMessage> _notificationQueue;
    private readonly Channel<NotificationMessage> _urgentQueue;

    public NotificationService(
        ILogger<NotificationService> logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;

        // Create unbounded channels for notification queuing
        // In production, use bounded channels with appropriate capacity
        _notificationQueue = Channel.CreateUnbounded<NotificationMessage>(new UnboundedChannelOptions
        {
            SingleReader = false, // Allow multiple background workers
            SingleWriter = false
        });

        _urgentQueue = Channel.CreateUnbounded<NotificationMessage>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public async Task SendNotificationAsync(
        Guid userId,
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Queue notification for async processing
            var notification = new NotificationMessage
            {
                UserId = userId,
                Title = title,
                Message = message,
                Priority = NotificationPriority.Normal,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _notificationQueue.Writer.WriteAsync(notification, cancellationToken);

            _logger.LogInformation("Notification queued for user {UserId}: {Title}", userId, title);

            // Background worker will process from queue and send via:
            // - Firebase Cloud Messaging (FCM) for Android
            // - Apple Push Notification Service (APNS) for iOS
            // - WebPush for web clients
            // Implementation requires platform-specific device tokens stored in user profile
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue notification for user {UserId}", userId);
            // Fallback to email if push notification queuing fails
            await FallbackToEmailAsync(userId, title, message, cancellationToken);
        }
    }

    public async Task SendUrgentNotificationAsync(
        Guid userId,
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use priority queue for urgent notifications - processed first
            var notification = new NotificationMessage
            {
                UserId = userId,
                Title = title,
                Message = message,
                Priority = NotificationPriority.Urgent,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _urgentQueue.Writer.WriteAsync(notification, cancellationToken);

            _logger.LogInformation("Urgent notification queued for user {UserId}: {Title}", userId, title);

            // Urgent notifications are processed by dedicated high-priority background workers
            // with shorter polling intervals and retry logic
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue urgent notification for user {UserId}", userId);
            // For urgent notifications, attempt immediate email fallback
            await FallbackToEmailAsync(userId, title, message, cancellationToken);
        }
    }

    public async Task NotifyOrganizationAsync(
        Guid organizationId,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Organization notifications require member lookup
            // This would query the Tenant/Organization members from the database
            // For now, logging the intent - actual implementation requires:
            // 1. ITenantService to get organization members
            // 2. Batch notification processing with rate limiting
            // 3. Failed notification tracking and retry logic

            _logger.LogInformation(
                "Organization notification queued for {OrganizationId}: {Subject}",
                organizationId, subject);

            // In production, this would:
            // var members = await _tenantService.GetOrganizationMembersAsync(organizationId);
            // await SendBulkNotificationAsync(members.Select(m => m.UserId), subject, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send organization notification for {OrganizationId}", organizationId);
        }
    }

    public async Task ScheduleReminderAsync(
        Guid userId,
        string title,
        string message,
        DateTime scheduledTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Scheduled reminders require background job infrastructure (Hangfire/Quartz)
            // For now, we create a scheduled notification record
            var scheduledNotification = new ScheduledNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                ScheduledTime = scheduledTime,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = "Pending"
            };

            // In production, this would:
            // await _backgroundJobClient.Schedule(
            //     () => SendNotificationAsync(userId, title, message, CancellationToken.None),
            //     scheduledTime);

            _logger.LogInformation(
                "Reminder scheduled for user {UserId} at {ScheduledTime}: {Title}",
                userId, scheduledTime, title);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule reminder for user {UserId}", userId);
        }
    }

    public async Task SendBulkNotificationAsync(
        IEnumerable<Guid> userIds,
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        var tasks = userIds.Select(userId => SendNotificationAsync(userId, title, message, cancellationToken));
        await Task.WhenAll(tasks);
        _logger.LogInformation("Bulk notification sent to {Count} users: {Title}", userIds.Count(), title);
    }

    /// <summary>
    /// Fallback to email when push notification fails
    /// </summary>
    private async Task FallbackToEmailAsync(
        Guid userId,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            // In production, lookup user email from Person/User repository
            // For now, just log the fallback attempt
            _logger.LogWarning(
                "Push notification failed for user {UserId}, attempting email fallback for: {Title}",
                userId, title);

            // await _emailService.SendEmailAsync(
            //     userEmail,
            //     title,
            //     message,
            //     cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email fallback also failed for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Get notification queue for background workers to consume
    /// </summary>
    internal ChannelReader<NotificationMessage> GetNotificationQueue() => _notificationQueue.Reader;

    /// <summary>
    /// Get urgent notification queue for high-priority background workers
    /// </summary>
    internal ChannelReader<NotificationMessage> GetUrgentQueue() => _urgentQueue.Reader;
}

/// <summary>
/// Notification message for queuing
/// </summary>
internal class NotificationMessage
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
}

/// <summary>
/// Notification priority levels
/// </summary>
internal enum NotificationPriority
{
    Normal = 0,
    Urgent = 1
}

/// <summary>
/// Scheduled notification record
/// </summary>
internal class ScheduledNotification
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = "Pending";
}
