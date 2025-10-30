using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Infrastructure.WalletBuilders;
using System.IO.Compression;
using System.Text.Json;

namespace NumbatWallet.Infrastructure.Tests.WalletBuilders;

public class AppleWalletBuilderTests
{
    private readonly Mock<ILogger<AppleWalletBuilder>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AppleWalletBuilder _builder;

    public AppleWalletBuilderTests()
    {
        _mockLogger = new Mock<ILogger<AppleWalletBuilder>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup default configuration
        _mockConfiguration.Setup(x => x["AppleWallet:TeamIdentifier"]).Returns("TEST_TEAM_ID");
        _mockConfiguration.Setup(x => x["AppleWallet:WwdrCertificatePath"]).Returns(string.Empty);

        _builder = new AppleWalletBuilder(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task GeneratePkpassAsync_WithValidTemplate_ReturnsValidZipArchive()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        var result = await _builder.GeneratePkpassAsync(template, data, options);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        // Verify it's a valid ZIP archive
        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        archive.Entries.Should().NotBeEmpty();

        // Verify required files exist
        archive.Entries.Should().Contain(e => e.Name == "pass.json");
        archive.Entries.Should().Contain(e => e.Name == "manifest.json");
        archive.Entries.Should().Contain(e => e.Name == "signature");
    }

    [Fact]
    public async Task GeneratePkpassAsync_WithLogo_IncludesLogoInArchive()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.Logo = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        // Act
        var result = await _builder.GeneratePkpassAsync(template, data, options);

        // Assert
        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        archive.Entries.Should().Contain(e => e.Name == "logo.png");
    }

    [Fact]
    public async Task GeneratePkpassAsync_WithIcon_IncludesIconInArchive()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.Icon = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        // Act
        var result = await _builder.GeneratePkpassAsync(template, data, options);

