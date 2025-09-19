using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Infrastructure.Crypto;
using NumbatWallet.Infrastructure.Crypto.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;
using Xunit;
using FluentAssertions;

namespace NumbatWallet.Infrastructure.Tests.Crypto;

public class CryptoServiceTests : IDisposable
{
    private readonly Mock<IKeyWrapProvider> _wrapProviderMock;
    private readonly Mock<IKeyVaultService> _keyVaultServiceMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly MemoryCache _memoryCache;
    private readonly Mock<ILogger<CryptoService>> _loggerMock;

    public CryptoServiceTests()
    {
        _wrapProviderMock = new Mock<IKeyWrapProvider>();
        _keyVaultServiceMock = new Mock<IKeyVaultService>();
        _tenantServiceMock = new Mock<ITenantService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<CryptoService>>();

        // Setup tenant service
        var tenantId = Guid.NewGuid();
        _tenantServiceMock.Setup(x => x.TenantId).Returns(tenantId);
    }

    [Fact]
    public async Task EncryptAsync_WithPublicData_ReturnsPlaintext()
    {
        // Arrange
        var plaintext = "Public information";
        var classification = DataClassification.Unofficial;

        var sut = CreateTestableService();

        // Act
        var result = await sut.EncryptAsync(plaintext, classification);

        // Assert
        result.Should().Be(plaintext);
        _wrapProviderMock.Verify(x => x.WrapAsync(
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task EncryptAsync_WithOfficialData_ReturnsPlaintext()
    {
        // Arrange
        var plaintext = "Official information";
        var classification = DataClassification.Official;

        var sut = CreateTestableService();

        // Act
        var result = await sut.EncryptAsync(plaintext, classification);

        // Assert
        result.Should().Be(plaintext);
    }

    [Fact]
    public async Task EncryptAsync_WithSensitiveData_ReturnsEncrypted()
    {
        // Arrange
        var plaintext = "Sensitive information";
        var classification = DataClassification.OfficialSensitive;

        var sut = CreateTestableService();

        // Setup wrap provider to return a dummy wrapped DEK and KEK ID
        var dummyWrappedDek = new byte[] { 1, 2, 3, 4, 5 };
        var kekId = "test-kek-id";

        _wrapProviderMock.Setup(x => x.CreateKekAsync(
                It.IsAny<string>(),
                It.IsAny<KekProperties>()))
            .ReturnsAsync(kekId);

        _wrapProviderMock.Setup(x => x.WrapAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(dummyWrappedDek);

        _keyVaultServiceMock.Setup(x => x.SetSecretAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await sut.EncryptAsync(plaintext, classification);

        // Assert
        result.Should().NotBe(plaintext);
        _wrapProviderMock.Verify(x => x.WrapAsync(
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DecryptAsync_WithPublicData_ReturnsOriginal()
    {
        // Arrange
        var ciphertext = "Public information";
        var classification = DataClassification.Unofficial;

        var sut = CreateTestableService();

        // Act
        var result = await sut.DecryptAsync(ciphertext, classification);

        // Assert
        result.Should().Be(ciphertext);
    }

    [Theory]
    [InlineData("")]
    public async Task EncryptAsync_WithEmpty_ReturnsInput(string input)
    {
        // Arrange
        var classification = DataClassification.Protected;
        var sut = CreateTestableService();

        // Act
        var result = await sut.EncryptAsync(input, classification);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public async Task EncryptAsync_WithNull_ReturnsInput()
    {
        // Arrange
        string? input = null;
        var classification = DataClassification.Protected;
        var sut = CreateTestableService();

        // Act
        var result = await sut.EncryptAsync(input!, classification);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EncryptBytesAsync_WithSensitiveData_AppendsVersionNonceTag()
    {
        // Arrange
        var plainBytes = Encoding.UTF8.GetBytes("Test data");
        var classification = DataClassification.Protected;

        var sut = CreateTestableService();

        // Setup wrap provider
        var dummyWrappedDek = new byte[] { 1, 2, 3, 4, 5 };
        var kekId = "test-kek-id";

        _wrapProviderMock.Setup(x => x.CreateKekAsync(
                It.IsAny<string>(),
                It.IsAny<KekProperties>()))
            .ReturnsAsync(kekId);

        _wrapProviderMock.Setup(x => x.WrapAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(dummyWrappedDek);

        _keyVaultServiceMock.Setup(x => x.SetSecretAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await sut.EncryptBytesAsync(plainBytes, classification);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(plainBytes.Length);
        result[0].Should().Be(1); // Version byte
        _wrapProviderMock.Verify(x => x.WrapAsync(
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentDekVersionAsync_ReturnsVersion()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString();
        var sut = CreateTestableService();

        // Act
        var version = await sut.GetCurrentDekVersionAsync(tenantId);

        // Assert
        version.Should().Be(1); // Default version
    }

    [Fact]
    public async Task RotateDekAsync_RemovesCachedKey()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString();
        var cacheKey = $"dek:{tenantId}";

        // Add dummy key to cache
        _memoryCache.Set(cacheKey, new byte[] { 1, 2, 3 });

        var sut = CreateTestableService();

        // Setup wrap provider
        var kekId = "test-kek-id";

        // Mock GetSecretAsync to return the KEK ID when getting KEK reference
        _keyVaultServiceMock.Setup(x => x.GetSecretAsync(
                It.Is<string>(s => s.Contains("kek-ref")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(kekId);

        _wrapProviderMock.Setup(x => x.CreateKekAsync(
                It.IsAny<string>(),
                It.IsAny<KekProperties>()))
            .ReturnsAsync(kekId);

        _wrapProviderMock.Setup(x => x.WrapAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 4, 5, 6 });

        _keyVaultServiceMock.Setup(x => x.SetSecretAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await sut.RotateDekAsync(tenantId);

        // Assert
        _memoryCache.TryGetValue(cacheKey, out byte[]? _).Should().BeFalse();
        _wrapProviderMock.Verify(x => x.WrapAsync(
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    private CryptoService CreateTestableService()
    {
        return new CryptoService(
            _wrapProviderMock.Object,
            _keyVaultServiceMock.Object,
            _tenantServiceMock.Object,
            _memoryCache,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }
}