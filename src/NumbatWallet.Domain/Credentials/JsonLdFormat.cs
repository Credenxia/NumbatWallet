using System.Text.Json;
using System.Text.Json.Serialization;

namespace NumbatWallet.Domain.Credentials;

/// <summary>
/// Implements JSON-LD format for W3C Verifiable Credentials
/// According to https://www.w3.org/TR/vc-data-model/#json-ld
/// </summary>
public class JsonLdFormat : ICredentialFormat
{
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly HashSet<string> _validContexts;

    public JsonLdFormat()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // W3C standard contexts
        _validContexts = new HashSet<string>
        {
            "https://www.w3.org/2018/credentials/v1",
            "https://www.w3.org/2018/credentials/v2",
            "https://w3id.org/citizenship/v1",
            "https://w3id.org/vaccination/v1",
            "https://w3id.org/security/v1"
        };
    }

    public string SerializeCredential(Dictionary<string, object> credentialData, string? signingKey = null)
    {
        if (credentialData == null)
            throw new ArgumentNullException(nameof(credentialData));

        // Ensure @context is present
        if (!credentialData.ContainsKey("@context"))
        {
            throw new InvalidOperationException("JSON-LD credential must contain @context");
        }

        // Ensure type is present
        if (!credentialData.ContainsKey("type"))
        {
            throw new InvalidOperationException("JSON-LD credential must contain type");
        }

        // Add default proof if not present and signing key is provided
        if (!credentialData.ContainsKey("proof") && !string.IsNullOrEmpty(signingKey))
        {
            credentialData["proof"] = CreateDefaultProof(signingKey);
        }

        return JsonSerializer.Serialize(credentialData, _serializerOptions);
    }

    public Dictionary<string, object> DeserializeCredential(string serializedCredential)
    {
        if (string.IsNullOrWhiteSpace(serializedCredential))
            throw new ArgumentNullException(nameof(serializedCredential));

        try
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedCredential, _serializerOptions);
            if (result == null)
                throw new InvalidOperationException("Failed to deserialize JSON-LD credential");

            // Validate required fields
            if (!result.ContainsKey("@context"))
                throw new InvalidOperationException("JSON-LD credential must contain @context");

            if (!result.ContainsKey("type"))
                throw new InvalidOperationException("JSON-LD credential must contain type");

            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to deserialize JSON-LD credential", ex);
        }
    }

    public bool IsValidFormat(string serializedCredential)
    {
        if (string.IsNullOrWhiteSpace(serializedCredential))
            return false;

        try
        {
            var credential = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedCredential);

            if (credential == null)
                return false;

            // Must have @context and type
            if (!credential.ContainsKey("@context") || !credential.ContainsKey("type"))
                return false;

            // Must have credentialSubject
            if (!credential.ContainsKey("credentialSubject"))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsValidContext(string[] contexts)
    {
        if (contexts == null || contexts.Length == 0)
            return false;

        // First context must be W3C credentials context
        if (!contexts[0].StartsWith("https://www.w3.org/2018/credentials/", StringComparison.Ordinal))
            return false;

        // Check if all contexts are valid
        return contexts.All(ctx => _validContexts.Contains(ctx) ||
                                   ctx.StartsWith("https://", StringComparison.Ordinal));
    }

    private Dictionary<string, object> CreateDefaultProof(string signingKey)
    {
        return new Dictionary<string, object>
        {
            ["type"] = "RsaSignature2018",
            ["created"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            ["proofPurpose"] = "assertionMethod",
            ["verificationMethod"] = $"did:key:{signingKey}",
            ["jws"] = GenerateJws(signingKey)
        };
    }

    private string GenerateJws(string signingKey)
    {
        // Simplified JWS generation for demonstration
        // In production, use proper cryptographic signing
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"RS256\"}"));
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(signingKey));
        var signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("signature"));
        return $"{header}.{payload}.{signature}";
    }
}