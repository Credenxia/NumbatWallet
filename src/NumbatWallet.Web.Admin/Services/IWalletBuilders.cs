using NumbatWallet.Web.Admin.Models;

namespace NumbatWallet.Web.Admin.Services;

/// <summary>
/// Apple Wallet (.pkpass) builder interface
/// </summary>
public interface IAppleWalletBuilder
{
    Task<byte[]> GeneratePkpassAsync(
        WalletTemplateDto walletTemplate,
        Dictionary<string, object> data,
        AppleWalletOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Google Wallet pass builder interface
/// </summary>
public interface IGoogleWalletBuilder
{
    Task<GoogleWalletPass> GenerateGooglePassAsync(
        WalletTemplateDto walletTemplate,
        Dictionary<string, object> data,
        GoogleWalletOptions options,
        CancellationToken cancellationToken = default);

    Task<string> CreateJwtAsync(GoogleWalletPass pass);
    string GetAddToWalletLink(string jwt);
}

/// <summary>
/// Web Wallet HTML builder interface
/// </summary>
public interface IWebWalletBuilder
{
    Task<string> GenerateWebWalletAsync(
        WalletTemplateDto walletTemplate,
        Dictionary<string, object> data,
        WebWalletOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Google Wallet Pass representation
/// </summary>
public class GoogleWalletPass
{
    public string Id { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = [];
}
