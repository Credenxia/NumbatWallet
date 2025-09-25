using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Infrastructure.WalletBuilders;

/// <summary>
/// Composite platform wallet builder that delegates to specific platform builders
/// </summary>
public class PlatformWalletBuilder : IPlatformWalletBuilder
{
    private readonly IAppleWalletBuilder _appleWalletBuilder;
    private readonly IGoogleWalletBuilder _googleWalletBuilder;
    private readonly IWebWalletBuilder _webWalletBuilder;
    private readonly ILogger<PlatformWalletBuilder> _logger;

    public PlatformWalletBuilder(
        IAppleWalletBuilder appleWalletBuilder,
        IGoogleWalletBuilder googleWalletBuilder,
        IWebWalletBuilder webWalletBuilder,
        ILogger<PlatformWalletBuilder> logger)
    {
        _appleWalletBuilder = appleWalletBuilder;
        _googleWalletBuilder = googleWalletBuilder;
        _webWalletBuilder = webWalletBuilder;
        _logger = logger;
    }

    public async Task<WalletPackageDto> BuildWalletPackageAsync(
        WalletTemplate walletTemplate,
        Dictionary<string, object> data,
        WalletPlatform platform,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building wallet package for platform: {Platform}, template: {TemplateName}",
            platform, walletTemplate.Name);

        var packageDto = new WalletPackageDto
        {
            Id = Guid.NewGuid(),
            TemplateId = walletTemplate.Id,
            TemplateName = walletTemplate.Name,
            Platform = platform.ToString(),
            CreatedAt = DateTime.UtcNow,
            GeneratedAt = DateTime.UtcNow,
            Data = data
        };

        try
        {
            switch (platform)
            {
                case WalletPlatform.AppleWallet:
                    var appleOptions = CreateAppleWalletOptions(walletTemplate, data);
                    var pkpassData = await _appleWalletBuilder.GeneratePkpassAsync(
                        walletTemplate, data, appleOptions, cancellationToken);

                    packageDto.PackageData = pkpassData;
                    packageDto.PackageType = "application/vnd.apple.pkpass";
                    packageDto.FileName = $"{walletTemplate.Name.Replace(" ", "_")}.pkpass";
                    break;

                case WalletPlatform.GoogleWallet:
                    var googleOptions = CreateGoogleWalletOptions(walletTemplate, data);
                    var googlePass = await _googleWalletBuilder.GenerateGooglePassAsync(
                        walletTemplate, data, googleOptions, cancellationToken);

                    var jwt = await _googleWalletBuilder.CreateJwtAsync(googlePass);
                    packageDto.PackageUrl = _googleWalletBuilder.GetAddToWalletLink(jwt);
                    packageDto.PackageType = "application/jwt";
                    packageDto.Metadata["jwt"] = jwt;
                    packageDto.Metadata["passId"] = googlePass.Id;
                    break;

                case WalletPlatform.Web:
                case WalletPlatform.PWA:
                    var webOptions = CreateWebWalletOptions(walletTemplate, data);
                    var webWallet = await _webWalletBuilder.GenerateWebWalletAsync(
                        walletTemplate, data, webOptions, cancellationToken);

                    packageDto.HtmlContent = webWallet.HtmlContent;
                    packageDto.PackageType = "text/html";
                    packageDto.Metadata["walletId"] = webWallet.Id;
                    packageDto.Metadata["shareUrl"] = webWallet.ShareUrl;

                    if (webOptions.EnableQrCode)
                    {
                        var qrOptions = new QrCodeOptions { Size = 300 };
                        var qrCode = await _webWalletBuilder.GenerateQrCodeAsync(webWallet.Id.ToString(), qrOptions);
                        packageDto.QrCodeData = qrCode;
                    }

                    if (platform == WalletPlatform.PWA)
                    {
                        var manifest = await _webWalletBuilder.GeneratePwaManifestAsync(walletTemplate);
                        packageDto.Metadata["pwaManifest"] = manifest;
                    }
                    break;

                case WalletPlatform.All:
                    // Generate for all platforms
                    var tasks = new List<Task<WalletPackageDto>>
                    {
                        BuildWalletPackageAsync(walletTemplate, data, WalletPlatform.AppleWallet, cancellationToken),
                        BuildWalletPackageAsync(walletTemplate, data, WalletPlatform.GoogleWallet, cancellationToken),
                        BuildWalletPackageAsync(walletTemplate, data, WalletPlatform.Web, cancellationToken)
                    };

                    var results = await Task.WhenAll(tasks);

                    // Combine results into a single package
                    packageDto.MultiPlatformPackages = results.ToList();
                    packageDto.PackageType = "multiplatform";
                    break;

                default:
                    throw new NotSupportedException($"Platform {platform} is not supported");
            }

            packageDto.IsSuccess = true;
            _logger.LogInformation("Successfully built wallet package for platform: {Platform}", platform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building wallet package for platform: {Platform}", platform);
            packageDto.IsSuccess = false;
            packageDto.ErrorMessage = ex.Message;
        }

        return packageDto;
    }

