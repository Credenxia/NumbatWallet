using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Infrastructure.WalletBuilders;

/// <summary>
/// Implementation of Apple Wallet (.pkpass) builder
/// </summary>
public class AppleWalletBuilder : IAppleWalletBuilder
{
    private readonly ILogger<AppleWalletBuilder> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AppleWalletBuilder(ILogger<AppleWalletBuilder> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }


    public async Task<byte[]> GeneratePkpassAsync(
        WalletTemplate walletTemplate,
        Dictionary<string, object> data,
        AppleWalletOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating pkpass for template {TemplateId}", walletTemplate.Id);

        // Create pass.json structure
        var passJson = CreatePassJson(walletTemplate, data, options);

        // Create manifest.json
        var manifest = new Dictionary<string, string>();

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Add pass.json
            var passJsonBytes = Encoding.UTF8.GetBytes(passJson);
            await AddFileToArchive(archive, "pass.json", passJsonBytes);
            manifest["pass.json"] = ComputeSha1(passJsonBytes);

            // Add images if provided
            if (options.Logo != null)
            {
                await AddFileToArchive(archive, "logo.png", options.Logo);
                manifest["logo.png"] = ComputeSha1(options.Logo);
            }

            if (options.Icon != null)
            {
                await AddFileToArchive(archive, "icon.png", options.Icon);
                manifest["icon.png"] = ComputeSha1(options.Icon);
            }

            if (options.Thumbnail != null)
            {
                await AddFileToArchive(archive, "thumbnail.png", options.Thumbnail);
                manifest["thumbnail.png"] = ComputeSha1(options.Thumbnail);
            }

            // Add manifest.json
            var manifestJson = JsonSerializer.Serialize(manifest, _jsonOptions);
            var manifestBytes = Encoding.UTF8.GetBytes(manifestJson);
            await AddFileToArchive(archive, "manifest.json", manifestBytes);

            // TODO: Add signature file (requires Apple Developer certificates)
            // For now, we'll add a placeholder
            var signaturePlaceholder = Encoding.UTF8.GetBytes("SIGNATURE_PLACEHOLDER");
            await AddFileToArchive(archive, "signature", signaturePlaceholder);
        }

