using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Guards;
using System.Text.Json;

namespace NumbatWallet.Domain.Entities;

/// <summary>
/// Defines the schema for a credential type including its structure and validation rules
/// </summary>
public class CredentialSchema : Entity<Guid>
{
    public Guid TenantId { get; private set; }
    public string SchemaId { get; private set; } // e.g., "org.iso.18013.5.1.mDL"
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Version { get; private set; }
    public string Type { get; private set; } // e.g., "VerifiableCredential"
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private readonly List<CredentialField> _attributes = new();
    private readonly List<string> _contexts = new();
    private readonly Dictionary<string, object> _metadata = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public IReadOnlyList<CredentialField> Attributes => _attributes.AsReadOnly();
    public IReadOnlyList<string> Contexts => _contexts.AsReadOnly();
    public IReadOnlyDictionary<string, object> Metadata => _metadata.AsReadOnly();

    private CredentialSchema() : base(Guid.Empty)
    {
        // EF Core constructor
        SchemaId = string.Empty;
        Name = string.Empty;
        Description = string.Empty;
        Version = "1.0.0";
        Type = "VerifiableCredential";
    }

    public CredentialSchema(
        Guid tenantId,
        string schemaId,
        string name,
        string description,
        string type = "VerifiableCredential")
        : base(Guid.NewGuid())
    {
        Guard.AgainstEmptyGuid(tenantId, nameof(tenantId));
        Guard.AgainstNullOrWhiteSpace(schemaId, nameof(schemaId));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(description, nameof(description));

        TenantId = tenantId;
        SchemaId = schemaId;
        Name = name;
        Description = description;
        Type = type;
        Version = "1.0.0";
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;

        // Add default W3C context
        _contexts.Add("https://www.w3.org/2018/credentials/v1");
    }

