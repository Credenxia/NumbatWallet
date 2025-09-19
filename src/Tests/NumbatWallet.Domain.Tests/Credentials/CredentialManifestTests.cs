using FluentAssertions;
using NumbatWallet.Domain.Credentials;
using Xunit;

namespace NumbatWallet.Domain.Tests.Credentials;

public class CredentialManifestTests
{
    [Fact]
    public void CredentialManifest_Should_ParseValidManifest()
    {
        // Arrange
        var manifestJson = @"{
            ""id"": ""https://example.com/manifests/driver-license"",
            ""version"": ""1.0.0"",
            ""issuer"": {
                ""id"": ""did:example:issuer"",
                ""name"": ""DMV Office""
            },
            ""output_descriptors"": [
                {
                    ""id"": ""driver_license_output"",
                    ""schema"": ""https://schema.org/DriverLicense"",
                    ""display"": {
                        ""title"": {
                            ""text"": ""Driver License""
                        },
                        ""description"": {
                            ""text"": ""Official driver license credential""
                        }
                    }
                }
            ],
            ""presentation_definition"": {
                ""id"": ""driver_license_requirements"",
                ""input_descriptors"": [
                    {
                        ""id"": ""identity_verification"",
                        ""schema"": [
                            {
                                ""uri"": ""https://schema.org/Person""
                            }
                        ],
                        ""constraints"": {
                            ""fields"": [
                                {
                                    ""path"": [""$.credentialSubject.birthDate""],
                                    ""filter"": {
                                        ""type"": ""string"",
                                        ""format"": ""date""
                                    }
                                }
                            ]
                        }
                    }
                ]
            }
        }";

        var parser = new CredentialManifestParser();

        // Act
        var manifest = parser.Parse(manifestJson);

        // Assert
        manifest.Should().NotBeNull();
        manifest.Id.Should().Be("https://example.com/manifests/driver-license");
        manifest.Version.Should().Be("1.0.0");
        manifest.Issuer.Should().NotBeNull();
        manifest.Issuer.Id.Should().Be("did:example:issuer");
        manifest.OutputDescriptors.Should().HaveCount(1);
        manifest.PresentationDefinition.Should().NotBeNull();
    }

    [Fact]
    public void CredentialManifest_Should_ValidateRequiredFields()
    {
        // Arrange
        var validator = new CredentialManifestValidator();
        var validManifest = new CredentialManifest
        {
            Id = "https://example.com/manifest/1",
            Version = "1.0.0",
            Issuer = new ManifestIssuer
            {
                Id = "did:example:issuer",
                Name = "Test Issuer"
            },
            OutputDescriptors = new List<OutputDescriptor>
            {
                new OutputDescriptor
                {
                    Id = "output_1",
                    Schema = "https://schema.org/Credential"
                }
            }
        };

        var invalidManifest = new CredentialManifest
        {
            Id = null, // Missing required field
            Version = "1.0.0"
        };

        // Act & Assert
        validator.IsValid(validManifest).Should().BeTrue();
        validator.IsValid(invalidManifest).Should().BeFalse();
    }

    [Fact]
    public void CredentialManifest_Should_GenerateCredentialFromManifest()
    {
        // Arrange
        var manifest = new CredentialManifest
        {
            Id = "https://example.com/manifest/1",
            Version = "1.0.0",
            Issuer = new ManifestIssuer
            {
                Id = "did:example:issuer",
                Name = "Test Issuer"
            },
            OutputDescriptors = new List<OutputDescriptor>
            {
                new OutputDescriptor
                {
                    Id = "license_output",
                    Schema = "https://schema.org/DriverLicense",
                    Display = new DisplayMetadata
                    {
                        Title = new LocalizedString { Text = "Driver License" },
                        Description = new LocalizedString { Text = "Official license" }
                    }
                }
            }
        };

        var claimData = new Dictionary<string, object>
        {
            ["licenseNumber"] = "DL123456",
            ["fullName"] = "John Doe",
            ["birthDate"] = "1990-01-01"
        };

        var generator = new CredentialGenerator();

        // Act
        var credential = generator.GenerateFromManifest(manifest, claimData);

        // Assert
        credential.Should().NotBeNull();
        credential.Should().ContainKey("@context");
        credential.Should().ContainKey("type");
        credential.Should().ContainKey("credentialSubject");
        credential["issuer"].Should().Be("did:example:issuer");
        var subject = credential["credentialSubject"] as Dictionary<string, object>;
        subject.Should().ContainKey("licenseNumber");
        subject["licenseNumber"].Should().Be("DL123456");
    }

    [Fact]
    public void CredentialManifest_Should_ValidatePresentationRequirements()
    {
        // Arrange
        var manifest = new CredentialManifest
        {
            Id = "https://example.com/manifest/1",
            Version = "1.0.0",
            PresentationDefinition = new PresentationDefinition
            {
                Id = "requirements_1",
                InputDescriptors = new List<InputDescriptor>
                {
                    new InputDescriptor
                    {
                        Id = "age_verification",
                        Schema = new List<SchemaReference>
                        {
                            new SchemaReference { Uri = "https://schema.org/Person" }
                        },
                        Constraints = new Constraints
                        {
                            Fields = new List<FieldConstraint>
                            {
                                new FieldConstraint
                                {
                                    Path = new[] { "$.credentialSubject.age" },
                                    Filter = new Dictionary<string, object>
                                    {
                                        ["type"] = "number",
                                        ["minimum"] = 18
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var validPresentation = new Dictionary<string, object>
        {
            ["credentialSubject"] = new Dictionary<string, object>
            {
                ["age"] = 21
            }
        };

        var invalidPresentation = new Dictionary<string, object>
        {
            ["credentialSubject"] = new Dictionary<string, object>
            {
                ["age"] = 16
            }
        };

        var validator = new PresentationValidator();

        // Act & Assert
        validator.ValidateAgainstManifest(validPresentation, manifest).Should().BeTrue();
        validator.ValidateAgainstManifest(invalidPresentation, manifest).Should().BeFalse();
    }

    [Fact]
    public void CredentialManifest_Should_SupportMultipleOutputFormats()
    {
        // Arrange
        var manifest = new CredentialManifest
        {
            Id = "https://example.com/manifest/1",
            Version = "1.0.0",
            OutputDescriptors = new List<OutputDescriptor>
            {
                new OutputDescriptor
                {
                    Id = "output_jwt",
                    Schema = "https://schema.org/Credential",
                    Format = "jwt_vc"
                },
                new OutputDescriptor
                {
                    Id = "output_json_ld",
                    Schema = "https://schema.org/Credential",
                    Format = "ldp_vc"
                }
            }
        };

        // Act
        var formats = manifest.GetSupportedFormats();

        // Assert
        formats.Should().Contain("jwt_vc");
        formats.Should().Contain("ldp_vc");
        formats.Should().HaveCount(2);
    }
}