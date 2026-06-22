using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Application.Services;

/// <summary>
/// Service for handling Credential Manifests according to DIF specification
/// </summary>
public class CredentialManifestService : ICredentialManifestService
{
    private readonly ILogger<CredentialManifestService> _logger;

    public CredentialManifestService(ILogger<CredentialManifestService> logger)
    {
        _logger = logger;
    }

    public Task<CredentialManifest> CreateManifestAsync(
        string credentialType,
        string issuerId,
        Dictionary<string, object>? outputDescriptors = null)
    {
        var manifest = new CredentialManifest
        {
            Id = $"manifest_{Guid.NewGuid()}",
            Version = "0.1.0",
            SpecVersion = "https://identity.foundation/credential-manifest/spec/v1.0.0/",
            Issuer = new ManifestIssuer
            {
                Id = issuerId,
                Name = "NumbatWallet Issuer",
                Styles = new IssuerStyles
                {
                    Thumbnail = new StyleDescriptor
                    {
                        Uri = "https://numbatwallet.wa.gov.au/logo.png",
                        Alt = "NumbatWallet Logo"
                    }
                }
            },
            OutputDescriptors = outputDescriptors ?? GetDefaultOutputDescriptors(credentialType),
            PresentationDefinition = CreatePresentationDefinition(credentialType)
        };

        _logger.LogInformation("Created credential manifest {ManifestId} for type {CredentialType}",
            manifest.Id, credentialType);

        return Task.FromResult(manifest);
    }

    public Task<bool> ValidateManifestAsync(string manifestJson)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
        {
            return Task.FromResult(false);
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<CredentialManifest>(manifestJson);

            // Validate required fields
            if (manifest == null ||
                string.IsNullOrWhiteSpace(manifest.Id) ||
                string.IsNullOrWhiteSpace(manifest.SpecVersion) ||
                !manifest.OutputDescriptors.Any())
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate credential manifest");
            return Task.FromResult(false);
        }
    }

    public Task<CredentialApplication> CreateApplicationAsync(
        string manifestId,
        string applicantId,
        Dictionary<string, object> claims)
    {
        var application = new CredentialApplication
        {
            Id = $"app_{Guid.NewGuid()}",
            SpecVersion = "https://identity.foundation/credential-manifest/spec/v1.0.0/",
            ManifestId = manifestId,
            ApplicantId = applicantId,
            SubmissionData = claims,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Created credential application {ApplicationId} for manifest {ManifestId}",
            application.Id, manifestId);

        return Task.FromResult(application);
    }

    public Task<CredentialResponse> CreateResponseAsync(
        string applicationId,
        bool approved,
        string? credentialId = null,
        string? rejectionReason = null)
    {
        var response = new CredentialResponse
        {
            Id = $"resp_{Guid.NewGuid()}",
            SpecVersion = "https://identity.foundation/credential-manifest/spec/v1.0.0/",
            ApplicationId = applicationId,
            Approved = approved,
            CredentialId = credentialId,
            RejectionReason = rejectionReason,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Created credential response {ResponseId} for application {ApplicationId} - Approved: {Approved}",
            response.Id, applicationId, approved);

        return Task.FromResult(response);
    }

    private Dictionary<string, object> GetDefaultOutputDescriptors(string credentialType)
    {
        return credentialType.ToLowerInvariant() switch
        {
            "driverlicense" => new Dictionary<string, object>
            {
                ["id"] = "driver_license_output",
                ["schema"] = "https://schemas.wa.gov.au/credentials/driver-license/v1",
                ["name"] = "Western Australia Driver License",
                ["description"] = "Official driver license issued by the Department of Transport"
            },
            "proofofage" => new Dictionary<string, object>
            {
                ["id"] = "proof_of_age_output",
                ["schema"] = "https://schemas.wa.gov.au/credentials/proof-of-age/v1",
                ["name"] = "Proof of Age Card",
                ["description"] = "Official proof of age credential"
            },
            _ => new Dictionary<string, object>
            {
                ["id"] = "generic_credential_output",
                ["schema"] = "https://www.w3.org/2018/credentials/v1",
                ["name"] = "Verifiable Credential",
                ["description"] = "Generic verifiable credential"
            }
        };
    }

    private PresentationDefinition? CreatePresentationDefinition(string credentialType)
    {
        // For certain credential types, require presentation of existing credentials
        if (credentialType.ToLowerInvariant() == "driverlicense")
        {
            return new PresentationDefinition
            {
                Id = $"pd_{Guid.NewGuid()}",
                InputDescriptors = new List<InputDescriptor>
                {
                    new InputDescriptor
                    {
                        Id = "proof_of_identity",
                        Name = "Proof of Identity",
                        Purpose = "Verify identity before issuing driver license",
                        Constraints = new InputConstraints
                        {
                            Fields = new List<FieldConstraint>
                            {
                                new FieldConstraint
                                {
                                    Path = new[] { "$.credentialSubject.firstName" },
                                    Filter = new { type = "string" }
                                },
                                new FieldConstraint
                                {
                                    Path = new[] { "$.credentialSubject.lastName" },
                                    Filter = new { type = "string" }
                                },
                                new FieldConstraint
                                {
                                    Path = new[] { "$.credentialSubject.dateOfBirth" },
                                    Filter = new { type = "string", format = "date" }
                                }
                            }
                        }
                    }
                }
            };
        }

        return null;
    }
}

// DTOs for Credential Manifest
public class CredentialManifest
{
    public required string Id { get; set; }
    public required string Version { get; set; }
    public required string SpecVersion { get; set; }
    public required ManifestIssuer Issuer { get; set; }
    public required Dictionary<string, object> OutputDescriptors { get; set; }
    public PresentationDefinition? PresentationDefinition { get; set; }
}

public class ManifestIssuer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public IssuerStyles? Styles { get; set; }
}

public class IssuerStyles
{
    public StyleDescriptor? Thumbnail { get; set; }
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
}

public class StyleDescriptor
{
    public required string Uri { get; set; }
    public string? Alt { get; set; }
}

public class PresentationDefinition
{
    public required string Id { get; set; }
    public List<InputDescriptor> InputDescriptors { get; set; } = new();
}

public class InputDescriptor
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Purpose { get; set; }
    public required InputConstraints Constraints { get; set; }
}

public class InputConstraints
{
    public List<FieldConstraint> Fields { get; set; } = new();
}

public class FieldConstraint
{
    public required string[] Path { get; set; }
    public object? Filter { get; set; }
    public bool? Optional { get; set; }
}

public class CredentialApplication
{
    public required string Id { get; set; }
    public required string SpecVersion { get; set; }
    public required string ManifestId { get; set; }
    public required string ApplicantId { get; set; }
    public required Dictionary<string, object> SubmissionData { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CredentialResponse
{
    public required string Id { get; set; }
    public required string SpecVersion { get; set; }
    public required string ApplicationId { get; set; }
    public bool Approved { get; set; }
    public string? CredentialId { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface ICredentialManifestService
{
    Task<CredentialManifest> CreateManifestAsync(string credentialType, string issuerId, Dictionary<string, object>? outputDescriptors = null);
    Task<bool> ValidateManifestAsync(string manifestJson);
    Task<CredentialApplication> CreateApplicationAsync(string manifestId, string applicantId, Dictionary<string, object> claims);
    Task<CredentialResponse> CreateResponseAsync(string applicationId, bool approved, string? credentialId = null, string? rejectionReason = null);
}