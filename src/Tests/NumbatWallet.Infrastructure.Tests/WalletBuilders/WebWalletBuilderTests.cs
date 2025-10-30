using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Infrastructure.WalletBuilders;

namespace NumbatWallet.Infrastructure.Tests.WalletBuilders;

public class WebWalletBuilderTests
{
    private readonly Mock<ILogger<WebWalletBuilder>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly WebWalletBuilder _builder;

    public WebWalletBuilderTests()
    {
        _mockLogger = new Mock<ILogger<WebWalletBuilder>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup default configuration
        _mockConfiguration.Setup(x => x["WebWallet:BaseUrl"]).Returns("https://wallet.example.com");

        _builder = new WebWalletBuilder(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task GenerateWebWalletAsync_WithValidTemplate_ReturnsWebWalletDto()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.TemplateId.Should().Be(template.Id.ToString());
        result.HtmlContent.Should().NotBeNullOrEmpty();
        result.JsonData.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateWebWalletAsync_WithQrCodeEnabled_GeneratesQrCode()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.EnableQrCode = true;

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.QrCodeDataUrl.Should().NotBeNullOrEmpty();
        result.QrCodeDataUrl.Should().StartWith("data:image/png;base64,");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_WithQrCodeDisabled_DoesNotGenerateQrCode()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.EnableQrCode = false;

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.QrCodeDataUrl.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateWebWalletAsync_WithOfflineMode_GeneratesPwaManifest()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.EnableOfflineMode = true;

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.PwaManifest.Should().NotBeNull();
        result.PwaManifest!.Name.Should().Contain(template.Name);
        result.PwaManifest.Icons.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateWebWalletAsync_WithOfflineModeDisabled_DoesNotGeneratePwaManifest()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.EnableOfflineMode = false;

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.PwaManifest.Should().BeNull();
    }

    [Fact]
    public async Task GenerateWebWalletAsync_GeneratesShareUrl()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.ShareUrl.Should().NotBeNullOrEmpty();
        result.ShareUrl.Should().StartWith("https://wallet.example.com/wallet/");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_IncludesCssAndJsResources()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.Resources.Should().ContainKey("css");
        result.Resources.Should().ContainKey("js");
        result.Resources["css"].Should().NotBeNullOrEmpty();
        result.Resources["js"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateWebWalletAsync_HtmlContent_ContainsTemplateFields()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("fullName", "Full Name", "text", true, 0));
        template.AddField(new WalletField("dateOfBirth", "Date of Birth", "date", true, 1));

        var data = new Dictionary<string, object>
        {
            ["fullName"] = "John Doe",
            ["dateOfBirth"] = "1990-01-01"
        };

        var options = CreateTestOptions();

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.HtmlContent.Should().Contain("Full Name");
        result.HtmlContent.Should().Contain("John Doe");
        result.HtmlContent.Should().Contain("Date of Birth");
        result.HtmlContent.Should().Contain("1990-01-01");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_JsonData_ContainsVerifiableCredentialStructure()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.JsonData.Should().ContainKey("@context");
        result.JsonData.Should().ContainKey("type");
        result.JsonData.Should().ContainKey("id");
        result.JsonData.Should().ContainKey("issuanceDate");
        result.JsonData.Should().ContainKey("issuer");
        result.JsonData.Should().ContainKey("credentialSubject");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_JsonData_IncludesTemplateFieldsInCredentialSubject()
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
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.JsonData.Should().ContainKey("credentialSubject");
        var subject = result.JsonData["credentialSubject"] as Dictionary<string, object>;
        subject.Should().NotBeNull();
        subject.Should().ContainKey("field1");
        subject.Should().ContainKey("field2");
        subject!["field1"].Should().Be("Value 1");
        subject["field2"].Should().Be("Value 2");
    }

    [Fact]
    public async Task GenerateQrCodeAsync_WithValidWalletId_GeneratesQrCodeImage()
    {
        // Arrange
        var walletId = Guid.NewGuid().ToString();
        var options = new QrCodeOptions
        {
            Size = 300,
            ErrorCorrection = "M",
            ForegroundColor = "#000000",
            BackgroundColor = "#FFFFFF"
        };

        // Act
        var result = await _builder.GenerateQrCodeAsync(walletId, options);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Length.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("L")]
    [InlineData("M")]
    [InlineData("Q")]
    [InlineData("H")]
    public async Task GenerateQrCodeAsync_WithDifferentErrorCorrectionLevels_GeneratesQrCode(string errorCorrection)
    {
        // Arrange
        var walletId = Guid.NewGuid().ToString();
        var options = new QrCodeOptions
        {
            Size = 300,
            ErrorCorrection = errorCorrection,
            ForegroundColor = "#000000",
            BackgroundColor = "#FFFFFF"
        };

        // Act
        var result = await _builder.GenerateQrCodeAsync(walletId, options);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePwaManifestAsync_WithValidTemplate_ReturnsManifestJson()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var result = await _builder.GeneratePwaManifestAsync(template);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain(template.Name);
        result.Should().Contain("icons");
        result.Should().Contain("display");
        result.Should().Contain("startUrl"); // camelCase JSON property
    }

    [Fact]
    public async Task GeneratePwaManifestAsync_IncludesRequiredIcons()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var result = await _builder.GeneratePwaManifestAsync(template);

