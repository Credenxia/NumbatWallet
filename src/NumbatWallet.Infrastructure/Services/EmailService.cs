using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace NumbatWallet.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public EmailService(
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _smtpHost = configuration["Email:SmtpHost"] ?? "localhost";
        _smtpPort = configuration.GetValue("Email:SmtpPort", 587);
        _smtpUsername = configuration["Email:SmtpUsername"] ?? "";
        _smtpPassword = configuration["Email:SmtpPassword"] ?? "";
        _fromAddress = configuration["Email:FromAddress"] ?? "noreply@numbatwallet.gov.au";
        _fromName = configuration["Email:FromName"] ?? "NumbatWallet";
    }

    public async Task SendEmailAsync(
        string recipient,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_fromAddress, _fromName);
            message.To.Add(new MailAddress(recipient));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;

            using var client = new SmtpClient(_smtpHost, _smtpPort);
            if (!string.IsNullOrEmpty(_smtpUsername))
            {
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
            }
            client.EnableSsl = _smtpPort != 25;

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(
        string recipient,
        string firstName,
        CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to NumbatWallet";
        var body = GenerateWelcomeEmailHtml(firstName);
        await SendEmailAsync(recipient, subject, body, true, cancellationToken);
    }

    public async Task SendCredentialIssuedEmailAsync(
        string recipient,
        string credentialType,
        DateTime? expiryDate,
        CancellationToken cancellationToken = default)
    {
        var subject = $"New {credentialType} Credential Issued";
        var body = GenerateCredentialIssuedEmailHtml(credentialType, expiryDate);
        await SendEmailAsync(recipient, subject, body, true, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(
        string recipient,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var subject = "Password Reset Request";
        var body = GeneratePasswordResetEmailHtml(resetToken);
        await SendEmailAsync(recipient, subject, body, true, cancellationToken);
    }

    public async Task SendBulkEmailAsync(
        IEnumerable<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var tasks = recipients.Select(recipient =>
            SendEmailAsync(recipient, subject, body, true, cancellationToken));

        await Task.WhenAll(tasks);
        _logger.LogInformation("Bulk email sent to {Count} recipients", recipients.Count());
    }

    private string GenerateWelcomeEmailHtml(string firstName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head><style>body { font-family: Arial, sans-serif; }</style></head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h2>Welcome to NumbatWallet, {firstName}!</h2>");
        sb.AppendLine("<p>Thank you for joining NumbatWallet, Western Australia's trusted digital identity wallet.</p>");
        sb.AppendLine("<p>You can now:</p>");
        sb.AppendLine("<ul>");
        sb.AppendLine("<li>Store your digital credentials securely</li>");
        sb.AppendLine("<li>Share your identity with trusted services</li>");
        sb.AppendLine("<li>Manage your digital documents in one place</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("<p>If you have any questions, please contact our support team.</p>");
        sb.AppendLine("<p>Best regards,<br>The NumbatWallet Team</p>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private string GenerateCredentialIssuedEmailHtml(string credentialType, DateTime? expiryDate)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head><style>body { font-family: Arial, sans-serif; }</style></head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h2>New {credentialType} Credential Issued</h2>");
        sb.AppendLine($"<p>A new {credentialType} credential has been issued to your NumbatWallet.</p>");
        if (expiryDate.HasValue)
        {
            sb.AppendLine($"<p><strong>Expiry Date:</strong> {expiryDate.Value:yyyy-MM-dd}</p>");
        }
        sb.AppendLine("<p>You can view and manage your credentials by logging into your NumbatWallet account.</p>");
        sb.AppendLine("<p>Best regards,<br>The NumbatWallet Team</p>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private string GeneratePasswordResetEmailHtml(string resetToken)
    {
        var resetUrl = $"{_configuration["Application:BaseUrl"]}/reset-password?token={resetToken}";
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head><style>body { font-family: Arial, sans-serif; }</style></head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<h2>Password Reset Request</h2>");
        sb.AppendLine("<p>We received a request to reset your NumbatWallet password.</p>");
        sb.AppendLine($"<p>Click the link below to reset your password:</p>");
        sb.AppendLine($"<p><a href=\"{resetUrl}\">Reset Password</a></p>");
        sb.AppendLine("<p>If you didn't request this, please ignore this email.</p>");
        sb.AppendLine("<p>This link will expire in 24 hours.</p>");
        sb.AppendLine("<p>Best regards,<br>The NumbatWallet Team</p>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }
}