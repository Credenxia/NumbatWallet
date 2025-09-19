using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Domain.Tests.Aggregates;

public class CredentialTests
{
    [Fact]
    public void Credential_Create_ShouldInitializeCorrectly()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var credentialType = "DriverLicence";
        var credentialData = """{"licenseNumber":"DL123456","class":"C"}""";
        var schemaId = "https://schema.org/driverlicence/v1";

        // Act
        var result = Credential.Create(
            walletId,
            issuerId,
            credentialType,
            credentialData,
            schemaId
        );

        // Assert
        Assert.True(result.IsSuccess);
        var credential = result.Value;
        Assert.NotEqual(Guid.Empty, credential.Id);
        Assert.Equal(walletId, credential.WalletId);
        Assert.Equal(issuerId, credential.IssuerId);
        Assert.Equal(credentialType, credential.CredentialType);
        Assert.Equal(credentialData, credential.CredentialData);
        Assert.Equal(schemaId, credential.SchemaId);
        Assert.Equal(CredentialStatus.Pending, credential.Status);
        Assert.NotEqual(default(DateTimeOffset), credential.IssuedAt);
        Assert.Null(credential.ExpiresAt);
        Assert.Null(credential.RevokedAt);
    }

    [Fact]
    public void Credential_Activate_ShouldChangeStatusToActive()
    {
        // Arrange
        var credential = CreateTestCredential();

        // Act
        var result = credential.Activate();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(CredentialStatus.Active, credential.Status);
    }

    [Fact]
    public void Credential_Activate_WhenAlreadyActive_ShouldFail()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();

        // Act
        var result = credential.Activate();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already active", result.Error.Message);
    }

    [Fact]
    public void Credential_Suspend_ShouldChangeStatusToSuspended()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();
        var reason = "Security review required";

        // Act
        var result = credential.Suspend(reason);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(CredentialStatus.Suspended, credential.Status);
    }

    [Fact]
    public void Credential_Revoke_ShouldChangeStatusToRevoked()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();
        var reason = "Fraudulent activity detected";

        // Act
        var result = credential.Revoke(reason);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(CredentialStatus.Revoked, credential.Status);
        Assert.NotNull(credential.RevokedAt);
    }

    [Fact]
    public void Credential_Revoke_WhenAlreadyRevoked_ShouldFail()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();
        credential.Revoke("Initial reason");

        // Act
        var result = credential.Revoke("Another reason");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already revoked", result.Error.Message);
    }

    [Fact]
    public void Credential_SetExpiry_ShouldUpdateExpiresAt()
    {
        // Arrange
        var credential = CreateTestCredential();
        var expiryDate = DateTimeOffset.UtcNow.AddYears(1);

        // Act
        var result = credential.SetExpiry(expiryDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expiryDate, credential.ExpiresAt);
    }

    [Fact]
    public void Credential_SetExpiry_WithPastDate_ShouldSetAsExpired()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.Activate();
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var result = credential.SetExpiry(pastDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(CredentialStatus.Expired, credential.Status);
        Assert.True(credential.IsExpired());
    }

    [Fact]
    public void Credential_IsExpired_ShouldReturnCorrectValue()
    {
        // Arrange
        var credential = CreateTestCredential();
        credential.SetExpiry(DateTimeOffset.UtcNow.AddDays(-1));

        // Act
        var isExpired = credential.IsExpired();

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void Credential_UpdateData_ShouldUpdateCredentialData()
    {
        // Arrange
        var credential = CreateTestCredential();
        var newData = """{"licenseNumber":"DL999999","class":"A"}""";

        // Act
        var result = credential.UpdateData(newData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newData, credential.CredentialData);
    }

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