    public bool IsTemplateCompatible(WalletTemplate walletTemplate, WalletPlatform platform)
    {
        // Check if template has required fields for the platform
        switch (platform)
        {
            case WalletPlatform.AppleWallet:
                // Apple Wallet requires certain field types
                return walletTemplate.Fields.Any(f =>
                    f.FieldType == "text" || f.FieldType == "date" || f.FieldType == "image");

            case WalletPlatform.GoogleWallet:
                // Google Wallet has different requirements
                return walletTemplate.Fields.Any(f =>
                    f.FieldType == "text" || f.FieldType == "number");

            case WalletPlatform.Web:
            case WalletPlatform.PWA:
                // Web is most flexible
                return true;

            case WalletPlatform.All:
                // Must be compatible with all platforms
                return IsTemplateCompatible(walletTemplate, WalletPlatform.AppleWallet) &&
                       IsTemplateCompatible(walletTemplate, WalletPlatform.GoogleWallet) &&
                       IsTemplateCompatible(walletTemplate, WalletPlatform.Web);

            default:
                return false;
        }
    }

    public PlatformRequirementsDto GetPlatformRequirements(WalletPlatform platform, WalletTemplateType templateType)
    {
        var requirements = new PlatformRequirementsDto
        {
            Platform = platform.ToString(),
            TemplateType = templateType.ToString()
        };

        switch (platform)
        {
            case WalletPlatform.AppleWallet:
                requirements.RequiredFields = new List<RequiredFieldDto>
                {
                    new() { FieldName = "title", FieldType = "text", IsMandatory = true },
                    new() { FieldName = "serialNumber", FieldType = "text", IsMandatory = true }
                };
                requirements.SupportedFieldTypes = new List<string> { "text", "date", "number", "image", "url" };
                requirements.MaxImageSize = 2 * 1024 * 1024; // 2MB
                requirements.ImageFormats = new List<string> { "PNG", "JPEG" };
                requirements.RequiresSigning = true;
                requirements.AdditionalRequirements["teamIdentifier"] = "Required for signing";
                requirements.AdditionalRequirements["passTypeIdentifier"] = "Required for pass type";
                break;

            case WalletPlatform.GoogleWallet:
                requirements.RequiredFields = new List<RequiredFieldDto>
                {
                    new() { FieldName = "id", FieldType = "text", IsMandatory = true },
                    new() { FieldName = "classId", FieldType = "text", IsMandatory = true },
                    new() { FieldName = "state", FieldType = "text", IsMandatory = true }
                };
                requirements.SupportedFieldTypes = new List<string> { "text", "number", "date", "url", "image" };
                requirements.MaxImageSize = 1 * 1024 * 1024; // 1MB
                requirements.ImageFormats = new List<string> { "PNG", "JPEG", "WEBP" };
                requirements.RequiresSigning = true;
                requirements.AdditionalRequirements["issuerId"] = "Google Wallet Issuer ID";
                requirements.AdditionalRequirements["serviceAccount"] = "Service account for JWT signing";
                break;

            case WalletPlatform.Web:
            case WalletPlatform.PWA:
                requirements.RequiredFields = new List<RequiredFieldDto>
                {
                    new() { FieldName = "id", FieldType = "text", IsMandatory = true }
                };
                requirements.SupportedFieldTypes = new List<string>
                {
                    "text", "number", "date", "datetime", "boolean",
                    "email", "phone", "url", "image", "select", "multiselect"
                };
                requirements.MaxImageSize = 5 * 1024 * 1024; // 5MB
                requirements.ImageFormats = new List<string> { "PNG", "JPEG", "WEBP", "SVG" };
                requirements.RequiresSigning = false;

                if (platform == WalletPlatform.PWA)
                {
                    requirements.AdditionalRequirements["manifest"] = "PWA manifest.json required";
                    requirements.AdditionalRequirements["serviceWorker"] = "Service worker for offline mode";
                    requirements.AdditionalRequirements["https"] = "HTTPS required for PWA";
                }
                break;
        }

        return requirements;
    }

