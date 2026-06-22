using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Infrastructure.WalletBuilders;

/// <summary>
/// Implementation of Google Wallet pass builder.
/// Uses RSA (RS256) signing with Google Service Account credentials.
/// </summary>
public class GoogleWalletBuilder : IGoogleWalletBuilder
{
    private readonly ILogger<GoogleWalletBuilder> _logger;
    private readonly IConfiguration _configuration;
    private const string GoogleWalletApiUrl = "https://walletobjects.googleapis.com/walletobjects/v1";
    private const string GoogleWalletSaveUrl = "https://pay.google.com/gp/v/save";

    public GoogleWalletBuilder(ILogger<GoogleWalletBuilder> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }


    public async Task<GoogleWalletPassDto> GenerateGooglePassAsync(
        WalletTemplate walletTemplate,
        Dictionary<string, object> data,
        GoogleWalletOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Google Wallet pass for template {TemplateId}", walletTemplate.Id);

        var passClass = CreatePassClass(options);
        var passObject = CreatePassObject(walletTemplate, data, options);

        var googlePass = new GoogleWalletPassDto
        {
            Id = options.ObjectId,
            ClassId = options.ClassId,
            State = "ACTIVE",
            ClassData = passClass,
            ObjectData = passObject
        };

        // Generate JWT for the pass
        googlePass.Jwt = await CreateJwtAsync(googlePass);
        googlePass.SaveUrl = GetAddToWalletLink(googlePass.Jwt);

        return googlePass;
    }

    public Task<string> CreateJwtAsync(GoogleWalletPassDto pass)
    {
        var serviceAccountEmail = _configuration["GoogleWallet:ServiceAccountEmail"]
            ?? throw new InvalidOperationException(
                "GoogleWallet:ServiceAccountEmail must be configured. " +
                "Set it to the service account email from your Google Cloud Console.");

        var issuerId = _configuration["GoogleWallet:IssuerId"]
            ?? throw new InvalidOperationException(
                "GoogleWallet:IssuerId must be configured. " +
                "Set it to the issuer ID from Google Pay & Wallet Console.");

        // Build the JWT payload with pass data
        var genericObject = new Dictionary<string, object>
        {
            ["id"] = $"{pass.ClassId}.{pass.Id}",
            ["classId"] = pass.ClassId,
            ["genericType"] = "GENERIC_TYPE_UNSPECIFIED",
            ["state"] = pass.State,
            ["header"] = pass.ObjectData.GetValueOrDefault("header", new Dictionary<string, string>()),
            ["textModulesData"] = pass.ObjectData.GetValueOrDefault("textModulesData", new List<object>()),
            ["barcode"] = pass.ObjectData.GetValueOrDefault("barcode", new Dictionary<string, string>())
        };

        // Load RSA private key from service account JSON or PEM
        var rsaKey = LoadRsaPrivateKey();
        var securityKey = new RsaSecurityKey(rsaKey);
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = serviceAccountEmail,
            Audience = "google",
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = signingCredentials,
            Claims = new Dictionary<string, object>
            {
                ["typ"] = "savetowallet",
                ["origins"] = new[] { "https://numbatwallet.wa.gov.au" },
                ["payload"] = new Dictionary<string, object>
                {
                    ["genericObjects"] = new[] { genericObject }
                }
            }
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        _logger.LogDebug("Generated RS256 JWT for Google Wallet pass {PassId}", pass.Id);
        return Task.FromResult(jwt);
    }

