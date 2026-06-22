using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;
using QRCoder;

namespace NumbatWallet.Infrastructure.WalletBuilders;

/// <summary>
/// Implementation of Web/PWA wallet builder
/// </summary>
public class WebWalletBuilder : IWebWalletBuilder
{
    private readonly ILogger<WebWalletBuilder> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    public WebWalletBuilder(ILogger<WebWalletBuilder> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }


    public async Task<WebWalletDto> GenerateWebWalletAsync(
        WalletTemplate walletTemplate,
        Dictionary<string, object> data,
        WebWalletOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Web wallet for template {TemplateId}", walletTemplate.Id);

        var walletId = Guid.NewGuid();
        var shareUrl = $"{_configuration["WebWallet:BaseUrl"]}/wallet/{walletId}";

        // Generate QR code if enabled
        string qrCodeDataUrl = string.Empty;
        if (options.EnableQrCode)
        {
            var qrOptions = new QrCodeOptions
            {
                Size = 300,
                ErrorCorrection = "M",
                ForegroundColor = "#000000",
                BackgroundColor = "#FFFFFF"
            };
            var qrCodeData = await GenerateQrCodeAsync(walletId.ToString(), qrOptions);
            qrCodeDataUrl = $"data:image/png;base64,{Convert.ToBase64String(qrCodeData)}";
        }

        // Create JSON data structure
        var jsonData = CreateJsonData(walletTemplate, data);

        // Generate HTML content
        var htmlContent = GenerateHtmlTemplate(walletTemplate, data, options);

        // Create PWA manifest if needed
        WebWalletManifestDto? pwaManifest = null;
        if (options.EnableOfflineMode)
        {
            var manifestJson = await GeneratePwaManifestAsync(walletTemplate);
            pwaManifest = JsonSerializer.Deserialize<WebWalletManifestDto>(manifestJson, _jsonOptions);
        }

        return new WebWalletDto
        {
            Id = walletId,
            TemplateId = walletTemplate.Id.ToString(),
            HtmlContent = htmlContent,
            JsonData = jsonData,
            QrCodeDataUrl = qrCodeDataUrl,
            ShareUrl = shareUrl,
            PwaManifest = pwaManifest,
            Resources = new Dictionary<string, string>
            {
                ["css"] = GenerateCss(options),
                ["js"] = GenerateJavaScript(options)
            }
        };
    }

    public Task<byte[]> GenerateQrCodeAsync(string walletId, QrCodeOptions options)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = $"{_configuration["WebWallet:BaseUrl"]}/verify/{walletId}";
            var qrCodeData = qrGenerator.CreateQrCode(qrData, GetErrorCorrectionLevel(options.ErrorCorrection));

            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(options.Size / 25); // Pixels per module

