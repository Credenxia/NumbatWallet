using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Infrastructure.Data.Configurations;

namespace NumbatWallet.Infrastructure.Tests.Data;

/// <summary>
/// Advanced tests for entity configurations
/// Tests relationships, constraints, indexes, and data integrity rules
/// </summary>
public class EntityConfigurationsAdvancedTests
{
    #region Relationship Tests

    [Fact]
    public void WalletConfiguration_ShouldDefine_PersonRelationship()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();

        // Act
        new WalletConfiguration().Configure(modelBuilder.Entity<Wallet>());
        new PersonConfiguration().Configure(modelBuilder.Entity<Person>());
        modelBuilder.FinalizeModel();

        // Assert
        var walletEntity = modelBuilder.Model.FindEntityType(typeof(Wallet));
        var personForeignKey = walletEntity!.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Person));

        personForeignKey.Should().NotBeNull();
        personForeignKey!.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    [Fact]
    public void CredentialConfiguration_ShouldDefine_WalletRelationship()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();

        // Act
        new CredentialConfiguration().Configure(modelBuilder.Entity<Credential>());
        new WalletConfiguration().Configure(modelBuilder.Entity<Wallet>());
        modelBuilder.FinalizeModel();

        // Assert
        var credentialEntity = modelBuilder.Model.FindEntityType(typeof(Credential));
        var walletForeignKey = credentialEntity!.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Wallet));

        walletForeignKey.Should().NotBeNull();
        walletForeignKey!.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void CredentialConfiguration_ShouldDefine_IssuerRelationship()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();

        // Act
        new CredentialConfiguration().Configure(modelBuilder.Entity<Credential>());
        new IssuerConfiguration().Configure(modelBuilder.Entity<Issuer>());
        modelBuilder.FinalizeModel();

        // Assert
        var credentialEntity = modelBuilder.Model.FindEntityType(typeof(Credential));
        var issuerForeignKey = credentialEntity!.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Issuer));

        issuerForeignKey.Should().NotBeNull();
        issuerForeignKey!.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    #endregion

    #region Index and Constraint Tests

    [Fact]
    public void WalletConfiguration_ShouldHave_UniqueIndex_OnWalletDid()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new WalletConfiguration().Configure(modelBuilder.Entity<Wallet>());

        // Act
        var walletEntity = modelBuilder.Model.FindEntityType(typeof(Wallet));

        // Assert
        var didIndex = walletEntity!.GetIndexes()
            .FirstOrDefault(idx => idx.Properties.Any(p => p.Name == "WalletDid"));

        didIndex.Should().NotBeNull();
        didIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void PersonConfiguration_ShouldHave_Index_OnTenantId()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new PersonConfiguration().Configure(modelBuilder.Entity<Person>());

        // Act
        var personEntity = modelBuilder.Model.FindEntityType(typeof(Person));

        // Assert
        var tenantIndex = personEntity!.GetIndexes()
            .FirstOrDefault(idx => idx.Properties.Any(p => p.Name == "TenantId"));

        tenantIndex.Should().NotBeNull();
    }

    [Fact]
    public void IssuerConfiguration_ShouldHave_UniqueIndex_OnCode()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new IssuerConfiguration().Configure(modelBuilder.Entity<Issuer>());

        // Act
        var issuerEntity = modelBuilder.Model.FindEntityType(typeof(Issuer));

        // Assert
        var codeIndex = issuerEntity!.GetIndexes()
            .FirstOrDefault(idx => idx.Properties.Any(p => p.Name == "Code"));

        codeIndex.Should().NotBeNull();
        codeIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void CredentialConfiguration_ShouldHave_CompositeIndex_OnWalletAndStatus()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new CredentialConfiguration().Configure(modelBuilder.Entity<Credential>());

        // Act
        var credentialEntity = modelBuilder.Model.FindEntityType(typeof(Credential));

        // Assert
        var compositeIndex = credentialEntity!.GetIndexes()
            .FirstOrDefault(idx =>
                idx.Properties.Any(p => p.Name == "WalletId") &&
                idx.Properties.Any(p => p.Name == "Status"));

        compositeIndex.Should().NotBeNull();
    }

    #endregion

    #region Column Type Tests

    [Fact]
    public void PersonConfiguration_ShouldUse_JsonbColumns_ForEncryptedFields()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new PersonConfiguration().Configure(modelBuilder.Entity<Person>());

        // Act
        var personEntity = modelBuilder.Model.FindEntityType(typeof(Person));

        // Assert
        var firstNameProperty = personEntity!.FindProperty("FirstName");
        firstNameProperty!.GetColumnType().Should().Be("jsonb");

        var lastNameProperty = personEntity.FindProperty("LastName");
        lastNameProperty!.GetColumnType().Should().Be("jsonb");

        var dobProperty = personEntity.FindProperty("DateOfBirth");
        dobProperty!.GetColumnType().Should().Be("jsonb");
    }

    [Fact]
    public void CredentialConfiguration_ShouldUse_JsonbColumn_ForCredentialData()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new CredentialConfiguration().Configure(modelBuilder.Entity<Credential>());

        // Act
        var credentialEntity = modelBuilder.Model.FindEntityType(typeof(Credential));
        var dataProperty = credentialEntity!.FindProperty("CredentialData");

        // Assert
        dataProperty.Should().NotBeNull();
        dataProperty!.GetColumnType().Should().Be("jsonb");
    }

    [Fact]
    public void WalletConfiguration_ShouldHave_CorrectColumnTypes()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new WalletConfiguration().Configure(modelBuilder.Entity<Wallet>());

        // Act
        var walletEntity = modelBuilder.Model.FindEntityType(typeof(Wallet));

        // Assert
        var walletDidProperty = walletEntity!.FindProperty("WalletDid");
        walletDidProperty!.GetMaxLength().Should().Be(512);

        var statusProperty = walletEntity.FindProperty("Status");
        statusProperty.Should().NotBeNull();
    }

    #endregion

    #region Required Field Tests

    [Fact]
    public void WalletConfiguration_RequiredFields_ShouldNotBeNullable()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new WalletConfiguration().Configure(modelBuilder.Entity<Wallet>());

        // Act
        var walletEntity = modelBuilder.Model.FindEntityType(typeof(Wallet));

        // Assert
        walletEntity!.FindProperty("WalletName")!.IsNullable.Should().BeFalse();
        walletEntity.FindProperty("WalletDid")!.IsNullable.Should().BeFalse();
        walletEntity.FindProperty("PersonId")!.IsNullable.Should().BeFalse();
        walletEntity.FindProperty("TenantId")!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void WalletConfiguration_OptionalFields_ShouldBeNullable()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new WalletConfiguration().Configure(modelBuilder.Entity<Wallet>());

        // Act
        var walletEntity = modelBuilder.Model.FindEntityType(typeof(Wallet));

        // Assert
        walletEntity!.FindProperty("SuspensionReason")!.IsNullable.Should().BeTrue();
        walletEntity.FindProperty("LockReason")!.IsNullable.Should().BeTrue();
        walletEntity.FindProperty("ExternalId")!.IsNullable.Should().BeTrue();
        walletEntity.FindProperty("ExpiresAt")!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void CredentialConfiguration_RequiredFields_ShouldNotBeNullable()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new CredentialConfiguration().Configure(modelBuilder.Entity<Credential>());

        // Act
        var credentialEntity = modelBuilder.Model.FindEntityType(typeof(Credential));

        // Assert
        credentialEntity!.FindProperty("CredentialType")!.IsNullable.Should().BeFalse();
        credentialEntity.FindProperty("CredentialData")!.IsNullable.Should().BeFalse();
        credentialEntity.FindProperty("SchemaId")!.IsNullable.Should().BeFalse();
        credentialEntity.FindProperty("WalletId")!.IsNullable.Should().BeFalse();
        credentialEntity.FindProperty("IssuerId")!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void PersonConfiguration_RequiredFields_ShouldNotBeNullable()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new PersonConfiguration().Configure(modelBuilder.Entity<Person>());

        // Act
        var personEntity = modelBuilder.Model.FindEntityType(typeof(Person));

        // Assert
        personEntity!.FindProperty("FirstName")!.IsNullable.Should().BeFalse();
        personEntity.FindProperty("LastName")!.IsNullable.Should().BeFalse();
        personEntity.FindProperty("DateOfBirth")!.IsNullable.Should().BeFalse();
        personEntity.FindProperty("TenantId")!.IsNullable.Should().BeFalse();
    }

    #endregion

    #region Audit Field Tests

    [Fact]
    public void WalletConfiguration_ShouldHave_AuditFields()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new WalletConfiguration().Configure(modelBuilder.Entity<Wallet>());

        // Act
        var walletEntity = modelBuilder.Model.FindEntityType(typeof(Wallet));

        // Assert
        walletEntity!.FindProperty("CreatedAt").Should().NotBeNull();
        walletEntity.FindProperty("CreatedBy").Should().NotBeNull();
        walletEntity.FindProperty("ModifiedAt").Should().NotBeNull();
        walletEntity.FindProperty("ModifiedBy").Should().NotBeNull();
    }

    [Fact]
    public void CredentialConfiguration_ShouldHave_AuditFields()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new CredentialConfiguration().Configure(modelBuilder.Entity<Credential>());

        // Act
        var credentialEntity = modelBuilder.Model.FindEntityType(typeof(Credential));

        // Assert
        credentialEntity!.FindProperty("CreatedAt").Should().NotBeNull();
        credentialEntity.FindProperty("CreatedBy").Should().NotBeNull();
        credentialEntity.FindProperty("ModifiedAt").Should().NotBeNull();
        credentialEntity.FindProperty("ModifiedBy").Should().NotBeNull();
    }

    #endregion

    #region Table Name Tests

    [Fact]
    public void EntityConfigurations_ShouldMap_ToCorrectTableNames()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new WalletConfiguration().Configure(modelBuilder.Entity<Wallet>());
        new CredentialConfiguration().Configure(modelBuilder.Entity<Credential>());
        new PersonConfiguration().Configure(modelBuilder.Entity<Person>());
        new IssuerConfiguration().Configure(modelBuilder.Entity<Issuer>());

        // Act & Assert
        modelBuilder.Model.FindEntityType(typeof(Wallet))!.GetTableName().Should().Be("Wallets");
        modelBuilder.Model.FindEntityType(typeof(Credential))!.GetTableName().Should().Be("Credentials");
        modelBuilder.Model.FindEntityType(typeof(Person))!.GetTableName().Should().Be("Persons");
        modelBuilder.Model.FindEntityType(typeof(Issuer))!.GetTableName().Should().Be("Issuers");
    }

    #endregion

    #region Max Length Tests

    [Fact]
    public void WalletConfiguration_ShouldEnforce_MaxLengthConstraints()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new WalletConfiguration().Configure(modelBuilder.Entity<Wallet>());

        // Act
        var walletEntity = modelBuilder.Model.FindEntityType(typeof(Wallet));

        // Assert
        walletEntity!.FindProperty("WalletName")!.GetMaxLength().Should().Be(256);
        walletEntity.FindProperty("WalletDid")!.GetMaxLength().Should().Be(512);
        walletEntity.FindProperty("SuspensionReason")!.GetMaxLength().Should().Be(1000);
        walletEntity.FindProperty("LockReason")!.GetMaxLength().Should().Be(1000);
    }

    [Fact]
    public void CredentialConfiguration_ShouldEnforce_MaxLengthConstraints()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new CredentialConfiguration().Configure(modelBuilder.Entity<Credential>());

        // Act
        var credentialEntity = modelBuilder.Model.FindEntityType(typeof(Credential));

        // Assert
        credentialEntity!.FindProperty("CredentialType")!.GetMaxLength().Should().Be(128);
        credentialEntity.FindProperty("SchemaId")!.GetMaxLength().Should().Be(512);
    }

    [Fact]
    public void IssuerConfiguration_ShouldEnforce_MaxLengthConstraints()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new IssuerConfiguration().Configure(modelBuilder.Entity<Issuer>());

        // Act
        var issuerEntity = modelBuilder.Model.FindEntityType(typeof(Issuer));

        // Assert
        issuerEntity!.FindProperty("Name")!.GetMaxLength().Should().Be(256);
        issuerEntity.FindProperty("Code")!.GetMaxLength().Should().Be(64);
        issuerEntity.FindProperty("TrustedDomain")!.GetMaxLength().Should().Be(256);
    }

    #endregion

    #region Tenant Isolation Tests

    [Fact]
    public void AllMultiTenantEntities_ShouldHave_TenantIdField()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new WalletConfiguration().Configure(modelBuilder.Entity<Wallet>());
        new CredentialConfiguration().Configure(modelBuilder.Entity<Credential>());
        new PersonConfiguration().Configure(modelBuilder.Entity<Person>());
        new IssuerConfiguration().Configure(modelBuilder.Entity<Issuer>());

        // Act & Assert - Multi-tenant entities should have TenantId
        modelBuilder.Model.FindEntityType(typeof(Wallet))!.FindProperty("TenantId").Should().NotBeNull();
        modelBuilder.Model.FindEntityType(typeof(Person))!.FindProperty("TenantId").Should().NotBeNull();
        modelBuilder.Model.FindEntityType(typeof(Issuer))!.FindProperty("TenantId").Should().NotBeNull();
    }

    [Fact]
    public void TenantId_ShouldBe_IndexedForPerformance()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new WalletConfiguration().Configure(modelBuilder.Entity<Wallet>());
        new PersonConfiguration().Configure(modelBuilder.Entity<Person>());

        // Act
        var walletEntity = modelBuilder.Model.FindEntityType(typeof(Wallet));
        var personEntity = modelBuilder.Model.FindEntityType(typeof(Person));

        // Assert
        walletEntity!.GetIndexes().Should().Contain(idx =>
            idx.Properties.Any(p => p.Name == "TenantId"));
        personEntity!.GetIndexes().Should().Contain(idx =>
            idx.Properties.Any(p => p.Name == "TenantId"));
    }

    #endregion

    #region Navigation Property Tests

    [Fact]
    public void WalletConfiguration_ShouldDefine_CredentialsCollectionNavigation()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new WalletConfiguration().Configure(modelBuilder.Entity<Wallet>());
        new CredentialConfiguration().Configure(modelBuilder.Entity<Credential>());
        modelBuilder.FinalizeModel();

        // Act
        var walletEntity = modelBuilder.Model.FindEntityType(typeof(Wallet));
        var credentialsNavigation = walletEntity!.FindNavigation("Credentials");

        // Assert
        credentialsNavigation.Should().NotBeNull();
        credentialsNavigation!.IsCollection.Should().BeTrue();
    }

    [Fact]
    public void PersonConfiguration_ShouldDefine_OwnedEntityNavigations()
    {
        // Arrange
        var modelBuilder = CreateModelBuilder();
        new PersonConfiguration().Configure(modelBuilder.Entity<Person>());

        // Act
        var personEntity = modelBuilder.Model.FindEntityType(typeof(Person));

        // Assert
        var emailNavigation = personEntity!.FindNavigation("Email");
        emailNavigation.Should().NotBeNull();

        var phoneNumberNavigation = personEntity.FindNavigation("PhoneNumber");
        phoneNumberNavigation.Should().NotBeNull();
    }

    #endregion

    private static ModelBuilder CreateModelBuilder()
    {
        return new ModelBuilder();
    }
}