        // Assert
        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        archive.Entries.Should().Contain(e => e.Name == "icon.png");
    }

    [Fact]
    public async Task GeneratePkpassAsync_WithThumbnail_IncludesThumbnailInArchive()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.Thumbnail = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        // Act
        var result = await _builder.GeneratePkpassAsync(template, data, options);

        // Assert
        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        archive.Entries.Should().Contain(e => e.Name == "thumbnail.png");
    }

    [Fact]
    public async Task GeneratePkpassAsync_PassJsonContainsRequiredFields()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        var result = await _builder.GeneratePkpassAsync(template, data, options);

        // Assert
        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var passJsonEntry = archive.Entries.First(e => e.Name == "pass.json");
        using var passStream = passJsonEntry.Open();
        using var reader = new StreamReader(passStream);
        var passJson = await reader.ReadToEndAsync();

        var passObject = JsonSerializer.Deserialize<Dictionary<string, object>>(passJson);
        passObject.Should().NotBeNull();
        passObject.Should().ContainKey("formatVersion");
        passObject.Should().ContainKey("passTypeIdentifier");
        passObject.Should().ContainKey("serialNumber");
        passObject.Should().ContainKey("teamIdentifier");
        passObject.Should().ContainKey("organizationName");
        passObject.Should().ContainKey("description");
    }

    [Fact]
    public async Task GeneratePkpassAsync_ManifestContainsAllFiles()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.Logo = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        options.Icon = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act
        var result = await _builder.GeneratePkpassAsync(template, data, options);

        // Assert
        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var manifestEntry = archive.Entries.First(e => e.Name == "manifest.json");
        using var manifestStream = manifestEntry.Open();
        using var reader = new StreamReader(manifestStream);
        var manifestJson = await reader.ReadToEndAsync();

        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(manifestJson);
        manifest.Should().NotBeNull();
        manifest.Should().ContainKey("pass.json");
        manifest.Should().ContainKey("logo.png");
        manifest.Should().ContainKey("icon.png");
    }

    [Fact]
    public async Task GeneratePkpassAsync_ManifestHashesAreValid()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        var result = await _builder.GeneratePkpassAsync(template, data, options);

        // Assert
        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var manifestEntry = archive.Entries.First(e => e.Name == "manifest.json");
        using var manifestStream = manifestEntry.Open();
        using var reader = new StreamReader(manifestStream);
        var manifestJson = await reader.ReadToEndAsync();

        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(manifestJson);

        // Verify each hash
        foreach (var kvp in manifest!)
        {
            var fileEntry = archive.Entries.FirstOrDefault(e => e.Name == kvp.Key);
            fileEntry.Should().NotBeNull($"File {kvp.Key} should exist in archive");

            // Hash should be a valid hex string (40 chars for SHA1)
            kvp.Value.Should().HaveLength(40);
            kvp.Value.Should().MatchRegex("^[a-f0-9]{40}$");
        }
    }

    [Fact]
    public async Task GeneratePkpassAsync_WithBarcodeEnabled_IncludesBarcodeInPass()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.EnableBarcode = true;
        options.BarcodeFormat = "PKBarcodeFormatQR";
        options.BarcodeMessage = "test-barcode-message";

        // Act
        var result = await _builder.GeneratePkpassAsync(template, data, options);

        // Assert
        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var passJsonEntry = archive.Entries.First(e => e.Name == "pass.json");
        using var passStream = passJsonEntry.Open();
        using var reader = new StreamReader(passStream);
        var passJson = await reader.ReadToEndAsync();

        passJson.Should().Contain("barcode");
        passJson.Should().Contain("PKBarcodeFormatQR");
        passJson.Should().Contain("test-barcode-message");
    }

    [Fact]
    public async Task GeneratePkpassAsync_WithTemplateFields_CreatesValidPass()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("field1", "Field 1", "text", true));
        template.AddField(new WalletField("field2", "Field 2", "text", false, 1));

        var data = new Dictionary<string, object>
        {
            ["field1"] = "Value 1",
            ["field2"] = "Value 2"
        };

        var options = CreateTestOptions();

        // Act
        var result = await _builder.GeneratePkpassAsync(template, data, options);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var passJsonEntry = archive.Entries.First(e => e.Name == "pass.json");
        using var passStream = passJsonEntry.Open();
        using var reader = new StreamReader(passStream);
        var passJson = await reader.ReadToEndAsync();

        // Verify pass.json is valid
        _builder.ValidatePassJson(passJson).Should().BeTrue();
    }

    [Fact]
    public void ValidatePassJson_WithAllRequiredFields_ReturnsTrue()
    {
        // Arrange
        var passJson = @"{
            ""formatVersion"": 1,
            ""passTypeIdentifier"": ""pass.test.example"",
            ""serialNumber"": ""123456"",
            ""teamIdentifier"": ""TEAM123"",
            ""organizationName"": ""Test Org"",
            ""description"": ""Test Pass""
        }";

        // Act
        var result = _builder.ValidatePassJson(passJson);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidatePassJson_WithMissingFormatVersion_ReturnsFalse()
    {
        // Arrange
        var passJson = @"{
            ""passTypeIdentifier"": ""pass.test.example"",
            ""serialNumber"": ""123456"",
            ""teamIdentifier"": ""TEAM123"",
            ""organizationName"": ""Test Org"",
            ""description"": ""Test Pass""
        }";

        // Act
        var result = _builder.ValidatePassJson(passJson);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePassJson_WithMissingPassTypeIdentifier_ReturnsFalse()
    {
        // Arrange
        var passJson = @"{
            ""formatVersion"": 1,
            ""serialNumber"": ""123456"",
            ""teamIdentifier"": ""TEAM123"",
            ""organizationName"": ""Test Org"",
            ""description"": ""Test Pass""
        }";

        // Act
        var result = _builder.ValidatePassJson(passJson);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePassJson_WithInvalidJson_ReturnsFalse()
    {
        // Arrange
        var passJson = "not valid json{";

        // Act
        var result = _builder.ValidatePassJson(passJson);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTemplateCompatible_ForApplePlatform_WithValidTemplate_ReturnsTrue()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("test", "Test", "text", true));

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.AppleWallet);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTemplateCompatible_ForApplePlatform_WithEmptyTemplate_ReturnsFalse()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.AppleWallet);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTemplateCompatible_ForGooglePlatform_ReturnsFalse()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("test", "Test", "text", true));

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.GoogleWallet);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetPlatformRequirements_ForApplePlatform_ReturnsRequirements()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.AppleWallet,
            WalletTemplateType.DriverLicense);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("AppleWallet");
        result.RequiredFields.Should().NotBeEmpty();
        result.RequiredAssets.Should().NotBeEmpty();
        result.Constraints.Should().NotBeEmpty();
        result.SupportedFeatures.Should().NotBeEmpty();
    }

    [Fact]
    public void GetPlatformRequirements_ForApplePlatform_HasMandatoryIcon()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.AppleWallet,
            WalletTemplateType.ProofOfAge);

        // Assert
        var iconAsset = result.RequiredAssets.Should().Contain(a => a.AssetName == "icon").Subject;
        iconAsset.IsMandatory.Should().BeTrue();
        iconAsset.Width.Should().Be(29);
        iconAsset.Height.Should().Be(29);
    }

    [Fact]
    public void GetPlatformRequirements_ForGooglePlatform_ReturnsEmptyRequirements()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.GoogleWallet,
            WalletTemplateType.DriverLicense);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("GoogleWallet");
    }

    [Theory]
    [InlineData("#FFFFFF")]
    [InlineData("#000000")]
    [InlineData("#003366")]
    public async Task GeneratePkpassAsync_WithCustomColors_AppliesColors(string backgroundColor)
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.BackgroundColor = backgroundColor;
        options.ForegroundColor = "#FFFFFF";

        // Act
        var result = await _builder.GeneratePkpassAsync(template, data, options);

        // Assert
        using var ms = new MemoryStream(result);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var passJsonEntry = archive.Entries.First(e => e.Name == "pass.json");
        using var passStream = passJsonEntry.Open();
        using var reader = new StreamReader(passStream);
        var passJson = await reader.ReadToEndAsync();

        passJson.Should().Contain(backgroundColor);
    }

    [Fact]
    public async Task GeneratePkpassAsync_LogsInformation()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        await _builder.GeneratePkpassAsync(template, data, options);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating pkpass")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GeneratePkpassAsync_WithoutSigningCertificate_LogsWarning()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.SigningCertificatePath = null;

        // Act
        await _builder.GeneratePkpassAsync(template, data, options);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No signing certificate")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Helper methods
    private static WalletTemplate CreateTestTemplate()
    {
        return new WalletTemplate(
            Guid.NewGuid(),
            "Test Wallet",
            "Test wallet template",
            WalletTemplateType.ProofOfAge,
            "test-user");
    }

    private static Dictionary<string, object> CreateTestData()
    {
        return new Dictionary<string, object>
        {
            ["name"] = "John Doe",
            ["dateOfBirth"] = "1990-01-01",
            ["expiryDate"] = "2025-12-31"
        };
    }

    private static AppleWalletOptions CreateTestOptions()
    {
        return new AppleWalletOptions
        {
            TeamIdentifier = "TEST_TEAM",
            PassTypeIdentifier = "pass.test.example",
            OrganizationName = "Test Organization",
            Description = "Test Pass",
            LogoText = "Test",
            BackgroundColor = "#003366",
            ForegroundColor = "#FFFFFF",
            LabelColor = "#CCCCCC",
            EnableBarcode = false
        };
    }
}
