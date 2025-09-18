using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Events;

// Audit and Compliance Events
public record DataAccessedEvent(
    string EntityType,
    Guid EntityId,
    string AccessedBy,
    string AccessReason,
    string[] AccessedFields,
    DateTimeOffset AccessedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public DataAccessedEvent(
        string entityType,
        Guid entityId,
        string accessedBy,
        string accessReason,
        string[] accessedFields,
        DateTimeOffset accessedAt)
        : this(entityType, entityId, accessedBy, accessReason, accessedFields, accessedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record DataModifiedEvent(
    string EntityType,
    Guid EntityId,
    string ModifiedBy,
    Dictionary<string, object?> OldValues,
    Dictionary<string, object?> NewValues,
    DateTimeOffset ModifiedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public DataModifiedEvent(
        string entityType,
        Guid entityId,
        string modifiedBy,
        Dictionary<string, object?> oldValues,
        Dictionary<string, object?> newValues,
        DateTimeOffset modifiedAt)
        : this(entityType, entityId, modifiedBy, oldValues, newValues, modifiedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record DataExportedEvent(
    Guid PersonId,
    string ExportFormat,
    string[] IncludedDataTypes,
    string ExportedBy,
    DateTimeOffset ExportedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public DataExportedEvent(
        Guid personId,
        string exportFormat,
        string[] includedDataTypes,
        string exportedBy,
        DateTimeOffset exportedAt)
        : this(personId, exportFormat, includedDataTypes, exportedBy, exportedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record DataDeletedEvent(
    string EntityType,
    Guid EntityId,
    string DeletedBy,
    bool HardDelete,
    DateTimeOffset DeletedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public DataDeletedEvent(
        string entityType,
        Guid entityId,
        string deletedBy,
        bool hardDelete,
        DateTimeOffset deletedAt)
        : this(entityType, entityId, deletedBy, hardDelete, deletedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record SecurityEventOccurredEvent(
    string EventType,
    string Severity,
    string Description,
    Dictionary<string, object> Context,
    DateTimeOffset SecurityOccurredAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public SecurityEventOccurredEvent(
        string eventType,
        string severity,
        string description,
        Dictionary<string, object> context,
        DateTimeOffset securityOccurredAt)
        : this(eventType, severity, description, context, securityOccurredAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record ComplianceCheckPerformedEvent(
    string CheckType,
    string CheckScope,
    bool Passed,
    string[] Findings,
    DateTimeOffset PerformedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public ComplianceCheckPerformedEvent(
        string checkType,
        string checkScope,
        bool passed,
        string[] findings,
        DateTimeOffset performedAt)
        : this(checkType, checkScope, passed, findings, performedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record PrivacyPolicyAcceptedEvent(
    Guid PersonId,
    string PolicyVersion,
    DateTimeOffset AcceptedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public PrivacyPolicyAcceptedEvent(
        Guid personId,
        string policyVersion,
        DateTimeOffset acceptedAt)
        : this(personId, policyVersion, acceptedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}

public record DataRetentionAppliedEvent(
    string EntityType,
    int RecordsProcessed,
    int RecordsDeleted,
    DateTimeOffset AppliedAt,
    Guid EventId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    public DataRetentionAppliedEvent(
        string entityType,
        int recordsProcessed,
        int recordsDeleted,
        DateTimeOffset appliedAt)
        : this(entityType, recordsProcessed, recordsDeleted, appliedAt, Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}