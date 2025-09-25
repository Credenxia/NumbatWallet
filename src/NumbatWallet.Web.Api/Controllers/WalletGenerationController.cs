using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Web.Api.Security;

namespace NumbatWallet.Web.Api.Controllers;

/// <summary>
/// Controller for generating platform-specific wallet packages
/// </summary>
[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/wallet-generation")]
[Microsoft.AspNetCore.Authorization.Authorize]
[Produces("application/json")]
public class WalletGenerationController : ControllerBase
{
    private readonly IWalletTemplateRepository _templateRepository;
    private readonly IPlatformWalletBuilder _platformWalletBuilder;
    private readonly IAppleWalletBuilder _appleWalletBuilder;
    private readonly IGoogleWalletBuilder _googleWalletBuilder;
    private readonly IWebWalletBuilder _webWalletBuilder;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<WalletGenerationController> _logger;

    public WalletGenerationController(
        IWalletTemplateRepository templateRepository,
        IPlatformWalletBuilder platformWalletBuilder,
        IAppleWalletBuilder appleWalletBuilder,
        IGoogleWalletBuilder googleWalletBuilder,
        IWebWalletBuilder webWalletBuilder,
        ISecurityAuditService auditService,
        ILogger<WalletGenerationController> logger)
    {
        _templateRepository = templateRepository;
        _platformWalletBuilder = platformWalletBuilder;
        _appleWalletBuilder = appleWalletBuilder;
        _googleWalletBuilder = googleWalletBuilder;
        _webWalletBuilder = webWalletBuilder;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a wallet package for a specific platform
    /// </summary>
    [HttpPost("generate/{platform}")]
    [ProducesResponseType(typeof(WalletPackageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateWallet(
        string platform,
        [FromBody] GenerateWalletRequest request)
    {
        _logger.LogInformation("Generating {Platform} wallet for template {TemplateId}",
            platform, request.TemplateId);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.DataAccess,
            $"Wallet generation for platform {platform}");

        // Get the template
        var template = await _templateRepository.GetByIdAsync(request.TemplateId);
        if (template == null)
        {
            return NotFound($"Template {request.TemplateId} not found");
        }

        // Parse platform
        if (!Enum.TryParse<WalletPlatform>(platform, true, out var walletPlatform))
        {
            return BadRequest($"Invalid platform: {platform}");
        }

        // Generate based on platform
        WalletPackageDto result;
        try
        {
            // Use the platform wallet builder which can handle all platforms
            result = await _platformWalletBuilder.BuildWalletPackageAsync(
                template, request.Data, walletPlatform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate {Platform} wallet", platform);
            return StatusCode(500, "Failed to generate wallet package");
        }

        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result);
    }

    /// <summary>
    /// Generate Apple Wallet (.pkpass) file
    /// </summary>
    [HttpPost("apple/{templateId}")]
    [Produces("application/vnd.apple.pkpass")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateAppleWallet(
        Guid templateId,
        [FromBody] Dictionary<string, object> data)
    {
        _logger.LogInformation("Generating Apple Wallet for template {TemplateId}", templateId);

        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null)
        {
            return NotFound($"Template {templateId} not found");
        }

        var options = new AppleWalletOptions
        {
            TeamIdentifier = HttpContext.Request.Headers["X-Apple-Team-Id"].FirstOrDefault() ?? "DUMMY",
            PassTypeIdentifier = $"pass.au.gov.wa.numbatwallet.{template.Type.ToString().ToLowerInvariant()}",
            OrganizationName = "Government of Western Australia",
            Description = template.Description,
            LogoText = template.Name
        };

        var pkpassData = await _appleWalletBuilder.GeneratePkpassAsync(template, data, options);

        return File(pkpassData, "application/vnd.apple.pkpass",
            $"{template.Name.Replace(" ", "_")}.pkpass");
    }

    /// <summary>
    /// Generate Google Wallet pass
    /// </summary>
    [HttpPost("google/{templateId}")]
    [ProducesResponseType(typeof(GoogleWalletResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateGoogleWallet(
        Guid templateId,
        [FromBody] Dictionary<string, object> data)
    {
        _logger.LogInformation("Generating Google Wallet for template {TemplateId}", templateId);

        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null)
        {
            return NotFound($"Template {templateId} not found");
        }

        var options = new GoogleWalletOptions
        {
            IssuerId = Configuration["GoogleWallet:IssuerId"] ?? "3388000000022297348",
            ClassId = $"numbatwallet_{template.Type.ToString().ToLowerInvariant()}",
            ObjectId = Guid.NewGuid().ToString("N"),
            CardTitle = template.Name,
            Header = template.Name,
            Subheader = "Government of Western Australia"
        };

        var googlePass = await _googleWalletBuilder.GenerateGooglePassAsync(template, data, options);
        var jwt = await _googleWalletBuilder.CreateJwtAsync(googlePass);
        var saveUrl = _googleWalletBuilder.GetAddToWalletLink(jwt);

        return Ok(new GoogleWalletResponseDto
        {
            Jwt = jwt,
            SaveUrl = saveUrl,
            PassId = googlePass.Id,
            ClassId = googlePass.ClassId
        });
    }

    /// <summary>
    /// Generate Web/PWA wallet
    /// </summary>
    [HttpPost("web/{templateId}")]
    [ProducesResponseType(typeof(WebWalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateWebWallet(
        Guid templateId,
        [FromBody] GenerateWebWalletRequest request)
    {
        _logger.LogInformation("Generating Web wallet for template {TemplateId}", templateId);

        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null)
        {
            return NotFound($"Template {templateId} not found");
        }

        var options = new WebWalletOptions
        {
            Theme = request.Theme ?? "light",
            PrimaryColor = request.PrimaryColor ?? "#003366",
            EnableQrCode = request.EnableQrCode ?? true,
            EnableSharing = request.EnableSharing ?? true,
            EnableOfflineMode = request.EnableOfflineMode ?? false
        };

        var webWallet = await _webWalletBuilder.GenerateWebWalletAsync(
            template, request.Data, options);

        return Ok(webWallet);
    }

    /// <summary>
    /// Preview wallet for a specific platform
    /// </summary>
    [HttpGet("preview/{templateId}/{platform}")]
    [ProducesResponseType(typeof(WalletPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewWallet(
        Guid templateId,
        string platform)
    {
        _logger.LogInformation("Previewing {Platform} wallet for template {TemplateId}",
            platform, templateId);

        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null)
        {
            return NotFound($"Template {templateId} not found");
        }

        if (!Enum.TryParse<WalletPlatform>(platform, true, out var walletPlatform))
        {
            return BadRequest($"Invalid platform: {platform}");
        }

        // Get platform requirements
        var requirements = _platformWalletBuilder.GetPlatformRequirements(walletPlatform, template.Type);

        // Check compatibility
        var isCompatible = _platformWalletBuilder.IsTemplateCompatible(template, walletPlatform);

        return Ok(new WalletPreviewDto
        {
            TemplateId = templateId,
            TemplateName = template.Name,
            Platform = platform,
            IsCompatible = isCompatible,
            Requirements = requirements,
            SampleData = GenerateSampleData(template)
        });
    }

    /// <summary>
    /// Get supported platforms for a template
    /// </summary>
    [HttpGet("supported-platforms/{templateId}")]
    [ProducesResponseType(typeof(SupportedPlatformsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSupportedPlatforms(Guid templateId)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null)
        {
            return NotFound($"Template {templateId} not found");
        }

        var platforms = new List<PlatformSupportDto>();

        foreach (WalletPlatform platform in Enum.GetValues<WalletPlatform>())
        {
            if (platform == WalletPlatform.All)
            {
                continue;
            }

            var isSupported = _platformWalletBuilder.IsTemplateCompatible(template, platform);

            platforms.Add(new PlatformSupportDto
            {
                Platform = platform.ToString(),
                IsSupported = isSupported,
                Requirements = isSupported ? GetPlatformRequirements(platform, template.Type) : null
            });
        }

        return Ok(new SupportedPlatformsDto
        {
            TemplateId = templateId,
            Platforms = platforms
        });
    }

    private IConfiguration Configuration => HttpContext.RequestServices.GetRequiredService<IConfiguration>();

    private Dictionary<string, object> GenerateSampleData(WalletTemplate template)
    {
        var sampleData = new Dictionary<string, object>();

        foreach (var field in template.Fields)
        {
            sampleData[field.Name] = field.FieldType switch
            {
                "text" => "Sample Text",
                "number" => "12345",
                "date" => DateTime.UtcNow.ToString("yyyy-MM-dd"),
                "email" => "sample@example.com",
                "phone" => "+61 400 000 000",
                _ => "Sample Value"
            };
        }

        return sampleData;
    }

    private PlatformRequirementsDto? GetPlatformRequirements(WalletPlatform platform, WalletTemplateType templateType)
    {
        return _platformWalletBuilder.GetPlatformRequirements(platform, templateType);
    }
}

// Request DTOs
public class GenerateWalletRequest
{
    public Guid TemplateId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public Dictionary<string, string>? Options { get; set; }
}

public class GenerateWebWalletRequest
{
    public Dictionary<string, object> Data { get; set; } = new();
    public string? Theme { get; set; }
    public string? PrimaryColor { get; set; }
    public bool? EnableQrCode { get; set; }
    public bool? EnableSharing { get; set; }
    public bool? EnableOfflineMode { get; set; }
}

// Response DTOs
public class GoogleWalletResponseDto
{
    public required string Jwt { get; set; }
    public required string SaveUrl { get; set; }
    public required string PassId { get; set; }
    public required string ClassId { get; set; }
}

public class WalletPreviewDto
{
    public Guid TemplateId { get; set; }
    public required string TemplateName { get; set; }
    public required string Platform { get; set; }
    public bool IsCompatible { get; set; }
    public PlatformRequirementsDto? Requirements { get; set; }
    public Dictionary<string, object> SampleData { get; set; } = new();
}

public class SupportedPlatformsDto
{
    public Guid TemplateId { get; set; }
    public List<PlatformSupportDto> Platforms { get; set; } = new();
}

public class PlatformSupportDto
{
    public required string Platform { get; set; }
    public bool IsSupported { get; set; }
    public PlatformRequirementsDto? Requirements { get; set; }
}