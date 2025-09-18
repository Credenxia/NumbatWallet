using Xunit;
using NumbatWallet.Domain.Aggregates;

namespace NumbatWallet.Domain.Tests.Aggregates;

public class IssuerTests
{
    [Fact]
    public void Issuer_Create_ShouldInitializeCorrectly()
    {
        // Arrange
        var name = "WA Department of Transport";
        var code = "WA-DOT";
        var trustedDomain = "transport.wa.gov.au";

        // Act
        var result = Issuer.Create(
            name,
            code,
            trustedDomain
        );

        // Assert
        Assert.True(result.IsSuccess);
        var issuer = result.Value;
        Assert.NotEqual(Guid.Empty, issuer.Id);
        Assert.Equal(string.Empty, issuer.TenantId); // Will be set by DbContext
        Assert.Equal(name, issuer.Name);
        Assert.Equal(code, issuer.Code);
        Assert.Equal($"did:web:{trustedDomain}", issuer.IssuerDid);
        Assert.Equal(string.Empty, issuer.PublicKey); // To be set later
        Assert.Equal(trustedDomain, issuer.TrustedDomain);
        Assert.True(issuer.IsActive);
        Assert.Contains(trustedDomain, issuer.GetTrustedDomains());
        Assert.Empty(issuer.GetSupportedCredentialTypes());
    }

    [Fact]
    public void Issuer_AddSupportedCredentialType_ShouldAddToCollection()
    {
        // Arrange
        var issuer = CreateTestIssuer();
        var credentialType = "DriverLicence";
        var schemaUrl = "https://schema.wa.gov.au/driverlicence/v1";

        // Act
        var result = issuer.AddSupportedCredentialType(credentialType, schemaUrl);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(credentialType, issuer.GetSupportedCredentialTypes());
        Assert.True(issuer.SupportsCredentialType(credentialType));
    }

    [Fact]
    public void Issuer_AddSupportedCredentialType_Duplicate_ShouldFail()
    {
        // Arrange
        var issuer = CreateTestIssuer();
        var credentialType = "DriverLicence";
        var schemaUrl = "https://schema.wa.gov.au/driverlicence/v1";
        issuer.AddSupportedCredentialType(credentialType, schemaUrl);

        // Act
        var result = issuer.AddSupportedCredentialType(credentialType, schemaUrl);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already supported", result.Error.Message);
    }

    [Fact]
    public void Issuer_RemoveSupportedCredentialType_ShouldRemoveFromCollection()
    {
        // Arrange
        var issuer = CreateTestIssuer();
        var credentialType = "DriverLicence";
        issuer.AddSupportedCredentialType(credentialType, "https://schema.test");

        // Act
        var result = issuer.RemoveSupportedCredentialType(credentialType);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(credentialType, issuer.GetSupportedCredentialTypes());
        Assert.False(issuer.SupportsCredentialType(credentialType));
    }

    [Fact]
    public void Issuer_AddTrustedDomain_ShouldAddToCollection()
    {
        // Arrange
        var issuer = CreateTestIssuer();
        var domain = "newdomain.wa.gov.au";

        // Act
        var result = issuer.AddTrustedDomain(domain);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(domain, issuer.GetTrustedDomains());
        Assert.True(issuer.IsDomainTrusted(domain));
    }

    [Fact]
    public void Issuer_RemoveTrustedDomain_ShouldRemoveFromCollection()
    {
        // Arrange
        var issuer = CreateTestIssuer();
        var domain = "test.wa.gov.au";
        issuer.AddTrustedDomain(domain);

        // Act
        var result = issuer.RemoveTrustedDomain(domain);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(domain, issuer.GetTrustedDomains());
        Assert.False(issuer.IsDomainTrusted(domain));
    }

    [Fact]
    public void Issuer_IsDomainTrusted_WithWildcard_ShouldMatchPattern()
    {
        // Arrange
        var issuer = CreateTestIssuer();
        issuer.AddTrustedDomain("*.wa.gov.au");

        // Act & Assert
        Assert.True(issuer.IsDomainTrusted("transport.wa.gov.au"));
        Assert.True(issuer.IsDomainTrusted("health.wa.gov.au"));
        Assert.False(issuer.IsDomainTrusted("example.com"));
    }

    [Fact]
    public void Issuer_UpdatePublicKey_ShouldUpdateKey()
    {
        // Arrange
        var issuer = CreateTestIssuer();
        var newPublicKey = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQNEW...";

        // Act
        var result = issuer.UpdatePublicKey(newPublicKey);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newPublicKey, issuer.PublicKey);
    }

    [Fact]
    public void Issuer_Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var issuer = CreateTestIssuer();
        var reason = "License expired";

        // Act
        var result = issuer.Deactivate(reason);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(issuer.IsActive);
    }

    [Fact]
    public void Issuer_Reactivate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var issuer = CreateTestIssuer();
        issuer.Deactivate("Test reason");

        // Act
        var result = issuer.Reactivate();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(issuer.IsActive);
    }

    [Fact]
    public void Issuer_UpdateDetails_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var issuer = CreateTestIssuer();
        var newName = "Updated Department Name";
        var newDescription = "Updated description";

        // Act
        var result = issuer.UpdateDetails(newName, newDescription);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newName, issuer.Name);
        Assert.Equal(newDescription, issuer.Description);
    }

    [Fact]
    public void Issuer_GetSchemaForCredentialType_ShouldReturnCorrectSchema()
    {
        // Arrange
        var issuer = CreateTestIssuer();
        var credentialType = "DriverLicence";
        var schemaUrl = "https://schema.wa.gov.au/driverlicence/v1";
        issuer.AddSupportedCredentialType(credentialType, schemaUrl);

        // Act
        var schema = issuer.GetSchemaForCredentialType(credentialType);

        // Assert
        Assert.Equal(schemaUrl, schema);
    }

    [Fact]
    public void Issuer_GetSchemaForCredentialType_NotSupported_ShouldReturnNull()
    {
        // Arrange
        var issuer = CreateTestIssuer();

        // Act
        var schema = issuer.GetSchemaForCredentialType("UnsupportedType");

        // Assert
        Assert.Null(schema);
    }

    private static Issuer CreateTestIssuer()
    {
        var issuer = Issuer.Create(
            "Test Issuer",
            "TEST-CODE",
            "test.wa.gov.au"
        ).Value;

        // Set additional properties using methods
        issuer.UpdateDetails("Test Issuer", "Test Description");
        issuer.UpdatePublicKey("TestPublicKey");

        return issuer;
    }
}