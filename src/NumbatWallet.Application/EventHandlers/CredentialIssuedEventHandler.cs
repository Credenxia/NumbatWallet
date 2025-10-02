using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.EventHandlers;

#pragma warning disable CA1711 // EventHandler naming is intentional for domain events
public class CredentialIssuedEventHandler : IDomainEventHandler<CredentialIssuedEvent>
{
    private readonly ILogger<CredentialIssuedEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    // private readonly IStatisticsService _statisticsService; // TODO: Add tracking when IStatisticsService is extended
    private readonly IWalletRepository _walletRepository;

    public CredentialIssuedEventHandler(
        ILogger<CredentialIssuedEventHandler> logger,
        INotificationService notificationService,
        IAuditService auditService,
        // IStatisticsService statisticsService,
        IWalletRepository walletRepository)
    {
        _logger = logger;
        _notificationService = notificationService;
        _auditService = auditService;
        // _statisticsService = statisticsService;
        _walletRepository = walletRepository;
    }

    public async Task HandleAsync(CredentialIssuedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling CredentialIssuedEvent for Credential {CredentialId}", domainEvent.CredentialId);

        // Get wallet owner for notification
        var wallet = await _walletRepository.GetByIdAsync(domainEvent.WalletId, cancellationToken);
        if (wallet != null)
        {
            // Send notification to wallet owner
            await _notificationService.SendNotificationAsync(
                wallet.PersonId,
                "New Credential Issued",
                $"A new {domainEvent.CredentialType} credential has been issued to your wallet.",
                cancellationToken);

            // TODO: Update statistics when IStatisticsService is extended
            // await _statisticsService.IncrementCredentialIssuedCountAsync(
            //     domainEvent.CredentialType.ToString(),
            //     cancellationToken);

            // Log audit entry with detailed information
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "Credential",
                EntityId = domainEvent.CredentialId.ToString(),
                Action = "Issued",
                UserId = "System",
                TenantId = Guid.Empty, // Will be set by tenant interceptor
                Timestamp = domainEvent.OccurredAt,
                MaxClassification = SharedKernel.Enums.DataClassification.Protected,
                ChangedFields = new Dictionary<string, object>
                {
                    ["CredentialId"] = domainEvent.CredentialId,
                    ["WalletId"] = domainEvent.WalletId,
                    ["CredentialType"] = domainEvent.CredentialType,
                    ["IssuerId"] = domainEvent.IssuerId,
                    ["CredentialDid"] = domainEvent.CredentialDid,
                    ["IssuedAt"] = domainEvent.IssuedAt
                }
            };
            await _auditService.LogAccessAsync(auditEntry, cancellationToken);

            // Schedule expiry reminders if credential has expiration date
            if (domainEvent.ExpiresAt.HasValue)
            {
                _logger.LogInformation("Credential {CredentialId} expires at {ExpiresAt}, scheduling reminders",
                    domainEvent.CredentialId, domainEvent.ExpiresAt.Value);

                // Calculate reminder dates
                var expiryDate = domainEvent.ExpiresAt.Value;
                var thirtyDaysBeforeExpiry = expiryDate.AddDays(-30);
                var sevenDaysBeforeExpiry = expiryDate.AddDays(-7);
                var oneDayBeforeExpiry = expiryDate.AddDays(-1);

                // Schedule reminders at different intervals
                // NOTE: This requires a background job scheduler (Hangfire/Quartz)
                // For now, we log the scheduled reminder times
                _logger.LogInformation(
                    "Expiry reminders scheduled for Credential {CredentialId}: 30 days ({ThirtyDays}), 7 days ({SevenDays}), 1 day ({OneDay})",
                    domainEvent.CredentialId, thirtyDaysBeforeExpiry, sevenDaysBeforeExpiry, oneDayBeforeExpiry);

                // Background Job Integration (Hangfire/Quartz)
                // To enable scheduled reminders in production:
                // 1. Add Hangfire NuGet package: Hangfire.AspNetCore, Hangfire.PostgreSql
                // 2. Configure in DI: services.AddHangfire(config => config.UsePostgreSqlStorage(connectionString))
                // 3. Enable dashboard: app.UseHangfireDashboard()
                // 4. Start server: app.UseHangfireServer()
                // 5. Schedule jobs using IBackgroundJobClient:
                //    _backgroundJobClient.Schedule(() => SendExpiryReminderAsync(domainEvent.CredentialId, 30), thirtyDaysBeforeExpiry);
                //    _backgroundJobClient.Schedule(() => SendExpiryReminderAsync(domainEvent.CredentialId, 7), sevenDaysBeforeExpiry);
                //    _backgroundJobClient.Schedule(() => SendExpiryReminderAsync(domainEvent.CredentialId, 1), oneDayBeforeExpiry);
            }
        }

        _logger.LogInformation("CredentialIssuedEvent handled successfully for Credential {CredentialId}", domainEvent.CredentialId);
    }
}
#pragma warning restore CA1711
