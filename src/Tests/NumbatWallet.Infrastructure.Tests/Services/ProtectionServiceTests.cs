using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Infrastructure.Services;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;
using FluentAssertions;

namespace NumbatWallet.Infrastructure.Tests.Services;

public class ProtectionServiceTests
{
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly Mock<ITenantPolicyService> _tenantPolicyServiceMock;
    private readonly Mock<ICurrentTenantService> _currentTenantServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<ProtectionService>> _loggerMock;
    private readonly ProtectionService _sut;

    public ProtectionServiceTests()
    {
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _tenantPolicyServiceMock = new Mock<ITenantPolicyService>();
        _currentTenantServiceMock = new Mock<ICurrentTenantService>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<ProtectionService>>();

        _sut = new ProtectionService(
            _encryptionServiceMock.Object,
            _tenantPolicyServiceMock.Object,
            _currentTenantServiceMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProtectAsync_WithSecretClassification_ShouldEncrypt()
    {
        // Arrange
        var value = "sensitive-data";
        var classification = DataClassification.Secret;
        var fieldName = "ApiKey";
        var entityType = "Configuration";
        var tenantId = "tenant-123";
        var keyId = "key-456";
        var encryptedBytes = Encoding.UTF8.GetBytes("encrypted");

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _tenantPolicyServiceMock.Setup(x => x.RequiresEncryptionAsync(
            It.IsAny<Guid>(), entityType, fieldName))
            .ReturnsAsync(true);
        _encryptionServiceMock.Setup(x => x.GetCurrentKeyIdAsync(tenantId))
            .ReturnsAsync(keyId);
        _encryptionServiceMock.Setup(x => x.EncryptAsync(It.IsAny<byte[]>(), keyId))
            .ReturnsAsync(encryptedBytes);

        // Act
        var result = await _sut.ProtectAsync(value, classification, fieldName, entityType);

        // Assert
        result.Should().NotBeNull();
        result.IsEncrypted.Should().BeTrue();
        result.EncryptedValue.Should().NotBeNull();
        result.EncryptedValue!.CipherText.Should().BeEquivalentTo(encryptedBytes);
        result.EncryptedValue.KeyId.Should().Be(keyId);
        result.PlainValue.Should().BeNull();
        result.Classification.Should().Be(classification);
    }

    [Fact]
    public async Task ProtectAsync_WithOfficialClassification_ShouldNotEncrypt()
    {
        // Arrange
        var value = "public-data";
        var classification = DataClassification.Official;
        var fieldName = "Name";
        var entityType = "Person";
        var tenantId = "tenant-123";

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _tenantPolicyServiceMock.Setup(x => x.RequiresEncryptionAsync(
            It.IsAny<Guid>(), entityType, fieldName))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ProtectAsync(value, classification, fieldName, entityType);

        // Assert
        result.Should().NotBeNull();
        result.IsEncrypted.Should().BeFalse();
        result.EncryptedValue.Should().BeNull();
        result.PlainValue.Should().Be(value);
        result.Classification.Should().Be(classification);
    }

    [Fact]
    public async Task UnprotectAsync_WithEncryptedValue_ShouldDecrypt()
    {
        // Arrange
        var originalValue = "sensitive-data";
        var encryptedBytes = Encoding.UTF8.GetBytes("encrypted");
        var keyId = "key-456";
        var reason = "Audit review";

        var protectedValue = new ProtectedValue<string>
        {
            EncryptedValue = new EncryptedData
            {
                CipherText = encryptedBytes,
                KeyId = keyId,
                Algorithm = "AES-256-GCM",
                EncryptedAt = DateTimeOffset.UtcNow
            },
            Classification = DataClassification.Secret
        };

        _encryptionServiceMock.Setup(x => x.DecryptAsync(encryptedBytes, keyId))
            .ReturnsAsync(Encoding.UTF8.GetBytes(originalValue));

        // Act
        var result = await _sut.UnprotectAsync(protectedValue, reason);

        // Assert
        result.Should().Be(originalValue);
        _auditServiceMock.Verify(x => x.LogUnmaskOperationAsync(
            "ProtectedValue",
            keyId,
            "Value",
            DataClassification.Secret,
            reason,
            It.IsAny<string>(),
            It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task UnprotectAsync_WithPlainValue_ShouldReturnDirectly()
    {
        // Arrange
        var value = "public-data";
        var protectedValue = new ProtectedValue<string>
        {
            PlainValue = value,
            Classification = DataClassification.Official
        };

        // Act
        var result = await _sut.UnprotectAsync(protectedValue);

        // Assert
        result.Should().Be(value);
        _encryptionServiceMock.Verify(x => x.DecryptAsync(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GenerateSearchTokensAsync_WithExactMatchStrategy_ShouldGenerateHmacToken()
    {
        // Arrange
        var value = "john.doe@example.com";
        var fieldName = "Email";
        var strategy = SearchStrategy.Exact;
        var tenantId = "tenant-123";
        var expectedToken = "hmac_token_123";

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _encryptionServiceMock.Setup(x => x.GenerateHmacAsync(
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _sut.GenerateSearchTokensAsync(value, fieldName, strategy);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Should().Be(expectedToken);
    }

    [Fact]
    public async Task GenerateSearchTokensAsync_WithPrefixStrategy_ShouldGenerateMultipleTokens()
    {
        // Arrange
        var value = "john";
        var fieldName = "FirstName";
        var strategy = SearchStrategy.Prefix;
        var tenantId = "tenant-123";

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _encryptionServiceMock.Setup(x => x.GenerateHmacAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string data, string context) => $"hmac_{data}");

        // Act
        var result = await _sut.GenerateSearchTokensAsync(value, fieldName, strategy);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(1);
        result.Should().Contain("hmac_j");
        result.Should().Contain("hmac_jo");
        result.Should().Contain("hmac_joh");
        result.Should().Contain("hmac_john");
    }

    [Theory]
    [InlineData("1234567890", RedactionPattern.ShowLastFour, "******7890")]
    [InlineData("john.doe@example.com", RedactionPattern.ShowDomain, "****@example.com")]
    [InlineData("0412345678", RedactionPattern.ShowFirstThree, "041*******")]
    [InlineData("sensitive", RedactionPattern.Full, "****")]
    public void GetRedactedValue_ShouldApplyCorrectPattern(string value, RedactionPattern pattern, string expected)
    {
        // Act
        var result = _sut.GetRedactedValue(value, pattern);

        // Assert
        result.Should().Be(expected);
    }
}