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

    private readonly List<WalletField> _fields = new();
    private readonly Dictionary<string, object> _metadata = new();
    private readonly List<string> _supportedCredentialTypes = new();

    public IReadOnlyList<WalletField> Fields => _fields.AsReadOnly();
    public IReadOnlyDictionary<string, object> Metadata => _metadata.AsReadOnly();
    public IReadOnlyList<string> SupportedCredentialTypes => _supportedCredentialTypes.AsReadOnly();

    private WalletTemplate() : base(Guid.Empty)
    {
        // EF Core constructor
        Name = string.Empty;
        Description = string.Empty;
        Version = "1.0.0";
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

        if (_fields.Any(f => f.Name == field.Name))
        {
            throw new InvalidOperationException($"Field with name '{field.Name}' already exists");
        }

        _fields.Add(field);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveField(string fieldName)
    {
        Guard.AgainstNullOrWhiteSpace(fieldName, nameof(fieldName));

        var field = _fields.FirstOrDefault(f => f.Name == fieldName);
        if (field != null)
        {
            _fields.Remove(field);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void UpdateField(string fieldName, Action<WalletField> updateAction)
    {
        Guard.AgainstNullOrWhiteSpace(fieldName, nameof(fieldName));
        Guard.AgainstNull(updateAction, nameof(updateAction));

        var field = _fields.FirstOrDefault(f => f.Name == fieldName);
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

        if (!_supportedCredentialTypes.Contains(credentialType))
        {
            _supportedCredentialTypes.Add(credentialType);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void RemoveSupportedCredentialType(string credentialType)
    {
        if (_supportedCredentialTypes.Remove(credentialType))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void UpdateMetadata(string key, object value)
    {
        Guard.AgainstNullOrWhiteSpace(key, nameof(key));
        Guard.AgainstNull(value, nameof(value));

        _metadata[key] = value;
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
    public string Name { get; private set; }
    public string Label { get; private set; }
    public string FieldType { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsEditable { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? ValidationRule { get; private set; }
    public string? DefaultValue { get; private set; }
    public string? MappedCredentialField { get; private set; }
    public Dictionary<string, object> Properties { get; private set; }

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