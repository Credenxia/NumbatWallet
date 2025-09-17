using Xunit;
using Microsoft.EntityFrameworkCore;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.Data.Configurations;
using NumbatWallet.Domain.Aggregates;

namespace NumbatWallet.Infrastructure.Tests.Data;

public class EntityConfigurationsTests
{
    [Fact]
    public void WalletConfiguration_ShouldConfigureCorrectly()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<NumbatWalletDbContext>();
        optionsBuilder.UseInMemoryDatabase("TestDb");

        var modelBuilder = new ModelBuilder();
        var configuration = new WalletConfiguration();

        // Act
        configuration.Configure(modelBuilder.Entity<Wallet>());

        // Assert
        var entityType = modelBuilder.Model.FindEntityType(typeof(Wallet));
        Assert.NotNull(entityType);

        var idProperty = entityType.FindProperty("Id");
        Assert.NotNull(idProperty);
        Assert.True(idProperty.IsKey());

        var nameProperty = entityType.FindProperty("Name");
        Assert.NotNull(nameProperty);
        Assert.Equal(256, nameProperty.GetMaxLength());
        Assert.False(nameProperty.IsNullable);
    }

    [Fact]
    public void CredentialConfiguration_ShouldConfigureCorrectly()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var configuration = new CredentialConfiguration();

        // Act
        configuration.Configure(modelBuilder.Entity<Credential>());

        // Assert
        var entityType = modelBuilder.Model.FindEntityType(typeof(Credential));
        Assert.NotNull(entityType);

        var credentialTypeProperty = entityType.FindProperty("CredentialType");
        Assert.NotNull(credentialTypeProperty);
        Assert.Equal(128, credentialTypeProperty.GetMaxLength());

        var credentialDataProperty = entityType.FindProperty("CredentialData");
        Assert.NotNull(credentialDataProperty);
        Assert.Equal("jsonb", credentialDataProperty.GetColumnType());
    }

    [Fact]
    public void PersonConfiguration_ShouldConfigureCorrectly()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var configuration = new PersonConfiguration();

        // Act
        configuration.Configure(modelBuilder.Entity<Person>());

        // Assert
        var entityType = modelBuilder.Model.FindEntityType(typeof(Person));
        Assert.NotNull(entityType);

        var firstNameProperty = entityType.FindProperty("FirstName");
        Assert.NotNull(firstNameProperty);
        Assert.Equal(128, firstNameProperty.GetMaxLength());

        var emailProperty = entityType.FindProperty("Email");
        Assert.NotNull(emailProperty);

        var phoneNumberProperty = entityType.FindProperty("PhoneNumber");
        Assert.NotNull(phoneNumberProperty);
    }

    [Fact]
    public void IssuerConfiguration_ShouldConfigureCorrectly()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var configuration = new IssuerConfiguration();

        // Act
        configuration.Configure(modelBuilder.Entity<Issuer>());

        // Assert
        var entityType = modelBuilder.Model.FindEntityType(typeof(Issuer));
        Assert.NotNull(entityType);

        var nameProperty = entityType.FindProperty("Name");
        Assert.NotNull(nameProperty);
        Assert.Equal(256, nameProperty.GetMaxLength());

        var codeProperty = entityType.FindProperty("Code");
        Assert.NotNull(codeProperty);
        Assert.Equal(64, codeProperty.GetMaxLength());

        var trustedDomainProperty = entityType.FindProperty("TrustedDomain");
        Assert.NotNull(trustedDomainProperty);
        Assert.Equal(256, trustedDomainProperty.GetMaxLength());
    }

    [Fact]
    public void Configuration_ShouldHaveIndexes()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();
        var walletConfig = new WalletConfiguration();
        var credentialConfig = new CredentialConfiguration();

        // Act
        walletConfig.Configure(modelBuilder.Entity<Wallet>());
        credentialConfig.Configure(modelBuilder.Entity<Credential>());

        // Assert
        var walletEntity = modelBuilder.Model.FindEntityType(typeof(Wallet));
        var walletIndexes = walletEntity.GetIndexes();
        Assert.Contains(walletIndexes, idx => idx.Properties.Any(p => p.Name == "PersonId"));
        Assert.Contains(walletIndexes, idx => idx.Properties.Any(p => p.Name == "TenantId"));

        var credentialEntity = modelBuilder.Model.FindEntityType(typeof(Credential));
        var credentialIndexes = credentialEntity.GetIndexes();
        Assert.Contains(credentialIndexes, idx => idx.Properties.Any(p => p.Name == "WalletId"));
        Assert.Contains(credentialIndexes, idx => idx.Properties.Any(p => p.Name == "IssuerId"));
    }
}