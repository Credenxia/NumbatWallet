using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.EventHandlers;

#pragma warning disable CA1711 // EventHandler naming is intentional for domain events
public class WalletCreatedEventHandler : IDomainEventHandler<WalletCreatedEvent>
{
    private readonly ILogger<WalletCreatedEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly IWalletRepository _walletRepository;

    public WalletCreatedEventHandler(
        ILogger<WalletCreatedEventHandler> logger,
        INotificationService notificationService,
        IAuditService auditService,
        IWalletRepository walletRepository)
    {
        _logger = logger;
        _notificationService = notificationService;
        _auditService = auditService;
        _walletRepository = walletRepository;
    }

    public async Task HandleAsync(WalletCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling WalletCreatedEvent for Wallet {WalletId}", domainEvent.WalletId);

        // Initialize wallet with default settings
        var wallet = await _walletRepository.GetByIdAsync(domainEvent.WalletId, cancellationToken);
        if (wallet != null)
        {
            // Send notification to user
            await _notificationService.SendNotificationAsync(
                domainEvent.PersonId,
                "Wallet Created",
                $"Your digital wallet has been created successfully.",
                cancellationToken);

            // Log audit entry
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "Wallet",
                EntityId = domainEvent.WalletId.ToString(),
                Action = "Created",
                UserId = "System",
                TenantId = Guid.Parse(domainEvent.TenantId),
                Timestamp = domainEvent.OccurredAt,
                MaxClassification = SharedKernel.Enums.DataClassification.Protected,
                ChangedFields = new Dictionary<string, object>
                {
                    ["WalletId"] = domainEvent.WalletId,
                    ["PersonId"] = domainEvent.PersonId,
                    ["WalletDid"] = domainEvent.WalletDid,
                    ["CreatedAt"] = domainEvent.CreatedAt
                }
            };
            await _auditService.LogAccessAsync(auditEntry, cancellationToken);
        }

        _logger.LogInformation("WalletCreatedEvent handled successfully for Wallet {WalletId}", domainEvent.WalletId);
    }
}
#pragma warning restore CA1711