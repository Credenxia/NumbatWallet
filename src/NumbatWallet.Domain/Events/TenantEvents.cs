using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Events;

public record TenantCreatedEvent(
    string TenantId,
    string Name,
    string Description,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public TenantCreatedEvent(
        string tenantId,
        string name,
        string description,
        string createdBy,
        DateTimeOffset createdAt)
        : this(tenantId, name, description, createdBy, createdAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record TenantDeletedEvent(
    string TenantId,
    string DeletedBy,
    string Reason,
    DateTimeOffset DeletedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public TenantDeletedEvent(
        string tenantId,
        string deletedBy,
        string reason,
        DateTimeOffset deletedAt)
        : this(tenantId, deletedBy, reason, deletedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record TenantUpdatedEvent(
    string TenantId,
    string Name,
    string Description,
    string UpdatedBy,
    DateTimeOffset UpdatedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public TenantUpdatedEvent(
        string tenantId,
        string name,
        string description,
        string updatedBy,
        DateTimeOffset updatedAt)
        : this(tenantId, name, description, updatedBy, updatedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}