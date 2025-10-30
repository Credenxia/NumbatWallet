using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Infrastructure.WalletBuilders;

namespace NumbatWallet.Infrastructure.Tests.WalletBuilders;

public class GoogleWalletBuilderTests
{
    private readonly Mock<ILogger<GoogleWalletBuilder>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly GoogleWalletBuilder _builder;

    public GoogleWalletBuilderTests()
    {
        _mockLogger = new Mock<ILogger<GoogleWalletBuilder>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup default configuration
        _mockConfiguration.Setup(x => x["GoogleWallet:ServiceAccountEmail"]).Returns("test@example.com");
        _mockConfiguration.Setup(x => x["GoogleWallet:PrivateKey"]).Returns("test-private-key-for-development");
        _mockConfiguration.Setup(x => x["GoogleWallet:IssuerId"]).Returns("3388000000022297348");

        _builder = new GoogleWalletBuilder(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task GenerateGooglePassAsync_WithValidTemplate_ReturnsPassDto()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        var result = await _builder.GenerateGooglePassAsync(template, data, options);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(options.ObjectId);
        result.ClassId.Should().Be(options.ClassId);
        result.State.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task GenerateGooglePassAsync_CreatesJwtToken()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        var result = await _builder.GenerateGooglePassAsync(template, data, options);

        // Assert
        result.Jwt.Should().NotBeNullOrEmpty();
        result.Jwt.Split('.').Should().HaveCount(3); // JWT has 3 parts
    }

    [Fact]
    public async Task GenerateGooglePassAsync_CreatesSaveUrl()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        var result = await _builder.GenerateGooglePassAsync(template, data, options);

        // Assert
        result.SaveUrl.Should().NotBeNullOrEmpty();
        result.SaveUrl.Should().StartWith("https://pay.google.com/gp/v/save/");
    }

