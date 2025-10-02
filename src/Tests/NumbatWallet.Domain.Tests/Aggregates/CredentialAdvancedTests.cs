using FluentAssertions;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;
using Xunit;

namespace NumbatWallet.Domain.Tests.Aggregates;

/// <summary>
/// Advanced unit tests for Credential aggregate
/// Tests edge cases, validation, and complex scenarios
/// </summary>
public class CredentialAdvancedTests
{
    #region Creation Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Credential_Create_WithEmptyType_ShouldFail(string emptyType)
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var credentialData = """{"test":"data"}""";
        var schemaId = "https://schema.org/test/v1";

        // Act
        var result = Credential.Create(walletId, issuerId, emptyType, credentialData, schemaId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().ContainEquivalentOf("Type");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Credential_Create_WithEmptyData_ShouldFail(string emptyData)
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var credentialType = "TestCredential";
        var schemaId = "https://schema.org/test/v1";

        // Act
        var result = Credential.Create(walletId, issuerId, credentialType, emptyData, schemaId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().ContainEquivalentOf("Data");
    }

    [Fact]
    public void Credential_Create_WithEmptyWalletId_ShouldFail()
    {
        // Arrange
        var walletId = Guid.Empty;
        var issuerId = Guid.NewGuid();
        var credentialType = "TestCredential";
        var credentialData = """{"test":"data"}""";
        var schemaId = "https://schema.org/test/v1";

        // Act
        var result = Credential.Create(walletId, issuerId, credentialType, credentialData, schemaId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("walletId");
    }

    [Fact]
    public void Credential_Create_WithEmptyIssuerId_ShouldFail()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.Empty;
        var credentialType = "TestCredential";
        var credentialData = """{"test":"data"}""";
        var schemaId = "https://schema.org/test/v1";

        // Act
        var result = Credential.Create(walletId, issuerId, credentialType, credentialData, schemaId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("issuerId");
    }

    [Fact]
    public void Credential_Create_WithValidData_ShouldSetIssuedAtToUtcNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();

        // Act
        var result = Credential.Create(walletId, issuerId, "Test", """{"test":"data"}""", "https://schema.org/test/v1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var after = DateTimeOffset.UtcNow;
        result.Value.IssuedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    #endregion

    #region Status Transition Tests

    [Fact]
    public void Credential_Suspend_FromPendingStatus_ShouldFail()
    {
        // Arrange
        var credential = CreateTestCredential();
        // Credential starts in Pending status

        // Act
        var result = credential.Suspend("Test suspension");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("pending");
    }

    [Fact]
    public void Credential_Suspend_WithEmptyReason_ShouldFail()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();

        // Act
        var result = credential.Suspend("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("reason");
    }

    [Fact]
    public void Credential_Revoke_WithEmptyReason_ShouldFail()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();

        // Act
        var result = credential.Revoke("   ");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("reason");
    }

    [Fact]
    public void Credential_Activate_AfterRevoke_ShouldFail()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();
        credential.Revoke("Test revocation");

        // Act
        var result = credential.Activate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("revoked");
    }

    [Fact]
    public void Credential_Activate_AfterExpiry_ShouldFail()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.SetExpiry(DateTimeOffset.UtcNow.AddDays(-1));

        // Act
        var result = credential.Activate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("expired");
    }

    [Fact]
    public void Credential_StatusTransitions_ShouldBeTracked()
    {
        // Arrange
        var credential = CreateTestCredential();

        // Act & Assert - Track status changes
        credential.Status.Should().Be(CredentialStatus.Pending);

        credential.Activate().IsSuccess.Should().BeTrue();
        credential.Status.Should().Be(CredentialStatus.Active);

        credential.Suspend("Test").IsSuccess.Should().BeTrue();
        credential.Status.Should().Be(CredentialStatus.Suspended);

        credential.Activate().IsSuccess.Should().BeTrue();
        credential.Status.Should().Be(CredentialStatus.Active);

        credential.Revoke("Final").IsSuccess.Should().BeTrue();
        credential.Status.Should().Be(CredentialStatus.Revoked);
    }

    #endregion

    #region Expiry Tests

    [Fact]
    public void Credential_SetExpiry_WithFutureDate_ShouldNotChangeStatus()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();
        var futureDate = DateTimeOffset.UtcNow.AddYears(1);