    private RSA LoadRsaPrivateKey()
    {
        // Option 1: Load from Google Service Account JSON file
        var serviceAccountKeyPath = _configuration["GoogleWallet:ServiceAccountKeyPath"];
        if (!string.IsNullOrEmpty(serviceAccountKeyPath) && File.Exists(serviceAccountKeyPath))
        {
            var json = File.ReadAllText(serviceAccountKeyPath);
            var keyData = JsonSerializer.Deserialize<JsonElement>(json);

            if (keyData.TryGetProperty("private_key", out var privateKeyElement))
            {
                var privateKeyPem = privateKeyElement.GetString()
                    ?? throw new InvalidOperationException("Service account JSON 'private_key' is empty.");

                var rsa = RSA.Create();
                rsa.ImportFromPem(privateKeyPem);
                return rsa;
            }

            throw new InvalidOperationException("Service account JSON does not contain 'private_key'.");
        }

        // Option 2: Load from PEM string in config (e.g., from Azure Key Vault)
        var privateKey = _configuration["GoogleWallet:PrivateKeyPem"];
        if (!string.IsNullOrEmpty(privateKey))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(privateKey);
            return rsa;
        }

        throw new InvalidOperationException(
            "Google Wallet RSA private key not configured. " +
            "Set either GoogleWallet:ServiceAccountKeyPath (path to JSON) or GoogleWallet:PrivateKeyPem (PEM string).");
    }

    public string GetAddToWalletLink(string jwt)
    {
        return $"{GoogleWalletSaveUrl}/{jwt}";
    }

    public bool IsTemplateCompatible(WalletTemplate walletTemplate, WalletPlatform platform)
    {
        if (platform != WalletPlatform.GoogleWallet && platform != WalletPlatform.All)
        {
            return false;
        }

        // Check if template has required fields for Google Wallet
        return walletTemplate.Fields.Any() && !string.IsNullOrEmpty(walletTemplate.Name);
    }

    public PlatformRequirementsDto GetPlatformRequirements(WalletPlatform platform, WalletTemplateType templateType)
    {
        if (platform != WalletPlatform.GoogleWallet)
        {
            return new PlatformRequirementsDto { Platform = platform.ToString() };
        }

        return new PlatformRequirementsDto
        {
            Platform = "GoogleWallet",
            RequiredFields = new List<RequiredFieldDto>
            {
                new() { FieldName = "issuerId", FieldType = "string", IsMandatory = true, Description = "Google Wallet Issuer ID" },
                new() { FieldName = "classId", FieldType = "string", IsMandatory = true, Description = "Pass class identifier" },
                new() { FieldName = "header", FieldType = "string", IsMandatory = true, Description = "Pass header text" }
            },
            RequiredAssets = new List<RequiredAssetDto>
            {
                new() { AssetName = "logo", AssetType = "PNG/JPG", MimeType = "image/*", Width = 660, Height = 660, MaxSizeKb = 500, IsMandatory = false },
                new() { AssetName = "heroImage", AssetType = "PNG/JPG", MimeType = "image/*", Width = 1032, Height = 336, MaxSizeKb = 1000, IsMandatory = false }
            },
            Constraints = new Dictionary<string, string>
            {
                ["maxTextModules"] = "20",
                ["maxInfoModules"] = "20",
                ["maxImageModules"] = "10",
                ["supportedBarcodes"] = "QR_CODE,AZTEC,PDF_417,CODE_128,CODE_39,EAN_13,EAN_8,UPC_A"
            },
            SupportedFeatures = new List<string> { "barcode", "nfc", "smartTap", "messages", "locations", "save_to_android_pay" }
        };
    }

    private Dictionary<string, object> CreatePassClass(GoogleWalletOptions options)
    {
        return new Dictionary<string, object>
        {
            ["id"] = options.ClassId,
            ["classTemplateInfo"] = new Dictionary<string, object>
            {
                ["cardTemplateOverride"] = new Dictionary<string, object>
                {
                    ["cardRowTemplateInfos"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["twoItems"] = new Dictionary<string, object>
                            {
                                ["startItem"] = new Dictionary<string, object>
                                {
                                    ["firstValue"] = new Dictionary<string, object>
                                    {
                                        ["fields"] = new[]
                                        {
                                            new Dictionary<string, object>
                                            {
                                                ["fieldPath"] = "object.textModulesData['field1']"
                                            }
                                        }
                                    }
                                },
                                ["endItem"] = new Dictionary<string, object>
                                {
                                    ["firstValue"] = new Dictionary<string, object>
                                    {
                                        ["fields"] = new[]
                                        {
                                            new Dictionary<string, object>
                                            {
                                                ["fieldPath"] = "object.textModulesData['field2']"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private Dictionary<string, object> CreatePassObject(WalletTemplate walletTemplate, Dictionary<string, object> data, GoogleWalletOptions options)
    {
        var textModules = new List<Dictionary<string, object>>();
        var infoModules = new List<Dictionary<string, object>>();

        // Map template fields to Google Wallet modules
        foreach (var field in walletTemplate.Fields.OrderBy(f => f.DisplayOrder))
        {
            if (data.TryGetValue(field.Name, out var value))
            {
                if (field.DisplayOrder < 5)
                {
                    // Primary fields go to text modules
                    textModules.Add(new Dictionary<string, object>
                    {
                        ["header"] = field.Label,
                        ["body"] = value?.ToString() ?? string.Empty,
                        ["id"] = field.Name
                    });
                }
                else
                {
                    // Secondary fields go to info modules
                    infoModules.Add(new Dictionary<string, object>
                    {
                        ["labelValueRows"] = new[]
                        {
                            new Dictionary<string, object>
                            {
                                ["columns"] = new[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        ["label"] = field.Label,
                                        ["value"] = value?.ToString() ?? string.Empty
                                    }
                                }
                            }
                        }
                    });
                }
            }
        }

        // Add custom modules from options
        foreach (var kvp in options.TextModulesData)
        {
            textModules.Add(new Dictionary<string, object>
            {
                ["header"] = kvp.Key,
                ["body"] = kvp.Value,
                ["id"] = kvp.Key.ToLowerInvariant().Replace(" ", "_")
            });
        }

        var passObject = new Dictionary<string, object>
        {
            ["id"] = options.ObjectId,
            ["classId"] = options.ClassId,
            ["state"] = "ACTIVE",
            ["header"] = new Dictionary<string, object>
            {
                ["title"] = new Dictionary<string, object>
                {
                    ["defaultValue"] = new Dictionary<string, object>
                    {
                        ["language"] = "en",
                        ["value"] = options.Header
                    }
                },
                ["subtitle"] = new Dictionary<string, object>
                {
                    ["defaultValue"] = new Dictionary<string, object>
                    {
                        ["language"] = "en",
                        ["value"] = options.Subheader
                    }
                }
            },
            ["textModulesData"] = textModules,
            ["infoModulesData"] = infoModules
        };

        // Add barcode if provided
        if (!string.IsNullOrEmpty(options.BarcodeValue))
        {
            passObject["barcode"] = new Dictionary<string, object>
            {
                ["type"] = options.BarcodeType,
                ["value"] = options.BarcodeValue,
                ["alternateText"] = options.BarcodeAlternateText
            };
        }

        // Add logo if provided
        if (!string.IsNullOrEmpty(options.Logo))
        {
            passObject["logo"] = new Dictionary<string, object>
            {
                ["sourceUri"] = new Dictionary<string, object>
                {
                    ["uri"] = options.Logo
                }
            };
        }

        // Set background color
        passObject["hexBackgroundColor"] = options.HexBackgroundColor;

        return passObject;
    }

    private GoogleWalletOptions CreateDefaultOptions(WalletTemplate walletTemplate)
    {
        var issuerId = _configuration["GoogleWallet:IssuerId"] ?? "3388000000022297348";
        var classId = $"{issuerId}.{walletTemplate.Type.ToString().ToLowerInvariant()}_{walletTemplate.Id}";
        var objectId = $"{classId}_{Guid.NewGuid():N}";

        return new GoogleWalletOptions
        {
            IssuerId = issuerId,
            ClassId = classId,
            ObjectId = objectId,
            HexBackgroundColor = "#003366",
            CardTitle = walletTemplate.Name,
            Header = walletTemplate.Name,
            Subheader = "Government of Western Australia",
            BarcodeType = "QR_CODE",
            BarcodeValue = $"wallet://{walletTemplate.Id}",
            BarcodeAlternateText = $"Scan to verify {walletTemplate.Name}"
        };
    }
}