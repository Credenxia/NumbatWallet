using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// Log-only email sender used in Development/Testing when no SMTP host is configured.
/// Lets email-dependent flows (e.g. shareCredential) complete locally without a mail
/// server; the message content is written to the log instead of being sent.
/// </summary>
public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string recipient, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DEV email (not sent — no SMTP configured): To={Recipient} Subject={Subject} BodyLength={BodyLength}",
            recipient, subject, body?.Length ?? 0);
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string recipient, string firstName, CancellationToken cancellationToken = default)
        => SendEmailAsync(recipient, "Welcome to NumbatWallet", $"Welcome {firstName}", true, cancellationToken);

    public Task SendCredentialIssuedEmailAsync(string recipient, string credentialType, DateTime? expiryDate, CancellationToken cancellationToken = default)
        => SendEmailAsync(recipient, $"New {credentialType} Credential Issued", $"Expires: {expiryDate?.ToString("yyyy-MM-dd") ?? "never"}", true, cancellationToken);

    public Task SendPasswordResetEmailAsync(string recipient, string resetToken, CancellationToken cancellationToken = default)
        => SendEmailAsync(recipient, "Password Reset Request", "Reset token issued (see application logs)", true, cancellationToken);

    public async Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken = default)
    {
        foreach (var recipient in recipients)
        {
            await SendEmailAsync(recipient, subject, body, true, cancellationToken);
        }
    }
}
