namespace NumbatWallet.Domain.Credentials;

/// <summary>
/// Generates credentials based on manifest specifications
/// </summary>
public class CredentialGenerator
{
    public Dictionary<string, object> GenerateFromManifest(CredentialManifest manifest, Dictionary<string, object> claimData)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(claimData);

        var credential = new Dictionary<string, object>
        {
            ["@context"] = new[]
            {
                "https://www.w3.org/2018/credentials/v1"
            },
            ["id"] = $"urn:uuid:{Guid.NewGuid()}",
            ["type"] = new[] { "VerifiableCredential" },
            ["issuer"] = manifest.Issuer.Id,
            ["issuanceDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
        };

        // Add credential type from output descriptor
        if (manifest.OutputDescriptors.Count > 0)
        {
            var outputDescriptor = manifest.OutputDescriptors[0];

            // Add schema-based type
            if (!string.IsNullOrEmpty(outputDescriptor.Schema))
            {
                var schemaType = ExtractTypeFromSchema(outputDescriptor.Schema);
                if (!string.IsNullOrEmpty(schemaType))
                {
                    var types = new List<string> { "VerifiableCredential", schemaType };
                    credential["type"] = types.ToArray();
                }
            }

            // Add display metadata if present
            if (outputDescriptor.Display != null)
            {
                var credentialMetadata = new Dictionary<string, object>();

                if (outputDescriptor.Display.Title != null)
                {
                    credentialMetadata["name"] = outputDescriptor.Display.Title.Text;
                }

                if (outputDescriptor.Display.Description != null)
                {
                    credentialMetadata["description"] = outputDescriptor.Display.Description.Text;
                }

                if (credentialMetadata.Count > 0)
                {
                    credential["credentialMetadata"] = credentialMetadata;
                }
            }
        }

        // Build credential subject from claim data
        var credentialSubject = new Dictionary<string, object>(claimData);

        // Add subject ID if not present
        if (!credentialSubject.ContainsKey("id"))
        {
            credentialSubject["id"] = $"did:example:subject:{Guid.NewGuid():N}";
        }

        credential["credentialSubject"] = credentialSubject;

        // Add proof placeholder (would be signed in production)
        credential["proof"] = new Dictionary<string, object>
        {
            ["type"] = "RsaSignature2018",
            ["created"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            ["proofPurpose"] = "assertionMethod",
            ["verificationMethod"] = $"{manifest.Issuer.Id}#key-1"
        };

        return credential;
    }

    private string ExtractTypeFromSchema(string schemaUrl)
    {
        // Extract type from schema URL
        // e.g., "https://schema.org/DriverLicense" -> "DriverLicense"
        if (string.IsNullOrEmpty(schemaUrl))
        {
            return string.Empty;
        }

        var lastSlash = schemaUrl.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < schemaUrl.Length - 1)
        {
            return schemaUrl.Substring(lastSlash + 1);
        }

        var lastHash = schemaUrl.LastIndexOf('#');
        if (lastHash >= 0 && lastHash < schemaUrl.Length - 1)
        {
            return schemaUrl.Substring(lastHash + 1);
        }

        return string.Empty;
    }
}