using FluentAssertions;
using NumbatWallet.Domain.Entities;
using NumbatWallet.SharedKernel.Enums;
using Xunit;

namespace NumbatWallet.Domain.Tests.Entities;

public class TenantCertificateTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateCertificate()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var certificateData = "base64-certificate-data";
        var thumbprint = "ABC123DEF456";
        var subjectDn = "CN=test.example.com";
        var issuerDn = "CN=Example CA";
        var validFrom = DateTimeOffset.UtcNow;
        var validTo = validFrom.AddYears(1);
        var purpose = CertificatePurpose.Authentication;

        // Act
        var certificate = new TenantCertificate(
            tenantId,
            certificateData,
            thumbprint,
            subjectDn,
            issuerDn,
            validFrom,
            validTo,
            purpose);

        // Assert
        certificate.TenantId.Should().Be(tenantId);
        certificate.CertificateData.Should().Be(certificateData);
        certificate.Thumbprint.Should().Be(thumbprint.ToUpperInvariant());
        certificate.SubjectDn.Should().Be(subjectDn);
        certificate.IssuerDn.Should().Be(issuerDn);
        certificate.ValidFrom.Should().Be(validFrom);
        certificate.ValidTo.Should().Be(validTo);
        certificate.Purpose.Should().Be(purpose);
        certificate.IsActive.Should().BeTrue();
        certificate.IsRevoked.Should().BeFalse();
        certificate.TrustLevel.Should().Be(CertificateTrustLevel.Low);
    }

    [Fact]
    public void Constructor_WithEmptyTenantId_ShouldThrow()
    {
        // Arrange & Act
        var act = () => new TenantCertificate(
            Guid.Empty,
            "certificate-data",
            "thumbprint",
            "CN=test",
            "CN=issuer",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(1),
            CertificatePurpose.All);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*tenantId*");
    }

    [Fact]
    public void Constructor_WithInvalidDateRange_ShouldThrow()
    {
        // Arrange
        var validFrom = DateTimeOffset.UtcNow;
        var validTo = validFrom.AddDays(-1); // Invalid: expiry before start

        // Act
        var act = () => new TenantCertificate(
            Guid.NewGuid(),
            "certificate-data",
            "thumbprint",
            "CN=test",
            "CN=issuer",
            validFrom,
            validTo,
            CertificatePurpose.All);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*expiry*after*");
    }

    [Fact]
    public void Activate_WhenNotExpiredOrRevoked_ShouldActivate()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        certificate.Deactivate();
        certificate.IsActive.Should().BeFalse();

        // Act
        certificate.Activate();

        // Assert
        certificate.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenExpired_ShouldThrow()
    {
        // Arrange
        var certificate = CreateExpiredCertificate();

        // Act
        var act = () => certificate.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public void Activate_WhenRevoked_ShouldThrow()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        certificate.Revoke("Test revocation");

        // Act
        var act = () => certificate.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*revoked*");
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        certificate.IsActive.Should().BeTrue();

        // Act
        certificate.Deactivate();

        // Assert
        certificate.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithValidReason_ShouldRevokeCertificate()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        var reason = "Security breach detected";

        // Act
        certificate.Revoke(reason);

        // Assert
        certificate.IsRevoked.Should().BeTrue();
        certificate.RevokedAt.Should().NotBeNull();
        certificate.RevokedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        certificate.RevocationReason.Should().Be(reason);
        certificate.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldThrow()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        certificate.Revoke("First revocation");

        // Act
        var act = () => certificate.Revoke("Second revocation");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already revoked*");
    }

    [Fact]
    public void UpdateTrustLevel_WithValidLevel_ShouldUpdateLevel()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        var newLevel = CertificateTrustLevel.High;

        // Act
        certificate.UpdateTrustLevel(newLevel);

        // Assert
        certificate.TrustLevel.Should().Be(newLevel);
    }

    [Fact]
    public void UpdateTrustLevel_WhenRevoked_ShouldThrow()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        certificate.Revoke("Test");

        // Act
        var act = () => certificate.UpdateTrustLevel(CertificateTrustLevel.Full);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*revoked*");
    }

    [Fact]
    public void IsExpired_WhenExpired_ShouldReturnTrue()
    {
        // Arrange
        var certificate = CreateExpiredCertificate();

        // Act
        var isExpired = certificate.IsExpired();

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenValid_ShouldReturnFalse()
    {
        // Arrange
        var certificate = CreateValidCertificate();

        // Act
        var isExpired = certificate.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsValidAt_WithValidDate_ShouldReturnTrue()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        var checkDate = DateTimeOffset.UtcNow;

        // Act
        var isValid = certificate.IsValidAt(checkDate);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValidAt_WithDateBeforeValidFrom_ShouldReturnFalse()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        var checkDate = certificate.ValidFrom.AddDays(-1);

        // Act
        var isValid = certificate.IsValidAt(checkDate);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValidAt_WithDateAfterValidTo_ShouldReturnFalse()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        var checkDate = certificate.ValidTo.AddDays(1);

        // Act
        var isValid = certificate.IsValidAt(checkDate);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValidAt_WhenRevoked_ShouldReturnFalse()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        certificate.Revoke("Test");
        var checkDate = DateTimeOffset.UtcNow;

        // Act
        var isValid = certificate.IsValidAt(checkDate);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void CanBeUsedForPurpose_WithMatchingPurpose_ShouldReturnTrue()
    {
        // Arrange
        var certificate = new TenantCertificate(
            Guid.NewGuid(),
            "cert-data",
            "thumbprint",
            "CN=test",
            "CN=issuer",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1),
            CertificatePurpose.Authentication);

        // Act
        var canUse = certificate.CanBeUsedForPurpose(CertificatePurpose.Authentication);

        // Assert
        canUse.Should().BeTrue();
    }

    [Fact]
    public void CanBeUsedForPurpose_WithAllPurpose_ShouldReturnTrueForAny()
    {
        // Arrange
        var certificate = new TenantCertificate(
            Guid.NewGuid(),
            "cert-data",
            "thumbprint",
            "CN=test",
            "CN=issuer",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1),
            CertificatePurpose.All);

        // Act & Assert
        certificate.CanBeUsedForPurpose(CertificatePurpose.Authentication).Should().BeTrue();
        certificate.CanBeUsedForPurpose(CertificatePurpose.Signing).Should().BeTrue();
        certificate.CanBeUsedForPurpose(CertificatePurpose.Encryption).Should().BeTrue();
    }

    [Fact]
    public void CanBeUsedForPurpose_WithMismatchedPurpose_ShouldReturnFalse()
    {
        // Arrange
        var certificate = new TenantCertificate(
            Guid.NewGuid(),
            "cert-data",
            "thumbprint",
            "CN=test",
            "CN=issuer",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1),
            CertificatePurpose.Authentication);

        // Act
        var canUse = certificate.CanBeUsedForPurpose(CertificatePurpose.Signing);

        // Assert
        canUse.Should().BeFalse();
    }

    [Fact]
    public void UpdateLastUsed_ShouldUpdateTimestampAndIncrementUsageCount()
    {
        // Arrange
        var certificate = CreateValidCertificate();
        var initialCount = certificate.UsageCount;
        certificate.LastUsedAt.Should().BeNull();

        // Act
        certificate.UpdateLastUsed();

        // Assert
        certificate.LastUsedAt.Should().NotBeNull();
        certificate.LastUsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        certificate.UsageCount.Should().Be(initialCount + 1);
    }

    // Helper methods
    private static TenantCertificate CreateValidCertificate()
    {
        return new TenantCertificate(
            Guid.NewGuid(),
            "base64-certificate-data",
            "ABC123",
            "CN=test.example.com",
            "CN=Example CA",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1),
            CertificatePurpose.All);
    }

    private static TenantCertificate CreateExpiredCertificate()
    {
        return new TenantCertificate(
            Guid.NewGuid(),
            "base64-certificate-data",
            "XYZ789",
            "CN=expired.example.com",
            "CN=Example CA",
            DateTimeOffset.UtcNow.AddYears(-2),
            DateTimeOffset.UtcNow.AddYears(-1),
            CertificatePurpose.All);
    }
}