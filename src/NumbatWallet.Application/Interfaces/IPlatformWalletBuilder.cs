using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Interface for platform-specific wallet builders
/// </summary>
public interface IPlatformWalletBuilder
{
    /// <summary>
    /// Build a platform-specific wallet package from a template
    /// </summary>
    Task<WalletPackageDto> BuildWalletPackageAsync(
        WalletTemplate walletTemplate,
        Dictionary<string, object> data,
        WalletPlatform platform,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Build Apple Wallet package
    /// </summary>
    Task<object> BuildAppleWalletAsync(
        Wallet wallet,
        WalletTemplate walletTemplate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Build Google Wallet package
    /// </summary>
    Task<object> BuildGoogleWalletAsync(
        Wallet wallet,
        WalletTemplate walletTemplate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Build Web Wallet package
    /// </summary>
    Task<object> BuildWebWalletAsync(
        Wallet wallet,
        WalletTemplate walletTemplate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that a template is compatible with a specific platform
    /// </summary>
    bool IsTemplateCompatible(WalletTemplate walletTemplate, WalletPlatform platform);

    /// <summary>
    /// Get platform-specific requirements for a wallet type
    /// </summary>
    PlatformRequirementsDto GetPlatformRequirements(WalletPlatform platform, WalletTemplateType templateType);
}

/// <summary>
/// Apple Wallet (Passbook) builder
/// </summary>
public interface IAppleWalletBuilder
{
    /// <summary>
    /// Generate a .pkpass file for Apple Wallet
    /// </summary>
    Task<byte[]> GeneratePkpassAsync(
        WalletTemplate walletTemplate,
        Dictionary<string, object> data,
        AppleWalletOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sign the pass with Apple certificates
    /// </summary>
    Task<byte[]> SignPassAsync(byte[] passData, string certificatePath, string password);

    /// <summary>
    /// Validate pass.json structure
    /// </summary>
    bool ValidatePassJson(string passJson);
}

/// <summary>
/// Google Wallet builder
/// </summary>
public interface IGoogleWalletBuilder
{
    /// <summary>
    /// Generate a Google Wallet pass
    /// </summary>
    Task<GoogleWalletPassDto> GenerateGooglePassAsync(
        WalletTemplate walletTemplate,
        Dictionary<string, object> data,
        GoogleWalletOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create JWT for Google Wallet API
    /// </summary>
    Task<string> CreateJwtAsync(GoogleWalletPassDto pass);

    /// <summary>
    /// Get "Add to Google Wallet" link
    /// </summary>
    string GetAddToWalletLink(string jwt);
}

/// <summary>
/// Web/PWA wallet builder
/// </summary>
public interface IWebWalletBuilder
{
    /// <summary>
    /// Generate web wallet HTML/JSON representation
    /// </summary>
    Task<WebWalletDto> GenerateWebWalletAsync(
        WalletTemplate walletTemplate,
        Dictionary<string, object> data,
        WebWalletOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate QR code for wallet data
    /// </summary>
    Task<byte[]> GenerateQrCodeAsync(string walletId, QrCodeOptions options);

    /// <summary>
    /// Generate PWA manifest for installable wallet
    /// </summary>
    Task<string> GeneratePwaManifestAsync(WalletTemplate walletTemplate);
}

/// <summary>
/// Supported wallet platforms
/// </summary>
public enum WalletPlatform
{
    AppleWallet,
    GoogleWallet,
    Web,
    PWA,
    All
}

/// <summary>
/// Apple Wallet configuration options
/// </summary>
public class AppleWalletOptions
{
    public string TeamIdentifier { get; set; } = string.Empty;
    public string PassTypeIdentifier { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string ForegroundColor { get; set; } = "#000000";
    public string LabelColor { get; set; } = "#666666";
    public string LogoText { get; set; } = string.Empty;
    public byte[]? Logo { get; set; }
    public byte[]? Icon { get; set; }
    public byte[]? Thumbnail { get; set; }
    public bool EnableBarcode { get; set; } = true;
    public string BarcodeFormat { get; set; } = "PKBarcodeFormatQR";
    public string BarcodeMessage { get; set; } = string.Empty;
    public List<AppleWalletField> HeaderFields { get; set; } = new();
    public List<AppleWalletField> PrimaryFields { get; set; } = new();
    public List<AppleWalletField> SecondaryFields { get; set; } = new();
    public List<AppleWalletField> AuxiliaryFields { get; set; } = new();
    public List<AppleWalletField> BackFields { get; set; } = new();
}

public class AppleWalletField
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? ChangeMessage { get; set; }
    public string? TextAlignment { get; set; }
}

/// <summary>
/// Google Wallet configuration options
/// </summary>
public class GoogleWalletOptions
{
    public string IssuerId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string HexBackgroundColor { get; set; } = "#FFFFFF";
    public string CardTitle { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public string Subheader { get; set; } = string.Empty;
    public Dictionary<string, string> TextModulesData { get; set; } = new();
    public Dictionary<string, string> InfoModulesData { get; set; } = new();
    public string BarcodeType { get; set; } = "QR_CODE";
    public string BarcodeValue { get; set; } = string.Empty;
    public string BarcodeAlternateText { get; set; } = string.Empty;
}

/// <summary>
/// Web wallet configuration options
/// </summary>
public class WebWalletOptions
{
    public string Theme { get; set; } = "light";
    public string PrimaryColor { get; set; } = "#007AFF";
    public string SecondaryColor { get; set; } = "#F0F0F0";
    public bool EnableQrCode { get; set; } = true;
    public bool EnableSharing { get; set; } = true;
    public bool EnablePrinting { get; set; } = true;
    public bool EnableOfflineMode { get; set; } = true;
    public string CustomCss { get; set; } = string.Empty;
    public string CustomJs { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// QR code generation options
/// </summary>
public class QrCodeOptions
{
    public int Size { get; set; } = 300;
    public string ErrorCorrection { get; set; } = "M";
    public string Format { get; set; } = "PNG";
    public string ForegroundColor { get; set; } = "#000000";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public byte[]? Logo { get; set; }
    public int Margin { get; set; } = 4;
}