        // Assert
        result.Should().Contain("192x192");
        result.Should().Contain("512x512");
        result.Should().Contain("maskable");
    }

    [Fact]
    public void IsTemplateCompatible_ForWebPlatform_ReturnsTrue()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.Web);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTemplateCompatible_ForPwaPlatform_ReturnsTrue()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.PWA);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTemplateCompatible_ForApplePlatform_ReturnsFalse()
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

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.GoogleWallet);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetPlatformRequirements_ForWebPlatform_ReturnsRequirements()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.Web,
            WalletTemplateType.ProofOfAge);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("Web");
        result.RequiredFields.Should().NotBeEmpty();
        result.RequiredAssets.Should().NotBeEmpty();
        result.Constraints.Should().NotBeEmpty();
        result.SupportedFeatures.Should().NotBeEmpty();
    }

    [Fact]
    public void GetPlatformRequirements_ForPwaPlatform_ReturnsRequirements()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.PWA,
            WalletTemplateType.ProofOfAge);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("PWA");
        result.RequiredFields.Should().NotBeEmpty();
        result.RequiredAssets.Should().NotBeEmpty();
    }

    [Fact]
    public void GetPlatformRequirements_ForWebPlatform_IncludesSupportedFeatures()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.Web,
            WalletTemplateType.ProofOfAge);

        // Assert
        result.SupportedFeatures.Should().Contain("responsive");
        result.SupportedFeatures.Should().Contain("offline");
        result.SupportedFeatures.Should().Contain("qrcode");
        result.SupportedFeatures.Should().Contain("sharing");
        result.SupportedFeatures.Should().Contain("printing");
    }

    [Fact]
    public void GetPlatformRequirements_ForApplePlatform_ReturnsEmptyRequirements()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.AppleWallet,
            WalletTemplateType.ProofOfAge);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("AppleWallet");
        result.RequiredFields.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateWebWalletAsync_WithCustomColors_AppliesColors()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.PrimaryColor = "#FF0000";
        options.SecondaryColor = "#00FF00";

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.Resources["css"].Should().Contain("#FF0000");
        result.Resources["css"].Should().Contain("#00FF00");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_WithDarkTheme_AppliesDarkTheme()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.Theme = "dark";

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.HtmlContent.Should().Contain("dark");
        result.Resources["css"].Should().Contain(".dark");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_WithCustomCss_IncludesCustomCss()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.CustomCss = ".custom-class { color: blue; }";

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.Resources["css"].Should().Contain(".custom-class { color: blue; }");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_WithCustomJs_IncludesCustomJs()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.CustomJs = "console.log('custom script');";

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.Resources["js"].Should().Contain("console.log('custom script');");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_WithSharingEnabled_IncludesShareButton()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.EnableSharing = true;

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.HtmlContent.Should().Contain("btn-share");
        result.Resources["js"].Should().Contain("navigator.share");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_WithPrintingEnabled_IncludesPrintButton()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.EnablePrinting = true;

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.HtmlContent.Should().Contain("btn-print");
        result.HtmlContent.Should().Contain("window.print()");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_WithOfflineMode_IncludesServiceWorkerRegistration()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.EnableOfflineMode = true;

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.Resources["js"].Should().Contain("serviceWorker");
        result.Resources["js"].Should().Contain("sw.js");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_HtmlContent_IsValidHtml()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.HtmlContent.Should().StartWith("<!DOCTYPE html>");
        result.HtmlContent.Should().Contain("<html");
        result.HtmlContent.Should().Contain("<head>");
        result.HtmlContent.Should().Contain("<body class="); // body has class attribute for theme
        result.HtmlContent.Should().Contain("</html>");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_HtmlContent_HasResponsiveMeta()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.HtmlContent.Should().Contain("viewport");
        result.HtmlContent.Should().Contain("width=device-width");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_HtmlContent_IncludesThemeColor()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();
        options.PrimaryColor = "#123456";

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        result.HtmlContent.Should().Contain("theme-color");
        result.HtmlContent.Should().Contain("#123456");
    }

    [Fact]
    public async Task GenerateWebWalletAsync_LogsInformation()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();
        var options = CreateTestOptions();

        // Act
        await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating Web wallet")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateWebWalletAsync_FieldsOrderedByDisplayOrder()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("field3", "Field 3", "text", true, 2));
        template.AddField(new WalletField("field1", "Field 1", "text", true, 0));
        template.AddField(new WalletField("field2", "Field 2", "text", true, 1));

        var data = new Dictionary<string, object>
        {
            ["field1"] = "Value 1",
            ["field2"] = "Value 2",
            ["field3"] = "Value 3"
        };

        var options = CreateTestOptions();

        // Act
        var result = await _builder.GenerateWebWalletAsync(template, data, options);

        // Assert
        var field1Index = result.HtmlContent.IndexOf("Field 1", StringComparison.Ordinal);
        var field2Index = result.HtmlContent.IndexOf("Field 2", StringComparison.Ordinal);
        var field3Index = result.HtmlContent.IndexOf("Field 3", StringComparison.Ordinal);

        field1Index.Should().BeLessThan(field2Index);
        field2Index.Should().BeLessThan(field3Index);
    }

    // Helper methods
    private static WalletTemplate CreateTestTemplate()
    {
        return new WalletTemplate(
            Guid.NewGuid(),
            "Test Web Wallet",
            "Test web wallet template",
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

    private static WebWalletOptions CreateTestOptions()
    {
        return new WebWalletOptions
        {
            Theme = "light",
            PrimaryColor = "#003366",
            SecondaryColor = "#F5F5F5",
            EnableQrCode = true,
            EnableSharing = true,
            EnablePrinting = true,
            EnableOfflineMode = false,
            CustomCss = "",
            CustomJs = ""
        };
    }
}
