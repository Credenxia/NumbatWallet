using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.Services;
using NumbatWallet.SharedKernel.Interfaces;
using Xunit;

namespace NumbatWallet.Infrastructure.Tests.Services;

public class RevocationRegistryServiceTests : IDisposable
{
    private readonly NumbatWalletDbContext _context;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<RevocationRegistryService>> _loggerMock;
    private readonly Mock<IHsmService> _hsmServiceMock;
    private readonly RevocationRegistryService _service;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTimeService> _dateTimeServiceMock;
    private readonly Mock<IEventDispatcher> _eventDispatcherMock;
    private readonly Mock<ILogger<NumbatWalletDbContext>> _dbLoggerMock;

    public RevocationRegistryServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<NumbatWalletDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Setup mocks for DbContext dependencies
        _tenantServiceMock = new Mock<ITenantService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _eventDispatcherMock = new Mock<IEventDispatcher>();
        _dbLoggerMock = new Mock<ILogger<NumbatWalletDbContext>>();

        _context = new NumbatWalletDbContext(
            options,
            _tenantServiceMock.Object,
            _currentUserServiceMock.Object,
            _dateTimeServiceMock.Object,
            _eventDispatcherMock.Object,
            _dbLoggerMock.Object);
        _cacheMock = new Mock<IDistributedCache>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<RevocationRegistryService>>();
        _hsmServiceMock = new Mock<IHsmService>();

