using System.Text.Json;
using FluentAssertions;
using NumbatWallet.Domain.Credentials;
using NumbatWallet.SharedKernel.Enums;
using Xunit;

namespace NumbatWallet.Domain.Tests.Credentials;

public class CredentialFormatTests
{
    [Fact]
    public void JwtVcFormat_Should_SerializeCredential_ToJwtFormat()
    {
        // Arrange
        var credentialData = new Dictionary<string, object>
        {
            ["@context"] = new[] { "https://www.w3.org/2018/credentials/v1" },
            ["type"] = new[] { "VerifiableCredential", "DriverLicence" },
            ["credentialSubject"] = new Dictionary<string, object>
            {
                ["id"] = "did:example:123",
                ["name"] = "John Doe",
                ["licenceNumber"] = "DL123456"
            }
        };

        var format = new JwtVcFormat();

        // Act
        var result = format.SerializeCredential(credentialData, "issuer-key");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Match("*.*.*"); // JWT format: header.payload.signature
    }

    [Fact]
    public void JwtVcFormat_Should_DeserializeCredential_FromJwtFormat()
    {
        // Arrange
        var format = new JwtVcFormat();
        var credentialData = new Dictionary<string, object>
        {
            ["@context"] = new[] { "https://www.w3.org/2018/credentials/v1" },
            ["type"] = new[] { "VerifiableCredential", "DriverLicence" },
            ["credentialSubject"] = new Dictionary<string, object>
            {
                ["id"] = "did:example:123",
                ["name"] = "John Doe"
            }
        };
        var jwt = format.SerializeCredential(credentialData, "issuer-key");

        // Act
        var result = format.DeserializeCredential(jwt);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("credentialSubject");
    }

    [Fact]
    public void JsonLdFormat_Should_SerializeCredential_ToJsonLdFormat()
    {
        // Arrange
        var credentialData = new Dictionary<string, object>
        {
            ["@context"] = new[] {
                "https://www.w3.org/2018/credentials/v1",
                "https://w3id.org/citizenship/v1"
            },
            ["type"] = new[] { "VerifiableCredential", "PermanentResidentCard" },
            ["credentialSubject"] = new Dictionary<string, object>
            {
                ["id"] = "did:example:456",
                ["givenName"] = "Jane",
                ["familyName"] = "Doe"
            },
            ["proof"] = new Dictionary<string, object>
            {
                ["type"] = "RsaSignature2018",
                ["created"] = "2024-01-01T00:00:00Z",
                ["proofPurpose"] = "assertionMethod"
            }
        };

        var format = new JsonLdFormat();

        // Act
        var result = format.SerializeCredential(credentialData);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
        parsed.Should().ContainKey("@context");
        parsed.Should().ContainKey("proof");
    }

    [Fact]
    public void JsonLdFormat_Should_ValidateContext()
    {
        // Arrange
        var format = new JsonLdFormat();
        var validContext = new[] { "https://www.w3.org/2018/credentials/v1" };
        var invalidContext = new[] { "https://invalid.example.com/v1" };

        // Act & Assert
        format.IsValidContext(validContext).Should().BeTrue();
        format.IsValidContext(invalidContext).Should().BeFalse();
    }

    [Fact]
    public void CredentialFormatFactory_Should_CreateCorrectFormat()
    {
        // Arrange
        var factory = new CredentialFormatFactory();

        // Act
        var jwtFormat = factory.CreateFormat(CredentialFormat.JwtVc);
        var jsonLdFormat = factory.CreateFormat(CredentialFormat.JsonLd);

        // Assert
        jwtFormat.Should().BeOfType<JwtVcFormat>();
        jsonLdFormat.Should().BeOfType<JsonLdFormat>();
    }

    [Fact]
    public void CredentialFormatValidator_Should_ValidateJwtFormat()
    {
        // Arrange
        var validator = new CredentialFormatValidator();
        var validJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var invalidJwt = "not-a-jwt";

        // Act & Assert
        validator.IsValidJwtVc(validJwt).Should().BeTrue();
        validator.IsValidJwtVc(invalidJwt).Should().BeFalse();
    }

    [Fact]
    public void CredentialFormatValidator_Should_ValidateJsonLdFormat()
    {
        // Arrange
        var validator = new CredentialFormatValidator();
        var validJsonLd = @"{
            ""@context"": [""https://www.w3.org/2018/credentials/v1""],
            ""type"": [""VerifiableCredential""],
            ""credentialSubject"": {
                ""id"": ""did:example:123""
            }
        }";
        var invalidJsonLd = @"{ ""invalid"": ""structure"" }";

        // Act & Assert
        validator.IsValidJsonLd(validJsonLd).Should().BeTrue();
        validator.IsValidJsonLd(invalidJsonLd).Should().BeFalse();
    }
}