using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Domain.Tests.Aggregates;

public class WalletTests
{
    [Fact]
    public void Wallet_Create_ShouldInitializeCorrectly()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var walletName = "My Digital Wallet";

        // Act
        var result = Wallet.Create(personId, walletName);

        // Assert
        Assert.True(result.IsSuccess);
        var wallet = result.Value;
        Assert.NotEqual(Guid.Empty, wallet.Id);
        Assert.Equal(personId, wallet.PersonId);
        Assert.Equal(walletName, wallet.WalletName);
        Assert.Equal(WalletStatus.Active, wallet.Status);
        Assert.NotNull(wallet.WalletDid);
        Assert.Empty(wallet.GetCredentials());
    }

    [Fact]
    public void Wallet_AddCredential_ShouldAddToCollection()
    {
        // Arrange
        var wallet = CreateTestWallet();
        var credentialId = Guid.NewGuid();

        // Act
        var result = wallet.AddCredential(credentialId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(credentialId, wallet.GetCredentials());
        Assert.Single(wallet.GetCredentials());
    }

    [Fact]
    public void Wallet_AddCredential_Duplicate_ShouldFail()
    {
        // Arrange
        var wallet = CreateTestWallet();
        var credentialId = Guid.NewGuid();
        wallet.AddCredential(credentialId);

        // Act
        var result = wallet.AddCredential(credentialId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already exists", result.Error.Message);
        Assert.Single(wallet.GetCredentials());
    }

    [Fact]
    public void Wallet_RemoveCredential_ShouldRemoveFromCollection()
    {
        // Arrange
        var wallet = CreateTestWallet();
        var credentialId = Guid.NewGuid();
        wallet.AddCredential(credentialId);

        // Act
        var result = wallet.RemoveCredential(credentialId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(credentialId, wallet.GetCredentials());
        Assert.Empty(wallet.GetCredentials());
    }

    [Fact]
    public void Wallet_RemoveCredential_NotExists_ShouldFail()
    {
        // Arrange
        var wallet = CreateTestWallet();
        var credentialId = Guid.NewGuid();

        // Act
        var result = wallet.RemoveCredential(credentialId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error.Message);
    }

    [Fact]
    public void Wallet_Suspend_ShouldChangeStatusToSuspended()
    {
        // Arrange
        var wallet = CreateTestWallet();
        var reason = "Security review";

        // Act
        var result = wallet.Suspend(reason);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(WalletStatus.Suspended, wallet.Status);
    }

    [Fact]
    public void Wallet_Suspend_AlreadySuspended_ShouldFail()
    {
        // Arrange
        var wallet = CreateTestWallet();
        wallet.Suspend("Initial reason");

        // Act
        var result = wallet.Suspend("Another reason");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already suspended", result.Error.Message);
    }

    [Fact]
    public void Wallet_Reactivate_FromSuspended_ShouldSucceed()
    {
        // Arrange
        var wallet = CreateTestWallet();
        wallet.Suspend("Test reason");

        // Act
        var result = wallet.Reactivate();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(WalletStatus.Active, wallet.Status);
    }

    [Fact]
    public void Wallet_Reactivate_AlreadyActive_ShouldFail()
    {
        // Arrange
        var wallet = CreateTestWallet();

        // Act
        var result = wallet.Reactivate();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already active", result.Error.Message);
    }

    [Fact]
    public void Wallet_Lock_ShouldChangeStatusToLocked()
    {
        // Arrange
        var wallet = CreateTestWallet();
        var reason = "Multiple failed login attempts";

        // Act
        var result = wallet.Lock(reason);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(WalletStatus.Locked, wallet.Status);
    }

    [Fact]
    public void Wallet_Unlock_FromLocked_ShouldSucceed()
    {
        // Arrange
        var wallet = CreateTestWallet();
        wallet.Lock("Test reason");

        // Act
        var result = wallet.Unlock();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(WalletStatus.Active, wallet.Status);
    }

    [Fact]
    public void Wallet_UpdateName_ShouldUpdateWalletName()
    {
        // Arrange
        var wallet = CreateTestWallet();
        var newName = "Updated Wallet Name";

        // Act
        var result = wallet.UpdateName(newName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newName, wallet.WalletName);
    }

    [Fact]
    public void Wallet_GetCredentialCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var wallet = CreateTestWallet();
        wallet.AddCredential(Guid.NewGuid());
        wallet.AddCredential(Guid.NewGuid());
        wallet.AddCredential(Guid.NewGuid());

        // Act
        var count = wallet.GetCredentialCount();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void Wallet_HasCredential_ShouldReturnCorrectValue()
    {
        // Arrange
        var wallet = CreateTestWallet();
        var credentialId = Guid.NewGuid();
        var otherCredentialId = Guid.NewGuid();
        wallet.AddCredential(credentialId);

        // Act & Assert
        Assert.True(wallet.HasCredential(credentialId));
        Assert.False(wallet.HasCredential(otherCredentialId));
    }

    private static Wallet CreateTestWallet()
    {
        return Wallet.Create(
            Guid.NewGuid(),
            "Test Wallet"
        ).Value;
    }
}