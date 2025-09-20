using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.EventHandlers;

#pragma warning disable CA1711 // EventHandler naming is intentional for domain events
public class CredentialRevokedEventHandler : IDomainEventHandler<CredentialRevokedEvent>
{
    private readonly ILogger<CredentialRevokedEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;

    public CredentialRevokedEventHandler(
        ILogger<CredentialRevokedEventHandler> logger,
        INotificationService notificationService,
        IAuditService auditService,
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository)
    {
        _logger = logger;
        _notificationService = notificationService;
        _auditService = auditService;
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
    }

    public async Task HandleAsync(CredentialRevokedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling CredentialRevokedEvent for Credential {CredentialId}", domainEvent.CredentialId);

        // Get credential and wallet for notification
        var credential = await _credentialRepository.GetByIdAsync(domainEvent.CredentialId, cancellationToken);
        if (credential != null)
        {
            var wallet = await _walletRepository.GetByIdAsync(credential.WalletId, cancellationToken);
            if (wallet != null)
            {
                // Send urgent notification to wallet owner
                await _notificationService.SendUrgentNotificationAsync(
                    wallet.PersonId,
                    "Credential Revoked",
                    $"Your {credential.CredentialType} credential has been revoked. Reason: {domainEvent.Reason}",
                    cancellationToken);
            }

            // Log detailed audit entry
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "Credential",
                EntityId = domainEvent.CredentialId.ToString(),
                Action = "Revoked",
                UserId = "System",
                TenantId = Guid.Empty, // Will be set by tenant interceptor
                Timestamp = domainEvent.OccurredAt,
                MaxClassification = SharedKernel.Enums.DataClassification.Protected,
                ChangedFields = new Dictionary<string, object>
                {
                    ["CredentialId"] = domainEvent.CredentialId,
                    ["CredentialType"] = credential.CredentialType.ToString(),
                    ["Reason"] = domainEvent.Reason,
                    ["IssuerId"] = domainEvent.IssuerId,
                    ["RevokedAt"] = domainEvent.RevokedAt,
                    ["WalletId"] = credential.WalletId
                }
            };
            await _auditService.LogAccessAsync(auditEntry, cancellationToken);

            // Notify issuer organization about revocation
            if (credential.IssuerId != Guid.Empty)
            {
                await _notificationService.NotifyOrganizationAsync(
                    credential.IssuerId,
                    "Credential Revocation",
                    $"Credential {domainEvent.CredentialId} has been revoked. Reason: {domainEvent.Reason}",
                    cancellationToken);
            }
        }

        _logger.LogInformation("CredentialRevokedEvent handled successfully for Credential {CredentialId}", domainEvent.CredentialId);
    }
}
#pragma warning restore CA1711