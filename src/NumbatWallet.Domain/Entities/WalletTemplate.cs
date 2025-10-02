using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Guards;

namespace NumbatWallet.Domain.Entities;

/// <summary>
/// Represents a wallet template that defines the structure and fields for digital wallets
/// </summary>
public class WalletTemplate : Entity<Guid>
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public WalletTemplateType Type { get; private set; }
    public string Version { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public string? CreatedBy { get; private set; }

    // EF Core-compatible collections with init setters (allows deserialization)
    public List<WalletField> Fields { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
    public List<string> SupportedCredentialTypes { get; init; } = new();

    private WalletTemplate() : base(Guid.Empty)
    {
        // EF Core constructor - initialize all required properties
        Name = string.Empty;
        Description = string.Empty;
        Version = "1.0.0";
        Fields = new List<WalletField>();
        Metadata = new Dictionary<string, object>();
        SupportedCredentialTypes = new List<string>();
    }

    public WalletTemplate(
        Guid tenantId,
        string name,
        string description,
        WalletTemplateType type,
        string? createdBy = null)
        : base(Guid.NewGuid())
    {
        Guard.AgainstEmptyGuid(tenantId, nameof(tenantId));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(description, nameof(description));

        TenantId = tenantId;
        Name = name;
        Description = description;
        Type = type;
        Version = "1.0.0";
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    public void AddField(WalletField field)
    {
        Guard.AgainstNull(field, nameof(field));

        if (Fields.Any(f => f.Name == field.Name))
        {
            throw new InvalidOperationException($"Field with name '{field.Name}' already exists");
        }

        Fields.Add(field);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveField(string fieldName)
    {
        Guard.AgainstNullOrWhiteSpace(fieldName, nameof(fieldName));

        var field = Fields.FirstOrDefault(f => f.Name == fieldName);
        if (field != null)
        {
            Fields.Remove(field);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void UpdateField(string fieldName, Action<WalletField> updateAction)
    {
        Guard.AgainstNullOrWhiteSpace(fieldName, nameof(fieldName));
        Guard.AgainstNull(updateAction, nameof(updateAction));

        var field = Fields.FirstOrDefault(f => f.Name == fieldName);
        if (field == null)
        {
            throw new InvalidOperationException($"Field with name '{fieldName}' not found");
        }

        updateAction(field);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddSupportedCredentialType(string credentialType)
    {
        Guard.AgainstNullOrWhiteSpace(credentialType, nameof(credentialType));

        if (!SupportedCredentialTypes.Contains(credentialType))
        {
            SupportedCredentialTypes.Add(credentialType);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void RemoveSupportedCredentialType(string credentialType)
    {
        if (SupportedCredentialTypes.Remove(credentialType))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void UpdateMetadata(string key, object value)
    {
        Guard.AgainstNullOrWhiteSpace(key, nameof(key));
        Guard.AgainstNull(value, nameof(value));

        Metadata[key] = value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateVersion(string version)
    {
        Guard.AgainstNullOrWhiteSpace(version, nameof(version));
        Version = version;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Represents a field in a wallet template
/// </summary>
public class WalletField
{
    public string Name { get; set; }
    public string Label { get; set; }
    public string FieldType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEditable { get; set; }
    public int DisplayOrder { get; set; }
    public string? ValidationRule { get; set; }
    public string? DefaultValue { get; set; }
    public string? MappedCredentialField { get; set; }
    public Dictionary<string, object> Properties { get; set; }

    private WalletField()
    {
        // EF Core constructor
        Name = string.Empty;
        Label = string.Empty;
        FieldType = string.Empty;
        Properties = new Dictionary<string, object>();
    }

    public WalletField(
        string name,
        string label,
        string fieldType,
        bool isRequired = false,
        int displayOrder = 0)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(label, nameof(label));
        Guard.AgainstNullOrWhiteSpace(fieldType, nameof(fieldType));

        Name = name;
        Label = label;
        FieldType = fieldType;
        IsRequired = isRequired;
        IsEditable = true;
        DisplayOrder = displayOrder;
        Properties = new Dictionary<string, object>();
    }

    public void SetMappedCredentialField(string credentialField)
    {
        MappedCredentialField = credentialField;
    }

    public void SetValidationRule(string validationRule)
    {
        ValidationRule = validationRule;
    }

    public void SetDefaultValue(string defaultValue)
    {
        DefaultValue = defaultValue;
    }

    public void UpdateEditability(bool isEditable)
    {
        IsEditable = isEditable;
    }

    public void UpdateDisplayOrder(int order)
    {
        DisplayOrder = order;
    }

    public void AddProperty(string key, object value)
    {
        Guard.AgainstNullOrWhiteSpace(key, nameof(key));
        Properties[key] = value;
    }
}

/// <summary>
/// Types of wallet templates
/// </summary>
public enum WalletTemplateType
{
    DriverLicense,
    Passport,
    StudentId,
    HealthCard,
    ProofOfAge,
    VaccinationCertificate,
    WorkingWithChildren,
    Custom
}
