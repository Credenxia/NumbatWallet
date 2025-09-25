using System.Text.Json;

namespace NumbatWallet.Application.Services;

/// <summary>
/// Service for handling JSON-LD context and processing for Verifiable Credentials
/// </summary>
public class JsonLdService : IJsonLdService
{
    private readonly Dictionary<string, object> _defaultContext = new()
    {
        ["@version"] = 1.1,
        ["@base"] = "https://www.w3.org/2018/credentials/v1",
        ["vc"] = "https://www.w3.org/2018/credentials#",
        ["schema"] = "http://schema.org/",
        ["credentialSubject"] = "vc:credentialSubject",
        ["issuer"] = "vc:issuer",
        ["issuanceDate"] = "vc:issuanceDate",
        ["expirationDate"] = "vc:expirationDate",
        ["proof"] = "vc:proof",
        ["verificationMethod"] = "vc:verificationMethod"
    };

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public Task<string> AddContextAsync(object credential, string[]? additionalContexts = null)
    {
        ArgumentNullException.ThrowIfNull(credential);

        var contexts = new List<object>
        {
            "https://www.w3.org/2018/credentials/v1"
        };

        if (additionalContexts != null)
        {
            contexts.AddRange(additionalContexts);
        }

        // Add Australian government context for local credentials
        contexts.Add("https://standards.gov.au/credentials/v1");

        var credentialDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(credential)) ?? new Dictionary<string, object>();

        credentialDict["@context"] = contexts;

        return Task.FromResult(JsonSerializer.Serialize(credentialDict, _jsonOptions));
    }

    public Task<bool> ValidateContextAsync(string jsonLd)
    {
        if (string.IsNullOrWhiteSpace(jsonLd))
        {
            return Task.FromResult(false);
        }

        try
        {
            var doc = JsonDocument.Parse(jsonLd);

            // Check if @context exists
            if (!doc.RootElement.TryGetProperty("@context", out var contextElement))
            {
                return Task.FromResult(false);
            }

            // Validate that it includes the W3C credentials context
            if (contextElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var ctx in contextElement.EnumerateArray())
                {
                    if (ctx.ValueKind == JsonValueKind.String &&
                        ctx.GetString() == "https://www.w3.org/2018/credentials/v1")
                    {
                        return Task.FromResult(true);
                    }
                }
            }
            else if (contextElement.ValueKind == JsonValueKind.String)
            {
                return Task.FromResult(contextElement.GetString() == "https://www.w3.org/2018/credentials/v1");
            }

            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<object> ExpandAsync(string jsonLd)
    {
        // Basic expansion - for POA phase, return as-is
        // Full implementation would use a JSON-LD processor
        try
        {
            var doc = JsonDocument.Parse(jsonLd);
            return Task.FromResult<object>(doc.RootElement);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to expand JSON-LD", ex);
        }
    }

    public Task<string> CompactAsync(object expanded, object? context = null)
    {
        // Basic compaction - for POA phase, serialize as-is
        // Full implementation would use a JSON-LD processor
        return Task.FromResult(JsonSerializer.Serialize(expanded, _jsonOptions));
    }
}

public interface IJsonLdService
{
    Task<string> AddContextAsync(object credential, string[]? additionalContexts = null);
    Task<bool> ValidateContextAsync(string jsonLd);
    Task<object> ExpandAsync(string jsonLd);
    Task<string> CompactAsync(object expanded, object? context = null);
}