    [Fact]
    public async Task GenerateGooglePassAsync_WithTemplateFields_MapsToTextModules()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("field1", "Field 1", "text", true, 0));
        template.AddField(new WalletField("field2", "Field 2", "text", false, 1));

        var data = new Dictionary<string, object>
        {
            ["field1"] = "Value 1",
            ["field2"] = "Value 2"
        };

        var options = CreateTestOptions();

        // Act
        var result = await _builder.GenerateGooglePassAsync(template, data, options);

        // Assert
        result.ClassData.Should().NotBeNull();
        result.ObjectData.Should().NotBeNull();
        result.ObjectData.Should().ContainKey("textModulesData");
    }

    [Fact]
    public async Task GenerateGooglePassAsync_WithBarcode_IncludesBarcodeInPass()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.BarcodeValue = "test-barcode-123";
        options.BarcodeType = "QR_CODE";

        // Act
        var result = await _builder.GenerateGooglePassAsync(template, data, options);

        // Assert
        result.ObjectData.Should().ContainKey("barcode");
        var barcode = (Dictionary<string, object>)result.ObjectData["barcode"];
        barcode.Should().ContainKey("value");
        barcode["value"].Should().Be("test-barcode-123");
        barcode["type"].Should().Be("QR_CODE");
    }

    [Fact]
    public async Task GenerateGooglePassAsync_WithLogo_IncludesLogoInPass()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.Logo = "https://example.com/logo.png";

        // Act
        var result = await _builder.GenerateGooglePassAsync(template, data, options);

        // Assert
        result.ObjectData.Should().ContainKey("logo");
    }

    [Fact]
    public async Task GenerateGooglePassAsync_WithCustomColors_AppliesHexBackgroundColor()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.HexBackgroundColor = "#FF0000";

        // Act
        var result = await _builder.GenerateGooglePassAsync(template, data, options);

        // Assert
        result.ObjectData.Should().ContainKey("hexBackgroundColor");
        result.ObjectData["hexBackgroundColor"].Should().Be("#FF0000");
    }

    [Fact]
    public async Task GenerateGooglePassAsync_WithTextModules_IncludesAllModules()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.TextModulesData["module1"] = "Value 1";
        options.TextModulesData["module2"] = "Value 2";

        // Act
        var result = await _builder.GenerateGooglePassAsync(template, data, options);

        // Assert
        result.ObjectData.Should().ContainKey("textModulesData");
        var textModules = (List<Dictionary<string, object>>)result.ObjectData["textModulesData"];
        textModules.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task CreateJwtAsync_WithValidPass_ReturnsJwtToken()
    {
        // Arrange
        var pass = new GoogleWalletPassDto
        {
            Id = "test-id",
            ClassId = "test-class-id",
            State = "ACTIVE",
            ClassData = new Dictionary<string, object>(),
            ObjectData = new Dictionary<string, object>
            {
                ["header"] = new Dictionary<string, string>(),
                ["textModulesData"] = new List<object>(),
                ["barcode"] = new Dictionary<string, string>()
            }
        };

        // Act
        var result = await _builder.CreateJwtAsync(pass);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Split('.').Should().HaveCount(3); // JWT format: header.payload.signature
    }

    [Fact]
    public void GetAddToWalletLink_WithJwt_ReturnsGooglePayUrl()
    {
        // Arrange
        var jwt = "test.jwt.token";

        // Act
        var result = _builder.GetAddToWalletLink(jwt);

        // Assert
        result.Should().Be("https://pay.google.com/gp/v/save/test.jwt.token");
    }

    [Fact]
    public void IsTemplateCompatible_ForGooglePlatform_WithValidTemplate_ReturnsTrue()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("test", "Test", "text", true, 0));

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.GoogleWallet);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTemplateCompatible_ForGooglePlatform_WithEmptyTemplate_ReturnsFalse()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.GoogleWallet);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTemplateCompatible_ForApplePlatform_ReturnsFalse()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("test", "Test", "text", true, 0));

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.AppleWallet);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetPlatformRequirements_ForGooglePlatform_ReturnsRequirements()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.GoogleWallet,
            WalletTemplateType.DriverLicense);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("GoogleWallet");
        result.RequiredFields.Should().NotBeEmpty();
        result.RequiredAssets.Should().NotBeEmpty();
        result.Constraints.Should().NotBeEmpty();
        result.SupportedFeatures.Should().NotBeEmpty();
    }

    [Fact]
    public void GetPlatformRequirements_ForGooglePlatform_HasRequiredFields()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.GoogleWallet,
            WalletTemplateType.ProofOfAge);

        // Assert
        result.RequiredFields.Should().Contain(f => f.FieldName == "issuerId");
        result.RequiredFields.Should().Contain(f => f.FieldName == "classId");
        result.RequiredFields.Should().Contain(f => f.FieldName == "header");
    }

    [Fact]
    public void GetPlatformRequirements_ForGooglePlatform_IncludesSupportedFeatures()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.GoogleWallet,
            WalletTemplateType.DriverLicense);

        // Assert
        result.Constraints.Should().ContainKey("maxTextModules");
        result.Constraints.Should().ContainKey("supportedBarcodes");
        result.SupportedFeatures.Should().Contain("barcode");
        result.SupportedFeatures.Should().Contain("save_to_android_pay");
    }

    [Fact]
    public void GetPlatformRequirements_ForApplePlatform_ReturnsEmptyRequirements()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.AppleWallet,
            WalletTemplateType.DriverLicense);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("AppleWallet");
    }

    [Fact]
    public async Task GenerateGooglePassAsync_LogsInformation()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        await _builder.GenerateGooglePassAsync(template, data, options);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating Google Wallet pass")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("QR_CODE")]
    [InlineData("PDF_417")]
    [InlineData("AZTEC")]
    [InlineData("CODE_128")]
    public async Task GenerateGooglePassAsync_WithDifferentBarcodeTypes_IncludesBarcodeType(string barcodeType)
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.BarcodeValue = "test-value";
        options.BarcodeType = barcodeType;

        // Act
        var result = await _builder.GenerateGooglePassAsync(template, data, options);

        // Assert
        result.ObjectData.Should().ContainKey("barcode");
        var barcode = (Dictionary<string, object>)result.ObjectData["barcode"];
        barcode["type"].Should().Be(barcodeType);
    }

    [Fact]
    public async Task GenerateGooglePassAsync_WithHeader_IncludesHeaderInObjectData()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.Header = "Test Header";
        options.Subheader = "Test Subheader";

        // Act
        var result = await _builder.GenerateGooglePassAsync(template, data, options);

        // Assert
        result.ObjectData.Should().ContainKey("header");
        var header = (Dictionary<string, object>)result.ObjectData["header"];
        header.Should().ContainKey("title");
        header.Should().ContainKey("subtitle");
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

    private static GoogleWalletOptions CreateTestOptions()
    {
        return new GoogleWalletOptions
        {
            IssuerId = "3388000000022297348",
            ClassId = "test-class-id",
            ObjectId = Guid.NewGuid().ToString(),
            CardTitle = "Test Card",
            Header = "Test Header",
            Subheader = "Test Subheader",
            HexBackgroundColor = "#003366"
        };
    }
}
