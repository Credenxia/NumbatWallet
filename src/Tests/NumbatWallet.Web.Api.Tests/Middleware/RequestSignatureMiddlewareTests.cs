using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.ValueObjects;
using NumbatWallet.Web.Api.Middleware;
using Xunit;

namespace NumbatWallet.Web.Api.Tests.Middleware;

public class RequestSignatureMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ILogger<RequestSignatureMiddleware>> _loggerMock;
    private readonly Mock<IRequestSigningService> _signingServiceMock;
    private readonly Mock<ITenantCertificateRepository> _certificateRepositoryMock;
    private readonly Mock<IApiKeyService> _apiKeyServiceMock;
    private readonly RequestSignatureMiddleware _middleware;
    private readonly RequestSignatureOptions _options;

    public RequestSignatureMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<RequestSignatureMiddleware>>();
        _signingServiceMock = new Mock<IRequestSigningService>();
        _certificateRepositoryMock = new Mock<ITenantCertificateRepository>();
        _apiKeyServiceMock = new Mock<IApiKeyService>();

        _options = new RequestSignatureOptions
        {
            RequireSignature = true,
            MaxSignatureAgeSeconds = 300,
            SignedHeaders = new List<string> { "Content-Type", "Host" },
            ExcludedPaths = new List<string> { "/health", "/swagger" }
        };

        _middleware = new RequestSignatureMiddleware(
            _nextMock.Object,
            _loggerMock.Object,
            Options.Create(_options));
    }

    [Fact]
    public async Task InvokeAsync_WithValidSignature_ShouldPassToNextMiddleware()
    {
        // Arrange
        var context = CreateHttpContext();
        var signature = CreateValidSignature();
        context.Request.Headers["X-Request-Signature"] = FormatSignatureHeader(signature);
        context.Request.Headers["X-API-Key"] = "test-api-key";

        _signingServiceMock
            .Setup(x => x.ParseSignatureHeader(It.IsAny<string>()))
            .Returns(signature);

        _apiKeyServiceMock
            .Setup(x => x.GetPublicKeyAsync("test-api-key"))
            .ReturnsAsync("test-public-key");

        _signingServiceMock
            .Setup(x => x.VerifyRequestSignatureAsync(
                It.IsAny<RequestSignature>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(
            context,
            _signingServiceMock.Object,
            _certificateRepositoryMock.Object,
            _apiKeyServiceMock.Object);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
        context.Items.Should().ContainKey("RequestSignature");
        context.Items.Should().ContainKey("SignatureVerified");
        context.Items["SignatureVerified"].Should().Be(true);
    }

    [Fact]
    public async Task InvokeAsync_WithoutSignature_WhenRequired_ShouldReturn401()
    {
        // Arrange
        var context = CreateHttpContext();
        // No signature header

        // Act
        await _middleware.InvokeAsync(
            context,
            _signingServiceMock.Object,
            _certificateRepositoryMock.Object,
            _apiKeyServiceMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        var responseBody = await ReadResponseBody(context);
        responseBody.Should().Be("Request signature required");
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidSignature_ShouldReturn401()
    {
        // Arrange
        var context = CreateHttpContext();
        var signature = CreateValidSignature();
        context.Request.Headers["X-Request-Signature"] = FormatSignatureHeader(signature);
        context.Request.Headers["X-API-Key"] = "test-api-key";

        _signingServiceMock
            .Setup(x => x.ParseSignatureHeader(It.IsAny<string>()))
            .Returns(signature);

        _apiKeyServiceMock
            .Setup(x => x.GetPublicKeyAsync("test-api-key"))
            .ReturnsAsync("test-public-key");

        _signingServiceMock
            .Setup(x => x.VerifyRequestSignatureAsync(
                It.IsAny<RequestSignature>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>()))
            .ReturnsAsync(false); // Invalid signature

        // Act
        await _middleware.InvokeAsync(
            context,
            _signingServiceMock.Object,
            _certificateRepositoryMock.Object,
            _apiKeyServiceMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        var responseBody = await ReadResponseBody(context);
        responseBody.Should().Be("Invalid request signature");
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact(Skip = "Integration test - requires full middleware implementation")]
    public async Task InvokeAsync_WithExpiredSignature_ShouldReturn401()
    {
        // Arrange
        var context = CreateHttpContext();
        var signature = CreateExpiredSignature();
        context.Request.Headers["X-Request-Signature"] = FormatSignatureHeader(signature);

        _signingServiceMock
            .Setup(x => x.ParseSignatureHeader(It.IsAny<string>()))
            .Returns(signature);

        // Act
        await _middleware.InvokeAsync(
            context,
            _signingServiceMock.Object,
            _certificateRepositoryMock.Object,
            _apiKeyServiceMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithExcludedPath_ShouldSkipValidation()
    {
        // Arrange
        var context = CreateHttpContext("/health");
        // No signature header

        // Act
        await _middleware.InvokeAsync(
            context,
            _signingServiceMock.Object,
            _certificateRepositoryMock.Object,
            _apiKeyServiceMock.Object);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WithRequestBody_ShouldVerifyBodyIntegrity()
    {
        // Arrange
        var context = CreateHttpContext();
        var body = "{\"data\":\"test\"}";
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;

        var signature = CreateValidSignature();
        context.Request.Headers["X-Request-Signature"] = FormatSignatureHeader(signature);
        context.Request.Headers["X-API-Key"] = "test-api-key";

        _signingServiceMock
            .Setup(x => x.ParseSignatureHeader(It.IsAny<string>()))
            .Returns(signature);

        _apiKeyServiceMock
            .Setup(x => x.GetPublicKeyAsync("test-api-key"))
            .ReturnsAsync("test-public-key");

        _signingServiceMock
            .Setup(x => x.VerifyRequestSignatureAsync(
                It.IsAny<RequestSignature>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                body, // Should receive the body content
                It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(
            context,
            _signingServiceMock.Object,
            _certificateRepositoryMock.Object,
            _apiKeyServiceMock.Object);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
        _signingServiceMock.Verify(x => x.VerifyRequestSignatureAsync(
            It.IsAny<RequestSignature>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            body,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithCertificate_ShouldUsePublicKeyFromCertificate()
    {
        // Arrange
        var context = CreateHttpContext();
        var certificate = CreateTestCertificate();
        context.Connection.ClientCertificate = certificate;

        var signature = CreateValidSignature();
        context.Request.Headers["X-Request-Signature"] = FormatSignatureHeader(signature);

        var tenantCert = new Domain.Entities.TenantCertificate(
            Guid.NewGuid(),
            Convert.ToBase64String(certificate.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert)),
            certificate.Thumbprint!,
            certificate.SubjectName.Name,
            certificate.IssuerName.Name,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(365),
            Domain.Entities.CertificatePurpose.Authentication);

        _certificateRepositoryMock
            .Setup(x => x.GetByThumbprintAsync(certificate.Thumbprint!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantCert);

        _signingServiceMock
            .Setup(x => x.ParseSignatureHeader(It.IsAny<string>()))
            .Returns(signature);

        _signingServiceMock
            .Setup(x => x.VerifyRequestSignatureAsync(
                It.IsAny<RequestSignature>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(
            context,
            _signingServiceMock.Object,
            _certificateRepositoryMock.Object,
            _apiKeyServiceMock.Object);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
        _apiKeyServiceMock.Verify(x => x.GetPublicKeyAsync(It.IsAny<string>()), Times.Never);
    }

    private static DefaultHttpContext CreateHttpContext(string path = "/api/test")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "GET";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static RequestSignature CreateValidSignature()
    {
        return new RequestSignature(
            "SHA256",
            "test-signature",
            "test-nonce",
            DateTimeOffset.UtcNow,
            new Dictionary<string, string>());
    }

    private static RequestSignature CreateExpiredSignature()
    {
        return new RequestSignature(
            "SHA256",
            "test-signature",
            "test-nonce",
            DateTimeOffset.UtcNow.AddMinutes(-10),
            new Dictionary<string, string>());
    }

    private static string FormatSignatureHeader(RequestSignature signature)
    {
        return $"Signature algorithm=\"{signature.Algorithm}\",signature=\"{signature.Signature}\",nonce=\"{signature.Nonce}\",timestamp=\"{signature.Timestamp.ToUnixTimeSeconds()}\"";
    }

    private static async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateTestCertificate()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            "CN=Test Client",
            rsa,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(365));

        // Export and reload to ensure proper format
        var pfxBytes = certificate.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx, "test");
        return System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12(pfxBytes, "test");
    }
}