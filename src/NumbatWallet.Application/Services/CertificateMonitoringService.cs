using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Services;

/// <summary>
/// Background service that monitors certificate expiration and sends alerts
/// </summary>
public class CertificateMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CertificateMonitoringService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); // Check every 6 hours
    private readonly int[] _alertDays = { 90, 60, 30, 14, 7, 3, 1 }; // Days before expiry to alert

    public CertificateMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<CertificateMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Certificate Monitoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckCertificateExpirationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while monitoring certificates");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Certificate Monitoring Service stopped");
    }

    private async Task CheckCertificateExpirationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var certificateRepository = scope.ServiceProvider.GetRequiredService<ITenantCertificateRepository>();
        var notificationService = scope.ServiceProvider.GetService<INotificationService>();

        _logger.LogDebug("Checking for expiring certificates");

        foreach (var days in _alertDays)
        {
            var expiringCerts = await certificateRepository.GetExpiringCertificatesAsync(
                days,
                cancellationToken);

            foreach (var cert in expiringCerts)
            {
                // Check if we should send an alert for this certificate
                if (ShouldSendAlert(cert, days))
                {
                    await SendExpirationAlertAsync(cert, days, notificationService);

                    _logger.LogWarning(
                        "Certificate {CertificateId} (Thumbprint: {Thumbprint}) expires in {Days} days",
                        cert.Id, cert.Thumbprint, days);
                }
            }
        }

        _logger.LogDebug("Certificate expiration check completed");
    }

    private bool ShouldSendAlert(Domain.Entities.TenantCertificate certificate, int daysBeforeExpiry)
    {
        // Calculate when the alert should be sent
        var alertDate = certificate.ValidTo.AddDays(-daysBeforeExpiry);
        var now = DateTimeOffset.UtcNow;

        // Send alert if we're within 24 hours of the alert date
        // This prevents duplicate alerts while ensuring we don't miss any
        return now >= alertDate && now < alertDate.AddHours(24);
    }

    private async Task SendExpirationAlertAsync(
        Domain.Entities.TenantCertificate certificate,
        int daysBeforeExpiry,
        INotificationService? notificationService)
    {
        if (notificationService == null)
        {
            _logger.LogWarning(
                "Notification service not configured. Cannot send alert for certificate {CertificateId}",
                certificate.Id);
            return;
        }

        var alertLevel = GetAlertLevel(daysBeforeExpiry);
        var message = $"Certificate with thumbprint {certificate.Thumbprint} " +
                     $"expires on {certificate.ValidTo:yyyy-MM-dd}. " +
                     $"Only {daysBeforeExpiry} days remaining.";

        var notification = new CertificateExpirationNotification
        {
            CertificateId = certificate.Id,
            TenantId = certificate.TenantId,
            Thumbprint = certificate.Thumbprint,
            SubjectDn = certificate.SubjectDn,
            ExpiryDate = certificate.ValidTo,
            DaysUntilExpiry = daysBeforeExpiry,
            AlertLevel = alertLevel,
            Message = message
        };

        await notificationService.SendCertificateExpirationAlertAsync(notification);
    }

    private CertificateAlertLevel GetAlertLevel(int daysBeforeExpiry)
    {
        return daysBeforeExpiry switch
        {
            <= 3 => CertificateAlertLevel.Critical,
            <= 7 => CertificateAlertLevel.High,
            <= 14 => CertificateAlertLevel.Medium,
            <= 30 => CertificateAlertLevel.Low,
            _ => CertificateAlertLevel.Info
        };
    }
}

public class CertificateExpirationNotification
{
    public Guid CertificateId { get; init; }
    public Guid TenantId { get; init; }
    public string Thumbprint { get; init; } = string.Empty;
    public string SubjectDn { get; init; } = string.Empty;
    public DateTimeOffset ExpiryDate { get; init; }
    public int DaysUntilExpiry { get; init; }
    public CertificateAlertLevel AlertLevel { get; init; }
    public string Message { get; init; } = string.Empty;
}

public enum CertificateAlertLevel
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Interface for notification service
/// </summary>
public interface INotificationService
{
    Task SendCertificateExpirationAlertAsync(CertificateExpirationNotification notification);
    Task SendCertificateRenewalNotificationAsync(Guid certificateId, string message);
    Task SendSecurityAlertAsync(string alertType, string message, string severity);
}