    private AppleWalletOptions CreateAppleWalletOptions(WalletTemplate walletTemplate, Dictionary<string, object> data)
    {
        var options = new AppleWalletOptions
        {
            OrganizationName = "Western Australia Government",
            Description = walletTemplate.Description,
            TeamIdentifier = "WA_TEAM_ID", // Should come from configuration
            PassTypeIdentifier = $"pass.au.gov.wa.{walletTemplate.Type.ToString().ToLowerInvariant()}",
            BackgroundColor = "#003087", // WA Government blue
            ForegroundColor = "#FFFFFF",
            LabelColor = "#CCCCCC"
        };

        // Map template fields to Apple Wallet fields
        foreach (var field in walletTemplate.Fields.OrderBy(f => f.DisplayOrder))
        {
            if (data.TryGetValue(field.Name, out var value))
            {
                var appleField = new AppleWalletField
                {
                    Key = field.Name,
                    Label = field.Label,
                    Value = value?.ToString() ?? string.Empty
                };

                // Distribute fields based on importance
                if (field.DisplayOrder < 2)
                {
                    options.HeaderFields.Add(appleField);
                }
                else if (field.DisplayOrder < 4)
                {
                    options.PrimaryFields.Add(appleField);
                }
                else if (field.DisplayOrder < 8)
                {
                    options.SecondaryFields.Add(appleField);
                }
                else
                {
                    options.BackFields.Add(appleField);
                }
            }
        }

        return options;
    }

    private GoogleWalletOptions CreateGoogleWalletOptions(WalletTemplate walletTemplate, Dictionary<string, object> data)
    {
        var options = new GoogleWalletOptions
        {
            IssuerId = "WA_GOV_ISSUER_ID", // Should come from configuration
            ClassId = $"wa_gov_{walletTemplate.Type.ToString().ToLowerInvariant()}_class",
            ObjectId = Guid.NewGuid().ToString(),
            CardTitle = walletTemplate.Name,
            Header = walletTemplate.Description,
            HexBackgroundColor = "#003087" // WA Government blue
        };

        // Map template fields to Google Wallet modules
        foreach (var field in walletTemplate.Fields)
        {
            if (data.TryGetValue(field.Name, out var value))
            {
                var valueStr = value?.ToString() ?? string.Empty;

                if (field.IsRequired || field.DisplayOrder < 5)
                {
                    options.TextModulesData[field.Label] = valueStr;
                }
                else
                {
                    options.InfoModulesData[field.Label] = valueStr;
                }
            }
        }

        return options;
    }

    private WebWalletOptions CreateWebWalletOptions(WalletTemplate walletTemplate, Dictionary<string, object> data)
    {
        return new WebWalletOptions
        {
            Theme = "light",
            PrimaryColor = "#003087", // WA Government blue
            SecondaryColor = "#F0F0F0",
            EnableQrCode = true,
            EnableSharing = true,
            EnablePrinting = true,
            EnableOfflineMode = true,
            Metadata = data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
        };
    }
}