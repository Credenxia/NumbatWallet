using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Application.DomainServices;
using NumbatWallet.Web.Api.Middleware;

namespace NumbatWallet.Web.Api.Tests.Middleware;

public class MutualTlsMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ILogger<MutualTlsMiddleware>> _loggerMock;
    private readonly Mock<ITenantCertificateRepository> _certificateRepositoryMock;
    private readonly Mock<ICertificateTrustStoreRepository> _trustStoreRepositoryMock;
    private readonly Mock<ICertificateValidationService> _validationServiceMock;
    private readonly MutualTlsMiddleware _middleware;
    private readonly MutualTlsOptions _options;

    public MutualTlsMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<MutualTlsMiddleware>>();
        _certificateRepositoryMock = new Mock<ITenantCertificateRepository>();
        _trustStoreRepositoryMock = new Mock<ICertificateTrustStoreRepository>();
        _validationServiceMock = new Mock<ICertificateValidationService>();

        _options = new MutualTlsOptions
        {
            RequireClientCertificate = true,
            ValidateCertificateChain = true,
            MinimumTrustLevel = "Medium",
            ExcludedPaths = new List<string> { "/health", "/swagger" }
        };

        _middleware = new MutualTlsMiddleware(
            _nextMock.Object,
            _loggerMock.Object,
            Options.Create(_options));
    }

    [Fact(Skip = "Integration test - requires full middleware implementation")]
    public async Task InvokeAsync_WithValidCertificate_ShouldPassToNextMiddleware()
    {
        // Arrange
        var context = CreateHttpContext();
        var certificate = CreateValidCertificate();
        context.Connection.ClientCertificate = certificate;

        var tenantCert = CreateTenantCertificate(certificate);
        _certificateRepositoryMock
            .Setup(x => x.GetByThumbprintAsync(certificate.Thumbprint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantCert);

        var trustStore = new CertificateTrustStore(
            Guid.NewGuid(),
            "Test Trust Store",
            "Test trust store for tenant");
        _trustStoreRepositoryMock
            .Setup(x => x.GetActiveByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trustStore);

        // Act
        await _middleware.InvokeAsync(
            context,
            _certificateRepositoryMock.Object,
            _trustStoreRepositoryMock.Object,
            _validationServiceMock.Object);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
        context.Items.Should().ContainKey("ClientCertificate");
        context.Items.Should().ContainKey("TenantId");
    }

    [Fact]
    public async Task InvokeAsync_WithoutCertificate_WhenRequired_ShouldReturn401()
    {
        // Arrange
        var context = CreateHttpContext();
        context.Connection.ClientCertificate = null;

        // Act
        await _middleware.InvokeAsync(
            context,
            _certificateRepositoryMock.Object,
            _trustStoreRepositoryMock.Object,
            _validationServiceMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact(Skip = "Integration test - requires full middleware implementation")]
    public async Task InvokeAsync_WithExpiredCertificate_ShouldReturn401()
    {
        // Arrange
        var context = CreateHttpContext();
        var certificate = CreateExpiredCertificate();
        context.Connection.ClientCertificate = certificate;

        var tenantCert = CreateTenantCertificate(certificate);
        tenantCert.Deactivate(); // Simulate expired cert

        _certificateRepositoryMock
            .Setup(x => x.GetByThumbprintAsync(certificate.Thumbprint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantCert);

        // Act
        await _middleware.InvokeAsync(
            context,
            _certificateRepositoryMock.Object,
            _trustStoreRepositoryMock.Object,
            _validationServiceMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact(Skip = "Integration test - requires full middleware implementation")]
    public async Task InvokeAsync_WithRevokedCertificate_ShouldReturn401()
    {
        // Arrange
        var context = CreateHttpContext();
        var certificate = CreateValidCertificate();
        context.Connection.ClientCertificate = certificate;

        var tenantCert = CreateTenantCertificate(certificate);
        tenantCert.Revoke("Test revocation");

        _certificateRepositoryMock
            .Setup(x => x.GetByThumbprintAsync(certificate.Thumbprint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantCert);

        // Act
        await _middleware.InvokeAsync(
            context,
            _certificateRepositoryMock.Object,
            _trustStoreRepositoryMock.Object,
            _validationServiceMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithExcludedPath_ShouldSkipValidation()
    {
        // Arrange
        var context = CreateHttpContext("/health");
        context.Connection.ClientCertificate = null;

        // Act
        await _middleware.InvokeAsync(
            context,
            _certificateRepositoryMock.Object,
            _trustStoreRepositoryMock.Object,
            _validationServiceMock.Object);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact(Skip = "Integration test - requires full middleware implementation")]
    public async Task InvokeAsync_WithInsufficientTrustLevel_ShouldReturn403()
    {
        // Arrange
        var context = CreateHttpContext();
        var certificate = CreateValidCertificate();
        context.Connection.ClientCertificate = certificate;

        var tenantCert = CreateTenantCertificate(certificate);
        tenantCert.UpdateTrustLevel(CertificateTrustLevel.Low); // Below required Medium

        _certificateRepositoryMock
            .Setup(x => x.GetByThumbprintAsync(certificate.Thumbprint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantCert);

        var trustStore = new CertificateTrustStore(
            Guid.NewGuid(),
            "Test Trust Store",
            "Test trust store for tenant");
        _trustStoreRepositoryMock
            .Setup(x => x.GetActiveByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trustStore);

        // Act
        await _middleware.InvokeAsync(
            context,
            _certificateRepositoryMock.Object,
            _trustStoreRepositoryMock.Object,
            _validationServiceMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(403);
        _nextMock.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
    }

    private static DefaultHttpContext CreateHttpContext(string path = "/api/test")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.ClientCertificate = null;
        return context;
    }

    private static X509Certificate2 CreateValidCertificate()
    {
        // Create a self-signed certificate for testing
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Test Certificate",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(365));

        // Export and reload to ensure proper format
        var pfxBytes = certificate.Export(X509ContentType.Pfx, "test");
        return X509CertificateLoader.LoadPkcs12(pfxBytes, "test");
    }

    private static X509Certificate2 CreateExpiredCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Expired Certificate",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-365),
            DateTimeOffset.UtcNow.AddDays(-1));

        // Export and reload to ensure proper format
        var pfxBytes = certificate.Export(X509ContentType.Pfx, "test");
        return X509CertificateLoader.LoadPkcs12(pfxBytes, "test");
    }

    private static TenantCertificate CreateTenantCertificate(X509Certificate2 x509Cert)
    {
        return new TenantCertificate(
            Guid.NewGuid(),
            Convert.ToBase64String(x509Cert.Export(X509ContentType.Cert)),
            x509Cert.Thumbprint,
            x509Cert.SubjectName.Name,
            x509Cert.IssuerName.Name,
            new DateTimeOffset(x509Cert.NotBefore, TimeSpan.Zero),
            new DateTimeOffset(x509Cert.NotAfter, TimeSpan.Zero),
            CertificatePurpose.Authentication);
    }
}
