using Microsoft.Extensions.Logging;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Services;

/// <summary>
/// POA-200: Wallet template builder service
/// Creates platform-specific wallet templates for different credential types
/// </summary>
public interface IWalletTemplateBuilder
{
    Task<WalletTemplate> CreateTemplateAsync(
        string name,
        WalletTemplateType type,
        Dictionary<string, object> configuration,
        CancellationToken cancellationToken = default);

    Task<WalletTemplate> CreateFromPresetAsync(
        string presetName,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<WalletTemplatePreset>> GetAvailablePresetsAsync(
        CancellationToken cancellationToken = default);

    Task<WalletPackageDto> BuildWalletPackageAsync(
        Guid templateId,
        Guid walletId,
        CancellationToken cancellationToken = default);
}

public class WalletTemplateBuilder : IWalletTemplateBuilder
{
    private readonly IWalletTemplateRepository _templateRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IPlatformWalletBuilder _platformBuilder;
    private readonly SharedKernel.Interfaces.ITenantService _tenantService;
    private readonly ILogger<WalletTemplateBuilder> _logger;

    private static readonly Dictionary<string, WalletTemplatePreset> Presets = new()
    {
        ["DriverLicense"] = new WalletTemplatePreset
        {
            Name = "Driver License",
            Type = WalletTemplateType.DriverLicense,
            Description = "Western Australia Driver License template",
            Configuration = new Dictionary<string, object>
            {
                ["title"] = "WA Driver License",
                ["backgroundColor"] = "#003366",
                ["foregroundColor"] = "#FFFFFF",
                ["logoUrl"] = "https://numbatwallet.wa.gov.au/images/wa-logo.png",
                ["fields"] = new[]
                {
                    new { key = "licenseNumber", label = "License Number", type = "text", required = true },
                    new { key = "fullName", label = "Full Name", type = "text", required = true },
                    new { key = "dateOfBirth", label = "Date of Birth", type = "date", required = true },
                    new { key = "address", label = "Address", type = "text", required = true },
                    new { key = "expiryDate", label = "Expiry Date", type = "date", required = true },
                    new { key = "class", label = "License Class", type = "text", required = true }
                },
                ["barcodeFormat"] = "PDF417",
                ["supportedPlatforms"] = new[] { "iOS", "Android", "Web" }
            }
        },
        ["Passport"] = new WalletTemplatePreset
        {
            Name = "Australian Passport",
            Type = WalletTemplateType.Passport,
            Description = "Australian Passport credential template",
            Configuration = new Dictionary<string, object>
            {
                ["title"] = "Australian Passport",
                ["backgroundColor"] = "#002B5C",
                ["foregroundColor"] = "#FFFFFF",
                ["logoUrl"] = "https://numbatwallet.wa.gov.au/images/au-passport.png",
                ["fields"] = new[]
                {
                    new { key = "passportNumber", label = "Passport Number", type = "text", required = true },
                    new { key = "surname", label = "Surname", type = "text", required = true },
                    new { key = "givenNames", label = "Given Names", type = "text", required = true },
                    new { key = "nationality", label = "Nationality", type = "text", required = true },
                    new { key = "dateOfBirth", label = "Date of Birth", type = "date", required = true },
                    new { key = "placeOfBirth", label = "Place of Birth", type = "text", required = true },
                    new { key = "dateOfIssue", label = "Date of Issue", type = "date", required = true },
                    new { key = "dateOfExpiry", label = "Date of Expiry", type = "date", required = true }
                },
                ["barcodeFormat"] = "MRZ",
                ["supportedPlatforms"] = new[] { "iOS", "Android", "Web" }
            }
        },
        ["StudentId"] = new WalletTemplatePreset
        {
            Name = "Student ID Card",
            Type = WalletTemplateType.StudentId,
            Description = "University Student ID credential template",
            Configuration = new Dictionary<string, object>
            {
                ["title"] = "Student ID",
                ["backgroundColor"] = "#4A148C",
                ["foregroundColor"] = "#FFFFFF",
                ["logoUrl"] = "https://numbatwallet.wa.gov.au/images/university.png",
                ["fields"] = new[]
                {
                    new { key = "studentId", label = "Student ID", type = "text", required = true },
                    new { key = "fullName", label = "Full Name", type = "text", required = true },
                    new { key = "university", label = "University", type = "text", required = true },
                    new { key = "faculty", label = "Faculty", type = "text", required = true },
                    new { key = "course", label = "Course", type = "text", required = true },
                    new { key = "validFrom", label = "Valid From", type = "date", required = true },
                    new { key = "validUntil", label = "Valid Until", type = "date", required = true }
                },
                ["barcodeFormat"] = "QR_CODE",
                ["supportedPlatforms"] = new[] { "iOS", "Android", "Web" },
                ["features"] = new[] { "nfc", "photo" }
            }
        },
        ["ProofOfAge"] = new WalletTemplatePreset
        {
            Name = "Proof of Age Card",
            Type = WalletTemplateType.ProofOfAge,
            Description = "WA Proof of Age Card template",
            Configuration = new Dictionary<string, object>
            {
                ["title"] = "WA Proof of Age",
                ["backgroundColor"] = "#006644",
                ["foregroundColor"] = "#FFFFFF",
                ["logoUrl"] = "https://numbatwallet.wa.gov.au/images/wa-poa.png",
                ["fields"] = new[]
                {
                    new { key = "cardNumber", label = "Card Number", type = "text", required = true },
                    new { key = "fullName", label = "Full Name", type = "text", required = true },
                    new { key = "dateOfBirth", label = "Date of Birth", type = "date", required = true },
                    new { key = "photo", label = "Photo", type = "image", required = true },
                    new { key = "signature", label = "Signature", type = "image", required = false },
                    new { key = "issueDate", label = "Issue Date", type = "date", required = true },
                    new { key = "expiryDate", label = "Expiry Date", type = "date", required = true }
                },
                ["barcodeFormat"] = "PDF417",
                ["supportedPlatforms"] = new[] { "iOS", "Android", "Web" },
                ["ageVerification"] = true
            }
        },
        ["HealthCard"] = new WalletTemplatePreset
        {
            Name = "Medicare Card",
            Type = WalletTemplateType.HealthCard,
            Description = "Australian Medicare Card template",
            Configuration = new Dictionary<string, object>
            {
                ["title"] = "Medicare Card",
                ["backgroundColor"] = "#00A651",
                ["foregroundColor"] = "#FFFFFF",
                ["logoUrl"] = "https://numbatwallet.wa.gov.au/images/medicare.png",
                ["fields"] = new[]
                {
                    new { key = "medicareNumber", label = "Medicare Number", type = "text", required = true },
                    new { key = "individualRefNumber", label = "IRN", type = "text", required = true },
                    new { key = "fullName", label = "Name", type = "text", required = true },
                    new { key = "validTo", label = "Valid To", type = "date", required = true }
                },
                ["barcodeFormat"] = "CODE128",
                ["supportedPlatforms"] = new[] { "iOS", "Android", "Web" },
                ["privacy"] = "high"
            }
        }
    };

