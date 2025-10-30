using Microsoft.Extensions.Logging;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Infrastructure.WalletBuilders;

namespace NumbatWallet.Infrastructure.Tests.WalletBuilders;

public class PlatformWalletBuilderTests
{
    private readonly Mock<IAppleWalletBuilder> _mockAppleBuilder;
    private readonly Mock<IGoogleWalletBuilder> _mockGoogleBuilder;
    private readonly Mock<IWebWalletBuilder> _mockWebBuilder;
    private readonly Mock<ILogger<PlatformWalletBuilder>> _mockLogger;
    private readonly PlatformWalletBuilder _builder;

    public PlatformWalletBuilderTests()
    {
        _mockAppleBuilder = new Mock<IAppleWalletBuilder>();
        _mockGoogleBuilder = new Mock<IGoogleWalletBuilder>();
        _mockWebBuilder = new Mock<IWebWalletBuilder>();
        _mockLogger = new Mock<ILogger<PlatformWalletBuilder>>();

        _builder = new PlatformWalletBuilder(
            _mockAppleBuilder.Object,
            _mockGoogleBuilder.Object,
            _mockWebBuilder.Object,
            _mockLogger.Object);

        // Setup default mock responses
        SetupDefaultMocks();
    }

    [Fact]
    public async Task BuildWalletPackageAsync_ForApplePlatform_ReturnsApplePackage()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        // Act
        var result = await _builder.BuildWalletPackageAsync(
            template, data, WalletPlatform.AppleWallet);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("AppleWallet");
        result.PackageData.Should().NotBeNull();
        result.PackageType.Should().Be("application/vnd.apple.pkpass");
        result.FileName.Should().EndWith(".pkpass");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BuildWalletPackageAsync_ForGooglePlatform_ReturnsGooglePackage()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        // Act
        var result = await _builder.BuildWalletPackageAsync(
            template, data, WalletPlatform.GoogleWallet);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("GoogleWallet");
        result.PackageUrl.Should().NotBeNullOrEmpty();
        result.PackageType.Should().Be("application/jwt");
        result.Metadata.Should().ContainKey("jwt");
        result.Metadata.Should().ContainKey("passId");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BuildWalletPackageAsync_ForWebPlatform_ReturnsWebPackage()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        // Act
        var result = await _builder.BuildWalletPackageAsync(
            template, data, WalletPlatform.Web);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("Web");
        result.HtmlContent.Should().NotBeNullOrEmpty();
        result.PackageType.Should().Be("text/html");
        result.Metadata.Should().ContainKey("walletId");
        result.Metadata.Should().ContainKey("shareUrl");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BuildWalletPackageAsync_ForPwaPlatform_IncludesPwaManifest()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        // Act
        var result = await _builder.BuildWalletPackageAsync(
            template, data, WalletPlatform.PWA);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("PWA");
        result.HtmlContent.Should().NotBeNullOrEmpty();
        result.Metadata.Should().ContainKey("pwaManifest");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BuildWalletPackageAsync_ForWebPlatform_WithQrCodeEnabled_IncludesQrCode()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        // Act
        var result = await _builder.BuildWalletPackageAsync(
            template, data, WalletPlatform.Web);

        // Assert
        result.QrCodeData.Should().NotBeNull();
        result.QrCodeData.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BuildWalletPackageAsync_ForAllPlatforms_GeneratesMultiplePlatformPackages()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        // Act
        var result = await _builder.BuildWalletPackageAsync(
            template, data, WalletPlatform.All);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("All");
        result.PackageType.Should().Be("multiplatform");
        result.MultiPlatformPackages.Should().NotBeNull();
        result.MultiPlatformPackages.Should().HaveCount(3);
        result.MultiPlatformPackages.Should().Contain(p => p.Platform == "AppleWallet");
        result.MultiPlatformPackages.Should().Contain(p => p.Platform == "GoogleWallet");
        result.MultiPlatformPackages.Should().Contain(p => p.Platform == "Web");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BuildWalletPackageAsync_ApplePlatform_CallsAppleWalletBuilder()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        // Act
        await _builder.BuildWalletPackageAsync(template, data, WalletPlatform.AppleWallet);