            _logger.LogDebug("Generated QR code for wallet {WalletId}", walletId);
            return Task.FromResult(qrCodeImage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate QR code");
            return Task.FromResult(Array.Empty<byte>());
        }
    }

    public Task<string> GeneratePwaManifestAsync(WalletTemplate walletTemplate)
    {
        var manifest = new WebWalletManifestDto
        {
            Name = $"{walletTemplate.Name} - NumbatWallet",
            ShortName = walletTemplate.Name.Length > 12 ? walletTemplate.Name[..12] : walletTemplate.Name,
            Description = walletTemplate.Description,
            StartUrl = "/",
            Display = "standalone",
            BackgroundColor = "#FFFFFF",
            ThemeColor = "#003366",
            Icons = new List<WebWalletIconDto>
            {
                new() { Src = "/icons/icon-192.png", Sizes = "192x192", Type = "image/png" },
                new() { Src = "/icons/icon-512.png", Sizes = "512x512", Type = "image/png" },
                new() { Src = "/icons/icon-maskable.png", Sizes = "512x512", Type = "image/png", Purpose = "maskable" }
            }
        };

        var manifestJson = JsonSerializer.Serialize(manifest, _jsonOptions);
        return Task.FromResult(manifestJson);
    }

    public bool IsTemplateCompatible(WalletTemplate walletTemplate, WalletPlatform platform)
    {
        if (platform != WalletPlatform.Web && platform != WalletPlatform.PWA && platform != WalletPlatform.All)
        {
            return false;
        }

        // Web wallets support all template types
        return true;
    }

    public PlatformRequirementsDto GetPlatformRequirements(WalletPlatform platform, WalletTemplateType templateType)
    {
        if (platform != WalletPlatform.Web && platform != WalletPlatform.PWA)
        {
            return new PlatformRequirementsDto { Platform = platform.ToString() };
        }

        return new PlatformRequirementsDto
        {
            Platform = platform == WalletPlatform.PWA ? "PWA" : "Web",
            RequiredFields = new List<RequiredFieldDto>
            {
                new() { FieldName = "title", FieldType = "string", IsMandatory = true, Description = "Wallet title" },
                new() { FieldName = "description", FieldType = "string", IsMandatory = false, Description = "Wallet description" }
            },
            RequiredAssets = new List<RequiredAssetDto>
            {
                new() { AssetName = "icon-192", AssetType = "PNG", MimeType = "image/png", Width = 192, Height = 192, IsMandatory = platform == WalletPlatform.PWA },
                new() { AssetName = "icon-512", AssetType = "PNG", MimeType = "image/png", Width = 512, Height = 512, IsMandatory = platform == WalletPlatform.PWA },
                new() { AssetName = "favicon", AssetType = "ICO/PNG", MimeType = "image/*", Width = 32, Height = 32, IsMandatory = false }
            },
            Constraints = new Dictionary<string, string>
            {
                ["maxHtmlSize"] = "5MB",
                ["supportedBrowsers"] = "Chrome,Firefox,Safari,Edge",
                ["minViewportWidth"] = "320px"
            },
            SupportedFeatures = new List<string>
            {
                "responsive", "offline", "qrcode", "sharing", "printing",
                "push_notifications", "biometric_auth", "deep_linking"
            }
        };
    }

    private Dictionary<string, object> CreateJsonData(WalletTemplate walletTemplate, Dictionary<string, object> data)
    {
        var jsonData = new Dictionary<string, object>
        {
            ["@context"] = "https://www.w3.org/2018/credentials/v1",
            ["type"] = new[] { "VerifiableCredential", walletTemplate.Type.ToString() },
            ["id"] = Guid.NewGuid().ToString(),
            ["issuanceDate"] = DateTime.UtcNow.ToString("O"),
            ["issuer"] = "Government of Western Australia",
            ["credentialSubject"] = new Dictionary<string, object>()
        };

        var subject = (Dictionary<string, object>)jsonData["credentialSubject"];
        foreach (var field in walletTemplate.Fields)
        {
            if (data.TryGetValue(field.Name, out var value))
            {
                subject[field.Name] = value;
            }
        }

        return jsonData;
    }

    private string GenerateHtmlTemplate(WalletTemplate walletTemplate, Dictionary<string, object> data, WebWalletOptions options)
    {
        var fieldsHtml = new StringBuilder();
        foreach (var field in walletTemplate.Fields.OrderBy(f => f.DisplayOrder))
        {
            if (data.TryGetValue(field.Name, out var value))
            {
                fieldsHtml.AppendLine($@"
                    <div class='field'>
                        <span class='field-label'>{field.Label}:</span>
                        <span class='field-value'>{value}</span>
                    </div>");
            }
        }

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta name='theme-color' content='{options.PrimaryColor}'>
    <title>{walletTemplate.Name} - NumbatWallet</title>
    <style>{GenerateCss(options)}</style>
    {(options.EnableOfflineMode ? "<link rel='manifest' href='/manifest.json'>" : "")}
</head>
<body class='{options.Theme}'>
    <div class='wallet-container'>
        <header class='wallet-header'>
            <img src='/logo.png' alt='WA Government' class='logo'>
            <h1>{walletTemplate.Name}</h1>
        </header>
        <main class='wallet-content'>
            <div class='wallet-card'>
                {fieldsHtml}
            </div>
            {(options.EnableQrCode ? "<div class='qr-code'><img src='{{qrCodeDataUrl}}' alt='QR Code'></div>" : "")}
        </main>
        <footer class='wallet-footer'>
            {(options.EnableSharing ? "<button class='btn-share'>Share</button>" : "")}
            {(options.EnablePrinting ? "<button class='btn-print' onclick='window.print()'>Print</button>" : "")}
        </footer>
    </div>
    <script>{GenerateJavaScript(options)}</script>
</body>
</html>";
    }

    private string GenerateHtmlContent(WebWalletDto webWallet)
    {
        return webWallet.HtmlContent.Replace("{{qrCodeDataUrl}}", webWallet.QrCodeDataUrl);
    }

    private string GenerateCss(WebWalletOptions options)
    {
        return $@"
:root {{
    --primary-color: {options.PrimaryColor};
    --secondary-color: {options.SecondaryColor};
}}

* {{
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}}

body {{
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    background: var(--secondary-color);
    min-height: 100vh;
}}

.wallet-container {{
    max-width: 600px;
    margin: 0 auto;
    padding: 20px;
}}

.wallet-header {{
    background: var(--primary-color);
    color: white;
    padding: 20px;
    border-radius: 10px 10px 0 0;
    text-align: center;
}}

.logo {{
    height: 50px;
    margin-bottom: 10px;
}}

.wallet-content {{
    background: white;
    padding: 20px;
    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
}}

.wallet-card {{
    padding: 20px;
    border: 1px solid #e0e0e0;
    border-radius: 8px;
    margin-bottom: 20px;
}}

.field {{
    display: flex;
    justify-content: space-between;
    padding: 10px 0;
    border-bottom: 1px solid #f0f0f0;
}}

.field:last-child {{
    border-bottom: none;
}}

.field-label {{
    font-weight: 600;
    color: #666;
}}

.field-value {{
    color: #333;
}}

.qr-code {{
    text-align: center;
    padding: 20px;
}}

.qr-code img {{
    max-width: 200px;
}}

.wallet-footer {{
    background: white;
    padding: 20px;
    border-radius: 0 0 10px 10px;
    text-align: center;
    border-top: 1px solid #e0e0e0;
}}

.btn-share, .btn-print {{
    background: var(--primary-color);
    color: white;
    border: none;
    padding: 10px 20px;
    border-radius: 5px;
    margin: 0 5px;
    cursor: pointer;
    font-size: 16px;
}}

.btn-share:hover, .btn-print:hover {{
    opacity: 0.9;
}}

.dark {{
    background: #1a1a1a;
    color: white;
}}

.dark .wallet-content {{
    background: #2a2a2a;
    color: white;
}}

.dark .wallet-card {{
    border-color: #444;
}}

.dark .field {{
    border-color: #333;
}}

.dark .field-label {{
    color: #aaa;
}}

.dark .field-value {{
    color: #fff;
}}

@media print {{
    .wallet-footer {{
        display: none;
    }}
}}

{options.CustomCss}";
    }

    private string GenerateJavaScript(WebWalletOptions options)
    {
        return $@"
// Service Worker registration for PWA
if ('serviceWorker' in navigator && {options.EnableOfflineMode.ToString().ToLowerInvariant()}) {{
    navigator.serviceWorker.register('/sw.js').then(function(registration) {{
        console.log('ServiceWorker registration successful');
    }}).catch(function(err) {{
        console.log('ServiceWorker registration failed: ', err);
    }});
}}

// Share functionality
document.querySelector('.btn-share')?.addEventListener('click', async () => {{
    if (navigator.share && {options.EnableSharing.ToString().ToLowerInvariant()}) {{
        try {{
            await navigator.share({{
                title: document.title,
                text: 'Check out my digital wallet',
                url: window.location.href
            }});
        }} catch (err) {{
            console.log('Error sharing:', err);
        }}
    }} else {{
        // Fallback to clipboard
        navigator.clipboard.writeText(window.location.href);
        alert('Link copied to clipboard!');
    }}
}});

// Theme toggle
function toggleTheme() {{
    document.body.classList.toggle('dark');
    localStorage.setItem('theme', document.body.classList.contains('dark') ? 'dark' : 'light');
}}

// Load saved theme
const savedTheme = localStorage.getItem('theme');
if (savedTheme) {{
    document.body.classList.toggle('dark', savedTheme === 'dark');
}}

{options.CustomJs}";
    }

    private QRCodeGenerator.ECCLevel GetErrorCorrectionLevel(string level)
    {
        return level?.ToUpperInvariant() switch
        {
            "L" => QRCodeGenerator.ECCLevel.L,
            "M" => QRCodeGenerator.ECCLevel.M,
            "Q" => QRCodeGenerator.ECCLevel.Q,
            "H" => QRCodeGenerator.ECCLevel.H,
            _ => QRCodeGenerator.ECCLevel.M
        };
    }

    private byte[] HexToByteArray(string hex)
    {
        hex = hex.Replace("#", "");
        var bytes = new byte[3];
        for (int i = 0; i < 3; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }
}