namespace NumbatWallet.Application.DTOs;

/// <summary>
/// Represents a generated wallet package for a specific platform
/// </summary>
public class WalletPackageDto
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public byte[]? PackageData { get; set; }
    public string? PackageUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string PackageType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? HtmlContent { get; set; }
    public byte[]? QrCodeData { get; set; }
    public List<WalletPackageDto>? MultiPlatformPackages { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Platform-specific requirements for wallet generation
/// </summary>
public class PlatformRequirementsDto
{
    public string Platform { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
    public List<RequiredFieldDto> RequiredFields { get; set; } = new();
    public List<string> SupportedFieldTypes { get; set; } = new();
    public List<RequiredAssetDto> RequiredAssets { get; set; } = new();
    public Dictionary<string, string> Constraints { get; set; } = new();
    public List<string> SupportedFeatures { get; set; } = new();
    public List<string> ImageFormats { get; set; } = new();
    public long MaxImageSize { get; set; }
    public bool RequiresSigning { get; set; }
    public Dictionary<string, string> AdditionalRequirements { get; set; } = new();
}

public class RequiredFieldDto
{
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public string? Description { get; set; }
    public string? ValidationPattern { get; set; }
}

public class RequiredAssetDto
{
    public string AssetName { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? MaxSizeKb { get; set; }
    public bool IsMandatory { get; set; }
}

/// <summary>
/// Google Wallet pass data
/// </summary>
public class GoogleWalletPassDto
{
    public string Id { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string State { get; set; } = "ACTIVE";
    public Dictionary<string, object> ClassData { get; set; } = new();
    public Dictionary<string, object> ObjectData { get; set; } = new();
    public string Jwt { get; set; } = string.Empty;
    public string SaveUrl { get; set; } = string.Empty;
}

/// <summary>
/// Web wallet data representation
/// </summary>
public class WebWalletDto
{
    public Guid Id { get; set; }
    public string TemplateId { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public Dictionary<string, object> JsonData { get; set; } = new();
    public string QrCodeDataUrl { get; set; } = string.Empty;
    public string ShareUrl { get; set; } = string.Empty;
    public WebWalletManifestDto? PwaManifest { get; set; }
    public Dictionary<string, string> Resources { get; set; } = new();
}

public class WebWalletManifestDto
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StartUrl { get; set; } = "/";
    public string Display { get; set; } = "standalone";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string ThemeColor { get; set; } = "#007AFF";
    public List<WebWalletIconDto> Icons { get; set; } = new();
}

public class WebWalletIconDto
{
    public string Src { get; set; } = string.Empty;
    public string Sizes { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Purpose { get; set; }
}