        // Assert
        _mockAppleBuilder.Verify(
            x => x.GeneratePkpassAsync(
                template,
                data,
                It.IsAny<AppleWalletOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BuildWalletPackageAsync_GooglePlatform_CallsGoogleWalletBuilder()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        // Act
        await _builder.BuildWalletPackageAsync(template, data, WalletPlatform.GoogleWallet);

        // Assert
        _mockGoogleBuilder.Verify(
            x => x.GenerateGooglePassAsync(
                template,
                data,
                It.IsAny<GoogleWalletOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockGoogleBuilder.Verify(
            x => x.CreateJwtAsync(It.IsAny<GoogleWalletPassDto>()),
            Times.Once);

        _mockGoogleBuilder.Verify(
            x => x.GetAddToWalletLink(It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task BuildWalletPackageAsync_WebPlatform_CallsWebWalletBuilder()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        // Act
        await _builder.BuildWalletPackageAsync(template, data, WalletPlatform.Web);

        // Assert
        _mockWebBuilder.Verify(
            x => x.GenerateWebWalletAsync(
                template,
                data,
                It.IsAny<WebWalletOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockWebBuilder.Verify(
            x => x.GenerateQrCodeAsync(
                It.IsAny<string>(),
                It.IsAny<QrCodeOptions>()),
            Times.Once);
    }

    [Fact]
    public async Task BuildWalletPackageAsync_WhenBuilderThrowsException_ReturnsFailedPackage()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        _mockAppleBuilder.Setup(x => x.GeneratePkpassAsync(
                It.IsAny<WalletTemplate>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<AppleWalletOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _builder.BuildWalletPackageAsync(
            template, data, WalletPlatform.AppleWallet);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Test error");
    }

    [Fact]
    public void IsTemplateCompatible_ForAppleWallet_WithTextFields_ReturnsTrue()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("name", "Name", "text", true));

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.AppleWallet);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTemplateCompatible_ForAppleWallet_WithDateFields_ReturnsTrue()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("dateOfBirth", "Date of Birth", "date", true));

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.AppleWallet);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTemplateCompatible_ForAppleWallet_WithImageFields_ReturnsTrue()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("photo", "Photo", "image", true));

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.AppleWallet);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTemplateCompatible_ForGoogleWallet_WithTextFields_ReturnsTrue()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("name", "Name", "text", true));

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.GoogleWallet);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTemplateCompatible_ForGoogleWallet_WithNumberFields_ReturnsTrue()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("age", "Age", "number", true));

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.GoogleWallet);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTemplateCompatible_ForWebPlatform_AlwaysReturnsTrue()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.Web);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTemplateCompatible_ForPwaPlatform_AlwaysReturnsTrue()
    {
        // Arrange
        var template = CreateTestTemplate();

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.PWA);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTemplateCompatible_ForAllPlatforms_RequiresCompatibilityWithAll()
    {
        // Arrange
        var template = CreateTestTemplate();
        template.AddField(new WalletField("name", "Name", "text", true));
        template.AddField(new WalletField("age", "Age", "number", true, 1));

        // Act
        var result = _builder.IsTemplateCompatible(template, WalletPlatform.All);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetPlatformRequirements_ForAppleWallet_ReturnsAppleRequirements()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.AppleWallet,
            WalletTemplateType.ProofOfAge);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("AppleWallet");
        result.RequiredFields.Should().NotBeEmpty();
        result.SupportedFieldTypes.Should().Contain("text");
        result.SupportedFieldTypes.Should().Contain("date");
        result.RequiresSigning.Should().BeTrue();
        result.MaxImageSize.Should().Be(2 * 1024 * 1024);
        result.ImageFormats.Should().Contain("PNG");
    }

    [Fact]
    public void GetPlatformRequirements_ForGoogleWallet_ReturnsGoogleRequirements()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.GoogleWallet,
            WalletTemplateType.ProofOfAge);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("GoogleWallet");
        result.RequiredFields.Should().NotBeEmpty();
        result.SupportedFieldTypes.Should().Contain("text");
        result.SupportedFieldTypes.Should().Contain("number");
        result.RequiresSigning.Should().BeTrue();
        result.MaxImageSize.Should().Be(1 * 1024 * 1024);
    }

    [Fact]
    public void GetPlatformRequirements_ForWebPlatform_ReturnsWebRequirements()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.Web,
            WalletTemplateType.ProofOfAge);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("Web");
        result.RequiredFields.Should().NotBeEmpty();
        result.SupportedFieldTypes.Should().Contain("text");
        result.SupportedFieldTypes.Should().Contain("email");
        result.RequiresSigning.Should().BeFalse();
        result.MaxImageSize.Should().Be(5 * 1024 * 1024);
    }

    [Fact]
    public void GetPlatformRequirements_ForPwaPlatform_IncludesPwaSpecificRequirements()
    {
        // Act
        var result = _builder.GetPlatformRequirements(
            WalletPlatform.PWA,
            WalletTemplateType.ProofOfAge);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("PWA");
        result.AdditionalRequirements.Should().ContainKey("manifest");
        result.AdditionalRequirements.Should().ContainKey("serviceWorker");
        result.AdditionalRequirements.Should().ContainKey("https");
    }

    [Fact]
    public async Task BuildAppleWalletAsync_WithWalletEntity_CallsAppleBuilder()
    {
        // Arrange
        var wallet = CreateTestWallet();
        var template = CreateTestTemplate();

        // Act
        var result = await _builder.BuildAppleWalletAsync(wallet, template);

        // Assert
        result.Should().NotBeNull();
        _mockAppleBuilder.Verify(
            x => x.GeneratePkpassAsync(
                It.IsAny<WalletTemplate>(),
                It.Is<Dictionary<string, object>>(d =>
                    d.ContainsKey("walletId") && d.ContainsKey("walletName")),
                It.IsAny<AppleWalletOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BuildGoogleWalletAsync_WithWalletEntity_CallsGoogleBuilder()
    {
        // Arrange
        var wallet = CreateTestWallet();
        var template = CreateTestTemplate();

        // Act
        var result = await _builder.BuildGoogleWalletAsync(wallet, template);

        // Assert
        result.Should().NotBeNull();
        _mockGoogleBuilder.Verify(
            x => x.GenerateGooglePassAsync(
                It.IsAny<WalletTemplate>(),
                It.Is<Dictionary<string, object>>(d =>
                    d.ContainsKey("walletId") && d.ContainsKey("walletName")),
                It.IsAny<GoogleWalletOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BuildWebWalletAsync_WithWalletEntity_CallsWebBuilder()
    {
        // Arrange
        var wallet = CreateTestWallet();
        var template = CreateTestTemplate();

        // Act
        var result = await _builder.BuildWebWalletAsync(wallet, template);

        // Assert
        result.Should().NotBeNull();
        _mockWebBuilder.Verify(
            x => x.GenerateWebWalletAsync(
                It.IsAny<WalletTemplate>(),
                It.Is<Dictionary<string, object>>(d =>
                    d.ContainsKey("walletId") && d.ContainsKey("walletName")),
                It.IsAny<WebWalletOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BuildWalletPackageAsync_LogsInformationOnSuccess()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        // Act
        await _builder.BuildWalletPackageAsync(template, data, WalletPlatform.AppleWallet);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Building wallet package")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully built")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BuildWalletPackageAsync_LogsErrorOnFailure()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        _mockAppleBuilder.Setup(x => x.GeneratePkpassAsync(
                It.IsAny<WalletTemplate>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<AppleWalletOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        await _builder.BuildWalletPackageAsync(template, data, WalletPlatform.AppleWallet);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error building wallet package")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BuildWalletPackageAsync_SetsCorrectMetadata()
    {
        // Arrange
        var template = CreateTestTemplate();
        var data = CreateTestData();

        // Act
        var result = await _builder.BuildWalletPackageAsync(template, data, WalletPlatform.AppleWallet);

        // Assert
        result.TemplateId.Should().Be(template.Id);
        result.TemplateName.Should().Be(template.Name);
        result.Data.Should().BeEquivalentTo(data);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // Helper methods
    private void SetupDefaultMocks()
    {
        // Apple Wallet mock
        _mockAppleBuilder.Setup(x => x.GeneratePkpassAsync(
                It.IsAny<WalletTemplate>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<AppleWalletOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

        // Google Wallet mock
        _mockGoogleBuilder.Setup(x => x.GenerateGooglePassAsync(
                It.IsAny<WalletTemplate>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<GoogleWalletOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoogleWalletPassDto
            {
                Id = "test-id",
                ClassId = "test-class-id",
                State = "ACTIVE",
                Jwt = "test.jwt.token",
                SaveUrl = "https://pay.google.com/gp/v/save/test.jwt.token"
            });

        _mockGoogleBuilder.Setup(x => x.CreateJwtAsync(It.IsAny<GoogleWalletPassDto>()))
            .ReturnsAsync("test.jwt.token");

        _mockGoogleBuilder.Setup(x => x.GetAddToWalletLink(It.IsAny<string>()))
            .Returns("https://pay.google.com/gp/v/save/test.jwt.token");

        // Web Wallet mock
        _mockWebBuilder.Setup(x => x.GenerateWebWalletAsync(
                It.IsAny<WalletTemplate>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<WebWalletOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebWalletDto
            {
                Id = Guid.NewGuid(),
                TemplateId = Guid.NewGuid().ToString(),
                HtmlContent = "<html><body>Test Wallet</body></html>",
                JsonData = new Dictionary<string, object>(),
                QrCodeDataUrl = "data:image/png;base64,test",
                ShareUrl = "https://wallet.example.com/share/123",
                Resources = new Dictionary<string, string>()
            });

        _mockWebBuilder.Setup(x => x.GenerateQrCodeAsync(
                It.IsAny<string>(),
                It.IsAny<QrCodeOptions>()))
            .ReturnsAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header

        _mockWebBuilder.Setup(x => x.GeneratePwaManifestAsync(It.IsAny<WalletTemplate>()))
            .ReturnsAsync("{\"name\": \"Test PWA\"}");
    }

    private static WalletTemplate CreateTestTemplate()
    {
        return new WalletTemplate(
            Guid.NewGuid(),
            "Test Wallet Template",
            "Test wallet template description",
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

    private static Wallet CreateTestWallet()
    {
        var personId = Guid.NewGuid();
        var walletResult = Wallet.Create(personId, "Test Wallet");

        if (!walletResult.IsSuccess)
        {
            throw new InvalidOperationException("Failed to create test wallet");
        }

        var wallet = walletResult.Value;
        wallet.SetTenantId(Guid.NewGuid().ToString());
        return wallet;
    }
}