    public void AddAttribute(CredentialField attribute)
    {
        Guard.AgainstNull(attribute, nameof(attribute));

        if (_attributes.Any(a => a.Name == attribute.Name))
        {
            throw new InvalidOperationException($"Attribute '{attribute.Name}' already exists in schema");
        }

        _attributes.Add(attribute);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveAttribute(string attributeName)
    {
        Guard.AgainstNullOrWhiteSpace(attributeName, nameof(attributeName));

        var attribute = _attributes.FirstOrDefault(a => a.Name == attributeName);
        if (attribute != null)
        {
            _attributes.Remove(attribute);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void AddContext(string context)
    {
        Guard.AgainstNullOrWhiteSpace(context, nameof(context));

        if (!_contexts.Contains(context))
        {
            _contexts.Add(context);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void UpdateVersion(string version)
    {
        Guard.AgainstNullOrWhiteSpace(version, nameof(version));
        Version = version;
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

    /// <summary>
    /// Validates credential data against this schema
    /// </summary>
    public ValidationResult ValidateCredentialData(Dictionary<string, object> credentialData)
    {
        var errors = new List<string>();

        // Check required attributes
        foreach (var attribute in _attributes.Where(a => a.IsRequired))
        {
            if (!credentialData.ContainsKey(attribute.Name))
            {
                errors.Add($"Required attribute '{attribute.Name}' is missing");
            }
        }

        // Validate attribute types and constraints
        foreach (var kvp in credentialData)
        {
            var attribute = _attributes.FirstOrDefault(a => a.Name == kvp.Key);
            if (attribute == null && !AllowAdditionalProperties)
            {
                errors.Add($"Unknown attribute '{kvp.Key}'");
                continue;
            }

            if (attribute != null)
            {
                var validationErrors = attribute.Validate(kvp.Value);
                errors.AddRange(validationErrors);
            }
        }

        return new ValidationResult(errors);
    }

    public bool AllowAdditionalProperties =>
        Metadata.TryGetValue("allowAdditionalProperties", out var value) &&
        value is bool allow && allow;

    public string ToJsonSchema()
    {
        var schema = new
        {
            @id = SchemaId,
            @type = "JsonSchema",
            name = Name,
            description = Description,
            version = Version,
            properties = _attributes.ToDictionary(
                a => a.Name,
                a => new
                {
                    type = a.DataType,
                    description = a.Description,
                    required = a.IsRequired,
                    pattern = a.Pattern,
                    minLength = a.MinLength,
                    maxLength = a.MaxLength,
                    minimum = a.MinValue,
                    maximum = a.MaxValue
                }),
            required = _attributes.Where(a => a.IsRequired).Select(a => a.Name).ToList(),
            additionalProperties = AllowAdditionalProperties
        };

        return JsonSerializer.Serialize(schema, JsonOptions);
    }
}

/// <summary>
/// Represents an attribute in a credential schema
/// </summary>
public class CredentialField
{
    public string Name { get; private set; }
    public string DisplayName { get; private set; }
    public string Description { get; private set; }
    public string DataType { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsArray { get; private set; }
    public string? Pattern { get; private set; }
    public int? MinLength { get; private set; }
    public int? MaxLength { get; private set; }
    public double? MinValue { get; private set; }
    public double? MaxValue { get; private set; }
    public List<string> AllowedValues { get; private set; }
    public Dictionary<string, object> Properties { get; private set; }

    public CredentialField(
        string name,
        string displayName,
        string dataType,
        bool isRequired = false)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(displayName, nameof(displayName));
        Guard.AgainstNullOrWhiteSpace(dataType, nameof(dataType));

        Name = name;
        DisplayName = displayName;
        Description = string.Empty;
        DataType = dataType;
        IsRequired = isRequired;
        AllowedValues = new List<string>();
        Properties = new Dictionary<string, object>();
    }

    public void SetDescription(string description)
    {
        Description = description;
    }

    public void SetPattern(string pattern)
    {
        Pattern = pattern;
    }

    public void SetStringConstraints(int? minLength, int? maxLength)
    {
        MinLength = minLength;
        MaxLength = maxLength;
    }

    public void SetNumberConstraints(double? minValue, double? maxValue)
    {
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public void SetAllowedValues(params string[] values)
    {
        AllowedValues.Clear();
        AllowedValues.AddRange(values);
    }

    public void MakeArray()
    {
        IsArray = true;
    }

    public List<string> Validate(object? value)
    {
        var errors = new List<string>();

        if (value == null)
        {
            if (IsRequired)
            {
                errors.Add($"'{Name}' is required but was null");
            }
            return errors;
        }

        // Type validation
        bool isValidType;
        switch (DataType.ToLowerInvariant())
        {
            case "string":
                isValidType = value is string;
                break;
            case "number":
                isValidType = value is int or long or float or double or decimal;
                break;
            case "boolean":
                isValidType = value is bool;
                break;
            case "date":
                isValidType = value is DateTime or DateTimeOffset ||
                             (value is string dateStr && DateTime.TryParse(dateStr, out _));
                break;
            case "object":
                isValidType = value is Dictionary<string, object> or JsonElement;
                break;
            case "array":
                isValidType = value is Array or System.Collections.IEnumerable;
                break;
            default:
                isValidType = true;
                break;
        }

        if (!isValidType)
        {
            errors.Add($"'{Name}' expected type '{DataType}' but got '{value.GetType().Name}'");
            return errors;
        }

        // String validations
        if (value is string strValue)
        {
            if (MinLength.HasValue && strValue.Length < MinLength.Value)
            {
                errors.Add($"'{Name}' must be at least {MinLength} characters");
            }

            if (MaxLength.HasValue && strValue.Length > MaxLength.Value)
            {
                errors.Add($"'{Name}' must be no more than {MaxLength} characters");
            }

            if (!string.IsNullOrEmpty(Pattern))
            {
                var regex = new System.Text.RegularExpressions.Regex(Pattern);
                if (!regex.IsMatch(strValue))
                {
                    errors.Add($"'{Name}' does not match required pattern");
                }
            }

            if (AllowedValues.Any() && !AllowedValues.Contains(strValue))
            {
                errors.Add($"'{Name}' must be one of: {string.Join(", ", AllowedValues)}");
            }
        }

        // Number validations
        if (value is IComparable numValue && DataType.ToLowerInvariant() == "number")
        {
            if (MinValue.HasValue && Convert.ToDouble(numValue) < MinValue.Value)
            {
                errors.Add($"'{Name}' must be at least {MinValue}");
            }

            if (MaxValue.HasValue && Convert.ToDouble(numValue) > MaxValue.Value)
            {
                errors.Add($"'{Name}' must be no more than {MaxValue}");
            }
        }

        return errors;
    }
}

/// <summary>
/// Result of credential validation against a schema
/// </summary>
public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; }

    public ValidationResult(List<string> errors)
    {
        Errors = errors ?? new List<string>();
    }
}