    public WalletTemplateBuilder(
        IWalletTemplateRepository templateRepository,
        IWalletRepository walletRepository,
        IPlatformWalletBuilder platformBuilder,
        SharedKernel.Interfaces.ITenantService tenantService,
        ILogger<WalletTemplateBuilder> logger)
    {
        _templateRepository = templateRepository;
        _walletRepository = walletRepository;
        _platformBuilder = platformBuilder;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<WalletTemplate> CreateTemplateAsync(
        string name,
        WalletTemplateType type,
        Dictionary<string, object> configuration,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating wallet template: {Name} of type {Type}", name, type);

        var tenantId = _tenantService.TenantId;
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        // Check if template already exists
        if (await _templateRepository.ExistsAsync(name, tenantId, cancellationToken))
        {
            throw new InvalidOperationException($"Template '{name}' already exists for this tenant");
        }

        // Create the template
        var template = new WalletTemplate(
            tenantId: tenantId,
            name: name,
            description: $"Template for {type}",
            type: type,
            createdBy: "System");

        // Add configuration as metadata
        foreach (var kvp in configuration)
        {
            template.UpdateMetadata(kvp.Key, kvp.Value);
        }

        // Save to repository
        var saved = await _templateRepository.AddAsync(template, cancellationToken);

        _logger.LogInformation("Created wallet template: {TemplateId}", saved.Id);
        return saved;
    }

    public async Task<WalletTemplate> CreateFromPresetAsync(
        string presetName,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating wallet template from preset: {PresetName}", presetName);

        if (!Presets.TryGetValue(presetName, out var preset))
        {
            throw new ArgumentException($"Preset '{presetName}' not found");
        }

        // Check if template already exists
        var templateName = $"{preset.Name} ({tenantId})";
        if (await _templateRepository.ExistsAsync(templateName, tenantId, cancellationToken))
        {
            // Return existing template
            return (await _templateRepository.GetByNameAsync(templateName, tenantId, cancellationToken))!;
        }

        // Create template from preset
        var template = new WalletTemplate(
            tenantId: tenantId,
            name: templateName,
            description: preset.Description,
            type: preset.Type,
            createdBy: "System");

        // Add configuration as metadata
        foreach (var kvp in preset.Configuration)
        {
            template.UpdateMetadata(kvp.Key, kvp.Value);
        }

        // Activate by default
        template.Activate();

        // Save to repository
        var saved = await _templateRepository.AddAsync(template, cancellationToken);

        _logger.LogInformation("Created wallet template from preset: {TemplateId}", saved.Id);
        return saved;
    }

    public Task<IEnumerable<WalletTemplatePreset>> GetAvailablePresetsAsync(
        CancellationToken cancellationToken = default)
    {
        var presets = Presets.Values.AsEnumerable();
        return Task.FromResult(presets);
    }

    public async Task<WalletPackageDto> BuildWalletPackageAsync(
        Guid templateId,
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building wallet package for template {TemplateId} and wallet {WalletId}",
            templateId, walletId);

        // Get template
        var template = await _templateRepository.GetByIdAsync(templateId, cancellationToken);
        if (template == null)
        {
            throw new ArgumentException($"Template {templateId} not found");
        }

        // Get wallet
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);
        if (wallet == null)
        {
            throw new ArgumentException($"Wallet {walletId} not found");
        }

