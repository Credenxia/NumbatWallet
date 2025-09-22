using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Domain.ValueObjects;
using NumbatWallet.Infrastructure.Security;
using Xunit;

namespace NumbatWallet.Infrastructure.Tests.Security;

public class RequestSigningServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<RequestSigningService>> _loggerMock;
    private readonly RequestSigningService _service;

    public RequestSigningServiceTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<RequestSigningService>>();
        _service = new RequestSigningService(_cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SignRequestAsync_ShouldGenerateValidSignature()
    {
        // Arrange
        var method = "POST";
        var path = "/api/v1/resource";
        var body = "{\"data\":\"test\"}";
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        };
        var privateKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-secret-key"));

        // Act
        var signature = await _service.SignRequestAsync(method, path, body, headers, privateKey);

        // Assert
        signature.Should().NotBeNullOrEmpty();
        signature.Should().Contain("algorithm=");
        signature.Should().Contain("signature=");
        signature.Should().Contain("nonce=");
        signature.Should().Contain("timestamp=");
    }

    [Fact]
    public async Task VerifyRequestSignatureAsync_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        var method = "GET";
        var path = "/api/v1/data";
        var privateKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("shared-secret"));

        // Sign the request first
        var signatureHeader = await _service.SignRequestAsync(method, path, null, null, privateKey);
        var signature = _service.ParseSignatureHeader(signatureHeader);

        // Setup cache to allow nonce
        _cacheMock.Setup(x => x.GetStringAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var isValid = await _service.VerifyRequestSignatureAsync(
            signature!,
            method,
            path,
            null,
            privateKey);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyRequestSignatureAsync_WithExpiredSignature_ShouldReturnFalse()
    {
        // Arrange
        var expiredSignature = new RequestSignature(
            "SHA256",
            "fake-signature",
            "fake-nonce",
            DateTimeOffset.UtcNow.AddMinutes(-10), // Expired
            new Dictionary<string, string>());

        var publicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-key"));

        // Act
        var isValid = await _service.VerifyRequestSignatureAsync(
            expiredSignature,
            "GET",
            "/api/test",
            null,
            publicKey);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyRequestSignatureAsync_WithReusedNonce_ShouldReturnFalse()
    {
        // Arrange
        var signature = new RequestSignature(
            "SHA256",
            "fake-signature",
            "used-nonce",
            DateTimeOffset.UtcNow,
            new Dictionary<string, string>());

        // Setup cache to return existing nonce (already used)
        _cacheMock.Setup(x => x.GetStringAsync(
            "nonce:used-nonce",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("2024-01-01");

        var publicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-key"));

        // Act
        var isValid = await _service.VerifyRequestSignatureAsync(
            signature,
            "POST",
            "/api/test",
            null,
            publicKey);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GenerateNonce_ShouldReturnUniqueValues()
    {
        // Act
        var nonce1 = _service.GenerateNonce();
        var nonce2 = _service.GenerateNonce();
        var nonce3 = _service.GenerateNonce();

        // Assert
        nonce1.Should().NotBeNullOrEmpty();
        nonce2.Should().NotBeNullOrEmpty();
        nonce3.Should().NotBeNullOrEmpty();
        nonce1.Should().NotBe(nonce2);
        nonce2.Should().NotBe(nonce3);
        nonce1.Should().NotBe(nonce3);
    }

    [Fact]
    public async Task ValidateNonceAsync_WithUnusedNonce_ShouldReturnTrue()
    {
        // Arrange
        var nonce = "unused-nonce";
        _cacheMock.Setup(x => x.GetStringAsync(
            $"nonce:{nonce}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var isValid = await _service.ValidateNonceAsync(nonce);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateNonceAsync_WithUsedNonce_ShouldReturnFalse()
    {
        // Arrange
        var nonce = "used-nonce";
        _cacheMock.Setup(x => x.GetStringAsync(
            $"nonce:{nonce}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("2024-01-01T00:00:00Z");

        // Act
        var isValid = await _service.ValidateNonceAsync(nonce);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task MarkNonceAsUsedAsync_ShouldStoreNonceInCache()
    {
        // Arrange
        var nonce = "test-nonce";

        // Act
        await _service.MarkNonceAsUsedAsync(nonce);

        // Assert
        _cacheMock.Verify(x => x.SetStringAsync(
            $"nonce:{nonce}",
            It.IsAny<string>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void ParseSignatureHeader_WithValidHeader_ShouldParseCorrectly()
    {
        // Arrange
        var header = "Signature algorithm=\"SHA256\",signature=\"abc123\",nonce=\"xyz789\",timestamp=\"1704067200\"";

        // Act
        var signature = _service.ParseSignatureHeader(header);

        // Assert
        signature.Should().NotBeNull();
        signature!.Algorithm.Should().Be("SHA256");
        signature.Signature.Should().Be("abc123");
        signature.Nonce.Should().Be("xyz789");
        signature.Timestamp.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1704067200));
    }

    [Fact]
    public void ParseSignatureHeader_WithInvalidHeader_ShouldReturnNull()
    {
        // Arrange
        var header = "Invalid header format";

        // Act
        var signature = _service.ParseSignatureHeader(header);

        // Assert
        signature.Should().BeNull();
    }
}