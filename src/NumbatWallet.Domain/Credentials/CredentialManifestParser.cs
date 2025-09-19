using System.Text.Json;

namespace NumbatWallet.Domain.Credentials;

/// <summary>
/// Parser for Credential Manifest JSON documents
/// </summary>
public class CredentialManifestParser
{
    private readonly JsonSerializerOptions _options;

    public CredentialManifestParser()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };
    }

    public CredentialManifest Parse(string manifestJson)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
        {
            throw new ArgumentNullException(nameof(manifestJson));
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<CredentialManifest>(manifestJson, _options);

            if (manifest == null)
            {
                throw new InvalidOperationException("Failed to parse credential manifest");
            }

            // Validate basic structure
            if (string.IsNullOrEmpty(manifest.Id))
            {
                throw new InvalidOperationException("Credential manifest must have an id");
            }

            if (manifest.Issuer == null)
            {
                throw new InvalidOperationException("Credential manifest must have an issuer");
            }

            if (manifest.OutputDescriptors == null || manifest.OutputDescriptors.Count == 0)
            {
                throw new InvalidOperationException("Credential manifest must have at least one output descriptor");
            }

            return manifest;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse credential manifest: {ex.Message}", ex);
        }
    }

    public string Serialize(CredentialManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        return JsonSerializer.Serialize(manifest, _options);
    }
}