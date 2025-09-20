using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Events;

namespace NumbatWallet.Application.EventHandlers;

#pragma warning disable CA1711 // EventHandler naming is intentional for domain events
public class PersonCreatedEventHandler : IDomainEventHandler<PersonCreatedEvent>
{
    private readonly ILogger<PersonCreatedEventHandler> _logger;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;

    public PersonCreatedEventHandler(
        ILogger<PersonCreatedEventHandler> logger,
        IEmailService emailService,
        IAuditService auditService)
    {
        _logger = logger;
        _emailService = emailService;
        _auditService = auditService;
    }

    public async Task HandleAsync(PersonCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling PersonCreatedEvent for Person {PersonId}", domainEvent.PersonId);

        // Send welcome email
        if (!string.IsNullOrEmpty(domainEvent.Email))
        {
            try
            {
                await _emailService.SendWelcomeEmailAsync(
                    domainEvent.Email,
                    "User", // Use generic salutation since we don't have FirstName in event
                    cancellationToken);

                _logger.LogInformation("Welcome email sent to {Email}", domainEvent.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", domainEvent.Email);
            }
        }

        // Log audit entry
        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Person",
            EntityId = domainEvent.PersonId.ToString(),
            Action = "Created",
            UserId = "System",
            TenantId = Guid.Empty, // Will be set by tenant interceptor
            Timestamp = domainEvent.OccurredAt,
            MaxClassification = SharedKernel.Enums.DataClassification.Protected,
            ChangedFields = new Dictionary<string, object>
            {
                ["ExternalId"] = domainEvent.ExternalId,
                ["Email"] = domainEvent.Email,
                ["CreatedAt"] = domainEvent.CreatedAt
            }
        };
        await _auditService.LogAccessAsync(auditEntry, cancellationToken);

        _logger.LogInformation("PersonCreatedEvent handled successfully for Person {PersonId}", domainEvent.PersonId);
    }
}
#pragma warning restore CA1711