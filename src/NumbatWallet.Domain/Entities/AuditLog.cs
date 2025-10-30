using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Domain.Entities;

/// <summary>
/// Audit log entity for tracking all system operations
/// Supports comprehensive audit trails for compliance and security
/// </summary>
public class AuditLog
{
    private AuditLog() { } // For EF Core

    public AuditLog(
        string userId,
        string action,
        string entityType,
        string entityId,
        Guid tenantId,
        AuditEventType eventType)
    {
        Id = Guid.NewGuid();
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Action = action ?? throw new ArgumentNullException(nameof(action));
        EntityType = entityType;
        EntityId = entityId;
        TenantId = tenantId;
        EventType = eventType;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public Guid TenantId { get; private set; }
    public AuditEventType EventType { get; private set; }
    public DataClassification MaxClassification { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Sets the old and new values for change tracking
    /// </summary>
    public void SetChangeValues(string? oldValues, string? newValues)
    {
        OldValues = oldValues;
        NewValues = newValues;
    }

    /// <summary>
    /// Sets the IP address and user agent from the request
    /// </summary>
    public void SetRequestContext(string? ipAddress, string? userAgent)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>
    /// Sets the maximum data classification accessed in this operation
    /// </summary>
    public void SetMaxClassification(DataClassification classification)
    {
        MaxClassification = classification;
    }
}