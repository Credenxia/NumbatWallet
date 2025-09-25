using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
/// Implementation of Google Wallet pass builder
/// </summary>
public class GoogleWalletBuilder : IGoogleWalletBuilder
{
    private readonly ILogger<GoogleWalletBuilder> _logger;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string GoogleWalletApiUrl = "https://walletobjects.googleapis.com/walletobjects/v1";
    private const string GoogleWalletSaveUrl = "https://pay.google.com/gp/v/save";

    public GoogleWalletBuilder(ILogger<GoogleWalletBuilder> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }


    public async Task<GoogleWalletPassDto> GenerateGooglePassAsync(
        WalletTemplate walletTemplate,
        Dictionary<string, object> data,
        GoogleWalletOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Google Wallet pass for template {TemplateId}", walletTemplate.Id);

        var passClass = CreatePassClass(walletTemplate, options);
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
        // Create JWT payload
        var payload = new Dictionary<string, object>
        {
            ["iss"] = _configuration["GoogleWallet:ServiceAccountEmail"] ?? "dummy@example.com",
            ["aud"] = "google",
            ["typ"] = "savetowallet",
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["origins"] = new[] { "https://numbatwallet.wa.gov.au" }
        };

        // Add pass object and class
        payload["payload"] = new Dictionary<string, object>
        {
            ["genericObjects"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["id"] = $"{pass.ClassId}.{pass.Id}",
                    ["classId"] = pass.ClassId,
                    ["genericType"] = "GENERIC_TYPE_UNSPECIFIED",
                    ["state"] = pass.State,
                    ["header"] = pass.ObjectData.GetValueOrDefault("header", new Dictionary<string, string>()),
                    ["textModulesData"] = pass.ObjectData.GetValueOrDefault("textModulesData", new List<object>()),
                    ["barcode"] = pass.ObjectData.GetValueOrDefault("barcode", new Dictionary<string, string>())
                }
            }
        };

        // In production, this would use the actual service account private key
        // For now, we'll create a dummy JWT
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["GoogleWallet:PrivateKey"] ?? "dummy_key_for_development");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("iss", payload["iss"].ToString()!),
                new Claim("aud", payload["aud"].ToString()!),
                new Claim("typ", payload["typ"].ToString()!)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        _logger.LogDebug("Generated JWT for Google Wallet pass");
        return Task.FromResult(jwt);
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

    private Dictionary<string, object> CreatePassClass(WalletTemplate walletTemplate, GoogleWalletOptions options)
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