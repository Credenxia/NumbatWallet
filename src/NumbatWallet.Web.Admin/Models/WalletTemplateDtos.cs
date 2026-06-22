namespace NumbatWallet.Web.Admin.Models;

/// <summary>
/// DTO for WalletTemplate entity - used by Admin Portal to communicate with API
/// </summary>
public class WalletTemplateDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WalletTemplateType Type { get; set; }
    public bool IsActive { get; set; }
    public List<WalletFieldDto> Fields { get; set; } = [];
    public List<string> SupportedCredentialTypes { get; set; } = [];
    public Dictionary<string, object> Metadata { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public int Version { get; set; } = 1; // For versioning templates
}

/// <summary>
/// DTO for WalletField value object
/// </summary>
public class WalletFieldDto
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsEditable { get; set; }
    public string? ValidationRule { get; set; }
    public string? DefaultValue { get; set; }
    public string? MappedCredentialField { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Wallet template types
/// </summary>
public enum WalletTemplateType
{
    Custom = 0,
    DriversLicense = 1,
    Passport = 2,
    HealthCard = 3,
    StudentId = 4
}

/// <summary>
/// Options for Apple Wallet (.pkpass) generation
/// </summary>
public class AppleWalletOptions
{
    public string TeamIdentifier { get; set; } = string.Empty;
    public string PassTypeIdentifier { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LogoText { get; set; } = string.Empty;
}

/// <summary>
/// Options for Google Wallet pass generation
/// </summary>
public class GoogleWalletOptions
{
    public string IssuerId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;
    public string CardTitle { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
}

/// <summary>
/// Options for Web Wallet HTML generation
/// </summary>
public class WebWalletOptions
{
    public string Theme { get; set; } = "light";
    public string PrimaryColor { get; set; } = "#007AFF";
    public bool EnableQrCode { get; set; } = true;
    public bool EnableSharing { get; set; } = true;
    public bool EnableOfflineMode { get; set; } = true;
}