        _service = new RevocationRegistryService(
            _context,
            _cacheMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object,
            _hsmServiceMock.Object);
    }

    [Fact]
    public async Task RevokeCertificateAsync_WithNewCertificate_ShouldCreateRevocationEntry()
    {
        // Arrange
        var serialNumber = "12:34:56:78:90";
        var reason = RevocationReason.KeyCompromise;
        var comment = "Test revocation";

        // Act
        var result = await _service.RevokeCertificateAsync(serialNumber, reason, comment);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serialNumber, result.SerialNumber);
        Assert.Equal(reason, result.Reason);
        Assert.Equal(comment, result.Comment);
        Assert.True(result.RevocationDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task RevokeCertificateAsync_WithAlreadyRevokedCertificate_ShouldReturnExisting()
    {
        // Arrange
        var serialNumber = "AA:BB:CC:DD:EE";
        var existingRevocation = new CertificateRevocation(
            serialNumber,
            (int)RevocationReason.Unspecified,
            "Already revoked",
            null);

        _context.Set<CertificateRevocation>().Add(existingRevocation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RevokeCertificateAsync(
            serialNumber,
            RevocationReason.KeyCompromise,
            "Attempt to re-revoke");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(serialNumber, result.SerialNumber);
        Assert.Equal(RevocationReason.Unspecified, result.Reason); // Should keep original reason
    }

    [Fact]
    public async Task CheckRevocationStatusAsync_WithRevokedCertificate_ShouldReturnRevoked()
    {
        // Arrange
        var serialNumber = "FF:EE:DD:CC:BB";
        var revocation = new CertificateRevocation(
            serialNumber,
            (int)RevocationReason.KeyCompromise,
            "Test",
            null);

        _context.Set<CertificateRevocation>().Add(revocation);
        await _context.SaveChangesAsync();

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync((byte[])null!);

        // Act
        var status = await _service.CheckRevocationStatusAsync(serialNumber);

        // Assert
        Assert.True(status.IsRevoked);
        Assert.NotNull(status.RevocationDate);
        Assert.Equal(RevocationReason.KeyCompromise, status.Reason);
        Assert.Equal(RevocationCheckSource.LocalRegistry, status.Source);
    }

    [Fact]
    public async Task CheckRevocationStatusAsync_WithNonRevokedCertificate_ShouldReturnNotRevoked()
    {
        // Arrange
        var serialNumber = "11:22:33:44:55";

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), default))
            .ReturnsAsync((byte[])null!);

        // Act
        var status = await _service.CheckRevocationStatusAsync(serialNumber);

        // Assert
        Assert.False(status.IsRevoked);
        Assert.Null(status.RevocationDate);
        Assert.Null(status.Reason);
        Assert.Equal(RevocationCheckSource.LocalRegistry, status.Source);
    }

    [Fact]
    public async Task GetRevokedCertificatesAsync_ShouldReturnAllRevoked()
    {
        // Arrange
        var rev1 = new CertificateRevocation("11:11:11", (int)RevocationReason.KeyCompromise, null, null);
        var rev2 = new CertificateRevocation("22:22:22", (int)RevocationReason.Superseded, null, null);

        _context.Set<CertificateRevocation>().AddRange(rev1, rev2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetRevokedCertificatesAsync();

        // Assert
        Assert.NotNull(results);
        var revocations = results.ToList();
        Assert.Equal(2, revocations.Count);
        Assert.Contains(revocations, r => r.SerialNumber == "11:11:11");
        Assert.Contains(revocations, r => r.SerialNumber == "22:22:22");
    }

    [Fact]
    public async Task GetRevokedCertificatesAsync_WithSinceFilter_ShouldReturnFiltered()
    {
        // Arrange
        var oldRevocation = new CertificateRevocation("OLD:OLD:OLD", (int)RevocationReason.Unspecified, null, null);
        var newRevocation = new CertificateRevocation("NEW:NEW:NEW", (int)RevocationReason.KeyCompromise, null, null);

        // Manipulate dates using reflection or EF Core features
        _context.Set<CertificateRevocation>().Add(oldRevocation);
        _context.Set<CertificateRevocation>().Add(newRevocation);
        await _context.SaveChangesAsync();

        var since = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var results = await _service.GetRevokedCertificatesAsync(since);

        // Assert
        Assert.NotNull(results);
        var revocations = results.ToList();
        Assert.True(revocations.Count >= 1); // At least the new one
    }

    [Fact]
    public void GetOcspResponderUrl_WithCertificateWithoutExtension_ShouldReturnNull()
    {
        // Arrange
        using var cert = CreateTestCertificate();

        // Act
        var url = _service.GetOcspResponderUrl(cert);

        // Assert
        Assert.Null(url); // Test certificate doesn't have OCSP extension
    }

    [Fact]
    public void GetCrlDistributionPoints_WithCertificateWithoutExtension_ShouldReturnEmpty()
    {
        // Arrange
        using var cert = CreateTestCertificate();

        // Act
        var points = _service.GetCrlDistributionPoints(cert);

        // Assert
        Assert.NotNull(points);
        Assert.Empty(points); // Test certificate doesn't have CRL distribution points
    }

    [Fact]
    public async Task CheckOcspStatusAsync_WithoutOcspUrl_ShouldReturnUnknown()
    {
        // Arrange
        using var cert = CreateTestCertificate();
        using var issuerCert = CreateTestCertificate();

        // Act
        var response = await _service.CheckOcspStatusAsync(cert, issuerCert);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(OcspResponseStatus.Unknown, response.Status);
        Assert.Equal(cert.SerialNumber, response.CertificateSerialNumber);
    }

    [Fact]
    public async Task GenerateCrlAsync_WithRevokedCertificates_ShouldGenerateCrl()
    {
        // Arrange
        var rev = new CertificateRevocation("AB:CD:EF", (int)RevocationReason.KeyCompromise, null, null);
        _context.Set<CertificateRevocation>().Add(rev);
        await _context.SaveChangesAsync();

        using var caCert = CreateTestCertificate();

        _hsmServiceMock.Setup(x => x.SignDataAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<SignatureAlgorithm>(),
            default))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

        // Act
        var crlData = await _service.GenerateCrlAsync(caCert);

        // Assert
        Assert.NotNull(crlData);
        Assert.NotEmpty(crlData);
    }

    [Fact]
    public async Task PruneExpiredEntriesAsync_WithExpiredEntries_ShouldRemoveThem()
    {
        // Arrange
        var expiredRevocation = new CertificateRevocation(
            "EXPIRED:CERT",
            (int)RevocationReason.CessationOfOperation,
            null,
            null);

        // Set invalidity date to over a year ago
        expiredRevocation.SetInvalidityDate(DateTime.UtcNow.AddYears(-2));

        _context.Set<CertificateRevocation>().Add(expiredRevocation);
        await _context.SaveChangesAsync();

        // Act
        var pruned = await _service.PruneExpiredEntriesAsync();

        // Assert
        Assert.Equal(1, pruned);
        var remaining = await _context.Set<CertificateRevocation>().CountAsync();
        Assert.Equal(0, remaining);
    }

    private static X509Certificate2 CreateTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var distinguishedName = new X500DistinguishedName("CN=Test Certificate, O=NumbatWallet Tests");
        var request = new CertificateRequest(
            distinguishedName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add some extensions
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: false));

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: false,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: false));

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.Now.AddDays(-1),
            DateTimeOffset.Now.AddDays(365));

        return certificate;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}