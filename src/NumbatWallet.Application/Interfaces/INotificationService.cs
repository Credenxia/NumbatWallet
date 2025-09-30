namespace NumbatWallet.Application.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
    Task SendUrgentNotificationAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
    Task NotifyOrganizationAsync(Guid organizationId, string subject, string message, CancellationToken cancellationToken = default);
    Task ScheduleReminderAsync(Guid userId, string title, string message, DateTime scheduledTime, CancellationToken cancellationToken = default);
    Task SendBulkNotificationAsync(IEnumerable<Guid> userIds, string title, string message, CancellationToken cancellationToken = default);
}

public interface IEmailService
{
    Task SendEmailAsync(string recipient, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string recipient, string firstName, CancellationToken cancellationToken = default);
    Task SendCredentialIssuedEmailAsync(string recipient, string credentialType, DateTime? expiryDate, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string recipient, string resetToken, CancellationToken cancellationToken = default);
    Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken = default);
}