        memoryStream.Position = 0;
        return memoryStream.ToArray();
    }

    public Task<byte[]> SignPassAsync(byte[] passData, string certificatePath, string password)
    {
        // TODO: Implement proper PKCS#7 signing with Apple certificates
        // This requires:
        // 1. Apple Developer account
        // 2. Pass Type ID certificate
        // 3. WWDR (Apple Worldwide Developer Relations) certificate

        _logger.LogWarning("Pass signing not implemented - returning unsigned pass");
        return Task.FromResult(passData);
    }

    public bool ValidatePassJson(string passJson)
    {
        try
        {
            var passObject = JsonSerializer.Deserialize<Dictionary<string, object>>(passJson);
            if (passObject == null)
            {
                return false;
            }

            // Check required fields
            var requiredFields = new[] { "formatVersion", "passTypeIdentifier", "serialNumber", "teamIdentifier", "organizationName", "description" };
            foreach (var field in requiredFields)
            {
                if (!passObject.ContainsKey(field))
                {
                    _logger.LogWarning("Missing required field: {Field}", field);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid pass.json structure");
            return false;
        }
    }

    public bool IsTemplateCompatible(WalletTemplate walletTemplate, WalletPlatform platform)
    {
        if (platform != WalletPlatform.AppleWallet && platform != WalletPlatform.All)
        {
            return false;
        }

        // Check if template has required fields for Apple Wallet
        return walletTemplate.Fields.Any() && !string.IsNullOrEmpty(walletTemplate.Name);
    }

    public PlatformRequirementsDto GetPlatformRequirements(WalletPlatform platform, WalletTemplateType templateType)
    {
        if (platform != WalletPlatform.AppleWallet)
        {
            return new PlatformRequirementsDto { Platform = platform.ToString() };
        }

        return new PlatformRequirementsDto
        {
            Platform = "AppleWallet",
            RequiredFields = new List<RequiredFieldDto>
            {
                new() { FieldName = "serialNumber", FieldType = "string", IsMandatory = true, Description = "Unique serial number for the pass" },
                new() { FieldName = "organizationName", FieldType = "string", IsMandatory = true, Description = "Organization name" },
                new() { FieldName = "description", FieldType = "string", IsMandatory = true, Description = "Pass description" }
            },
            RequiredAssets = new List<RequiredAssetDto>
            {
                new() { AssetName = "icon", AssetType = "PNG", MimeType = "image/png", Width = 29, Height = 29, IsMandatory = true },
                new() { AssetName = "logo", AssetType = "PNG", MimeType = "image/png", Width = 160, Height = 50, IsMandatory = false },
                new() { AssetName = "thumbnail", AssetType = "PNG", MimeType = "image/png", Width = 90, Height = 90, IsMandatory = false }
            },
            Constraints = new Dictionary<string, string>
            {
                ["maxFileSize"] = "10MB",
                ["supportedBarcodes"] = "QR,PDF417,Aztec,Code128",
                ["maxFields"] = "100"
            },
            SupportedFeatures = new List<string> { "barcode", "nfc", "location", "relevantDate", "push_updates" }
        };
    }

    private string CreatePassJson(WalletTemplate walletTemplate, Dictionary<string, object> data, AppleWalletOptions options)
    {
        var passType = GetPassType(walletTemplate.Type);

        var pass = new Dictionary<string, object>
        {
            ["formatVersion"] = 1,
            ["passTypeIdentifier"] = options.PassTypeIdentifier,
            ["serialNumber"] = Guid.NewGuid().ToString(),
            ["teamIdentifier"] = options.TeamIdentifier,
            ["organizationName"] = options.OrganizationName,
            ["description"] = options.Description,
            ["logoText"] = options.LogoText,
            ["foregroundColor"] = options.ForegroundColor,
            ["backgroundColor"] = options.BackgroundColor,
            ["labelColor"] = options.LabelColor
        };

        // Add barcode if enabled
        if (options.EnableBarcode)
        {
            pass["barcode"] = new Dictionary<string, object>
            {
                ["format"] = options.BarcodeFormat,
                ["message"] = options.BarcodeMessage,
                ["messageEncoding"] = "iso-8859-1"
            };
        }

        // Create the appropriate pass structure based on type
        var passStructure = new Dictionary<string, object>();

        if (options.HeaderFields.Any())
        {
            passStructure["headerFields"] = options.HeaderFields.Select(f => ConvertToPassField(f)).ToList();
        }

        if (options.PrimaryFields.Any())
        {
            passStructure["primaryFields"] = options.PrimaryFields.Select(f => ConvertToPassField(f)).ToList();
        }

        if (options.SecondaryFields.Any())
        {
            passStructure["secondaryFields"] = options.SecondaryFields.Select(f => ConvertToPassField(f)).ToList();
        }

        if (options.AuxiliaryFields.Any())
        {
            passStructure["auxiliaryFields"] = options.AuxiliaryFields.Select(f => ConvertToPassField(f)).ToList();
        }

        if (options.BackFields.Any())
        {
            passStructure["backFields"] = options.BackFields.Select(f => ConvertToPassField(f)).ToList();
        }

        // Map template fields to pass fields
        foreach (var field in walletTemplate.Fields)
        {
            if (data.TryGetValue(field.Name, out var value))
            {
                var passField = new AppleWalletField
                {
                    Key = field.Name,
                    Label = field.Label,
                    Value = value?.ToString() ?? string.Empty
                };

                // Determine which section to add the field to based on display order
                if (field.DisplayOrder < 2)
                {
                    options.PrimaryFields.Add(passField);
                }
                else if (field.DisplayOrder < 5)
                {
                    options.SecondaryFields.Add(passField);
                }
                else
                {
                    options.AuxiliaryFields.Add(passField);
                }
            }
        }

        pass[passType] = passStructure;

        return JsonSerializer.Serialize(pass, _jsonOptions);
    }

    private Dictionary<string, object> ConvertToPassField(AppleWalletField field)
    {
        var result = new Dictionary<string, object>
        {
            ["key"] = field.Key,
            ["label"] = field.Label,
            ["value"] = field.Value
        };

        if (!string.IsNullOrEmpty(field.ChangeMessage))
        {
            result["changeMessage"] = field.ChangeMessage;
        }

        if (!string.IsNullOrEmpty(field.TextAlignment))
        {
            result["textAlignment"] = field.TextAlignment;
        }

        return result;
    }

    private string GetPassType(WalletTemplateType templateType)
    {
        return templateType switch
        {
            WalletTemplateType.DriverLicense => "generic",
            WalletTemplateType.Passport => "generic",
            WalletTemplateType.StudentId => "storeCard",
            WalletTemplateType.HealthCard => "generic",
            WalletTemplateType.ProofOfAge => "generic",
            WalletTemplateType.VaccinationCertificate => "generic",
            WalletTemplateType.WorkingWithChildren => "generic",
            _ => "generic"
        };
    }

    private AppleWalletOptions CreateDefaultOptions(WalletTemplate walletTemplate)
    {
        return new AppleWalletOptions
        {
            TeamIdentifier = "DUMMY_TEAM", // TODO: Get from configuration
            PassTypeIdentifier = $"pass.au.gov.wa.numbatwallet.{walletTemplate.Type.ToString().ToLowerInvariant()}",
            OrganizationName = "Government of Western Australia",
            Description = walletTemplate.Description,
            BackgroundColor = "#003366",
            ForegroundColor = "#FFFFFF",
            LabelColor = "#CCCCCC",
            LogoText = "WA Government",
            EnableBarcode = true,
            BarcodeFormat = "PKBarcodeFormatQR",
            BarcodeMessage = $"wallet://{walletTemplate.Id}"
        };
    }

    private async Task AddFileToArchive(ZipArchive archive, string fileName, byte[] data)
    {
        var entry = archive.CreateEntry(fileName);
        using var stream = entry.Open();
        await stream.WriteAsync(data.AsMemory(0, data.Length));
    }

    private string ComputeSha1(byte[] data)
    {
        // SHA1 is required by Apple Wallet specification for manifest
        #pragma warning disable CA5350 // SHA1 required by Apple spec
        var hash = SHA1.HashData(data);
        #pragma warning restore CA5350
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}