        // Act
        var result = credential.SetExpiry(futureDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        credential.Status.Should().Be(CredentialStatus.Active);
        credential.ExpiresAt.Should().Be(futureDate);
        credential.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void Credential_SetExpiry_WithPastDate_ShouldSetExpiredStatus()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var result = credential.SetExpiry(pastDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        credential.Status.Should().Be(CredentialStatus.Expired);
        credential.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void Credential_IsExpired_WithNullExpiry_ShouldReturnFalse()
    {
        // Arrange
        var credential = CreateTestCredential();

        // Act
        var isExpired = credential.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void Credential_IsExpired_WithFutureExpiry_ShouldReturnFalse()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.SetExpiry(DateTimeOffset.UtcNow.AddYears(1));

        // Act
        var isExpired = credential.IsExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void Credential_SetExpiry_OnRevokedCredential_ShouldFail()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();
        credential.Revoke("Test");

        // Act
        var result = credential.SetExpiry(DateTimeOffset.UtcNow.AddYears(1));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("revoked");
    }

    #endregion

    #region Update Data Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Credential_UpdateData_WithEmptyData_ShouldFail(string emptyData)
    {
        // Arrange
        var credential = CreateTestCredential();

        // Act
        var result = credential.UpdateData(emptyData);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().ContainEquivalentOf("Data");
    }

    [Fact]
    public void Credential_UpdateData_OnRevokedCredential_ShouldFail()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();
        credential.Revoke("Test");

        // Act
        var result = credential.UpdateData("""{"updated":"data"}""");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("revoked");
    }

    [Fact]
    public void Credential_UpdateData_OnExpiredCredential_ShouldFail()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.SetExpiry(DateTimeOffset.UtcNow.AddDays(-1));

        // Act
        var result = credential.UpdateData("""{"updated":"data"}""");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("expired");
    }

    [Fact]
    public void Credential_UpdateData_WithValidJSON_ShouldSucceed()
    {
        // Arrange
        var credential = CreateTestCredential();
        var newData = """
        {
            "name": "John Doe",
            "licenseNumber": "ABC123",
            "class": "C",
            "expiryDate": "2025-12-31"
        }
        """;

        // Act
        var result = credential.UpdateData(newData);

        // Assert
        result.IsSuccess.Should().BeTrue();
        credential.CredentialData.Should().Be(newData);
    }

    #endregion

    #region Revocation Tests

    [Fact]
    public void Credential_Revoke_ShouldSetRevokedAtTimestamp()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();
        var before = DateTimeOffset.UtcNow;

        // Act
        credential.Revoke("Test revocation");

        // Assert
        credential.RevokedAt.Should().NotBeNull();
        credential.RevokedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Credential_Revoke_FromSuspendedStatus_ShouldSucceed()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();
        credential.Suspend("Test");

        // Act
        var result = credential.Revoke("Final revocation");

        // Assert
        result.IsSuccess.Should().BeTrue();
        credential.Status.Should().Be(CredentialStatus.Revoked);
        credential.RevokedAt.Should().NotBeNull();
    }

    #endregion

    #region Schema Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Credential_Create_WithEmptySchema_ShouldFail(string emptySchema)
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();

        // Act
        var result = Credential.Create(walletId, issuerId, "Test", """{"test":"data"}""", emptySchema);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("schema");
    }

    [Fact]
    public void Credential_Create_WithValidSchemaUrl_ShouldSucceed()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var schemaId = "https://schema.numbat.wa.gov.au/credentials/driver-licence/v1.0";

        // Act
        var result = Credential.Create(walletId, issuerId, "DriverLicence", """{"test":"data"}""", schemaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SchemaId.Should().Be(schemaId);
    }

    #endregion

    #region Edge Cases and Boundaries

    [Fact]
    public void Credential_Create_WithLargeDataPayload_ShouldSucceed()
    {
        // Arrange - Create 100KB JSON payload
        var largeData = """
        {
            "data": "
        """ + new string('A', 100000) + """
        "
        }
        """;

        // Act
        var result = Credential.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LargeCredential",
            largeData,
            "https://schema.org/large/v1");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Credential_MultipleStatusChanges_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var credential = CreateTestCredential();
        var originalData = credential.CredentialData;

        // Act - Multiple status changes
        credential.Activate();
        credential.Suspend("Test 1");
        credential.Activate();
        credential.Suspend("Test 2");
        credential.Activate();

        // Assert - Data should remain unchanged
        credential.CredentialData.Should().Be(originalData);
        credential.Status.Should().Be(CredentialStatus.Active);
    }

    [Fact]
    public void Credential_Properties_ShouldBeImmutableAfterCreation()
    {
        // Arrange & Act
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var credential = Credential.Create(
            walletId,
            issuerId,
            "TestCredential",
            """{"test":"data"}""",
            "https://schema.org/test/v1").Value;

        // Assert - Core properties should not change
        credential.WalletId.Should().Be(walletId);
        credential.IssuerId.Should().Be(issuerId);
        credential.CredentialType.Should().Be("TestCredential");
        credential.SchemaId.Should().Be("https://schema.org/test/v1");
    }

    #endregion

    private static Credential CreateTestCredential()
    {
        return Credential.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TestCredential",
            """{"test":"data"}""",
            "https://schema.test/v1"
        ).Value;
    }
}
