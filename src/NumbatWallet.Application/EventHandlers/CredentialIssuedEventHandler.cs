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

            // TODO: Schedule expiry reminders when we have expiry dates in the events
        }

        _logger.LogInformation("CredentialIssuedEvent handled successfully for Credential {CredentialId}", domainEvent.CredentialId);
    }
}
#pragma warning restore CA1711