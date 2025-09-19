using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.SharedKernel.Tests;

public class EnumsTests
{
    [Fact]
    public void CredentialStatus_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)CredentialStatus.Pending);
        Assert.Equal(1, (int)CredentialStatus.Active);
        Assert.Equal(2, (int)CredentialStatus.Suspended);
        Assert.Equal(3, (int)CredentialStatus.Revoked);
        Assert.Equal(4, (int)CredentialStatus.Expired);
    }

    [Fact]
    public void VerificationStatus_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)VerificationStatus.NotVerified);
        Assert.Equal(1, (int)VerificationStatus.Pending);
        Assert.Equal(2, (int)VerificationStatus.Verified);
        Assert.Equal(3, (int)VerificationStatus.Failed);
    }

    [Fact]
    public void WalletStatus_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)WalletStatus.Active);
        Assert.Equal(1, (int)WalletStatus.Inactive);
        Assert.Equal(2, (int)WalletStatus.Suspended);
        Assert.Equal(3, (int)WalletStatus.Locked);
    }

    [Fact]
    public void TenantStatus_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)TenantStatus.Provisioning);
        Assert.Equal(1, (int)TenantStatus.Active);
        Assert.Equal(2, (int)TenantStatus.Suspended);
        Assert.Equal(3, (int)TenantStatus.Deprovisioned);
    }

    [Fact]
    public void DataClassification_ShouldHaveExpectedValues()
    {
        // Assert - Australian PSPF/ISM classifications
        Assert.Equal(0, (int)DataClassification.Unofficial);
        Assert.Equal(1, (int)DataClassification.Official);
        Assert.Equal(2, (int)DataClassification.OfficialSensitive);
        Assert.Equal(3, (int)DataClassification.Protected);
    }

    [Fact]
    public void AuditEventType_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)AuditEventType.Create);
        Assert.Equal(1, (int)AuditEventType.Read);
        Assert.Equal(2, (int)AuditEventType.Update);
        Assert.Equal(3, (int)AuditEventType.Delete);
        Assert.Equal(4, (int)AuditEventType.Login);
        Assert.Equal(5, (int)AuditEventType.Logout);
        Assert.Equal(6, (int)AuditEventType.CredentialIssued);
        Assert.Equal(7, (int)AuditEventType.CredentialVerified);
        Assert.Equal(8, (int)AuditEventType.CredentialRevoked);
    }
}