        // Build platform-specific packages
        var packages = new Dictionary<string, object>();

        // Get supported platforms from template metadata
        var supportedPlatforms = template.Metadata.ContainsKey("supportedPlatforms")
            ? (string[])template.Metadata["supportedPlatforms"]
            : new[] { "Web" };

        foreach (var platform in supportedPlatforms)
        {
            switch (platform.ToLowerInvariant())
            {
                case "ios":
                    packages["ios"] = await _platformBuilder.BuildAppleWalletAsync(
                        wallet, template, cancellationToken);
                    break;

                case "android":
                    packages["android"] = await _platformBuilder.BuildGoogleWalletAsync(
                        wallet, template, cancellationToken);
                    break;

                default:
                    packages["web"] = await _platformBuilder.BuildWebWalletAsync(
                        wallet, template, cancellationToken);
                    break;
            }
        }

        var walletPackage = new WalletPackageDto
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            TemplateName = template.Name,
            Platform = "Multi",
            PackageType = template.Type.ToString(),
            FileName = $"wallet-{walletId}.package",
            GeneratedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Data = packages,
            IsSuccess = true
        };

        _logger.LogInformation("Built wallet package with {Count} platform packages", packages.Count);
        return walletPackage;
    }
}

public class WalletTemplatePreset
{
    public string Name { get; set; } = string.Empty;
    public WalletTemplateType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
}