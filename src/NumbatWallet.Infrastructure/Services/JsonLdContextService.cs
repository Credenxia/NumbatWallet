using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// Enhanced JSON-LD context service with caching and validation
/// POA: Implementation for JSON-LD context handling
/// </summary>
public class JsonLdContextService : IJsonLdContextService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<JsonLdContextService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    // Standard W3C Verifiable Credentials contexts
    private static readonly Dictionary<string, string> StandardContexts = new()
    {
        ["vc"] = "https://www.w3.org/2018/credentials/v1",
        ["vc2"] = "https://www.w3.org/ns/credentials/v2",
        ["did"] = "https://www.w3.org/ns/did/v1",
        ["sec"] = "https://w3id.org/security/v1",
        ["schema"] = "http://schema.org/"
    };

    // Australian Government specific contexts
    private static readonly Dictionary<string, object> AustralianContexts = new()
    {
        ["au-gov"] = new Dictionary<string, object>
        {
            ["@context"] = new Dictionary<string, object>
            {
                ["@version"] = 1.1,
                ["@base"] = "https://standards.gov.au/credentials/",
                ["aug"] = "https://standards.gov.au/credentials#",
                ["tdif"] = "https://www.digitalidentity.gov.au/tdif#",
                ["DriversLicense"] = "aug:DriversLicense",
                ["ProofOfAge"] = "aug:ProofOfAge",
                ["WorkingWithChildrenCheck"] = "aug:WorkingWithChildrenCheck",
                ["licenseNumber"] = "aug:licenseNumber",
                ["licenseClass"] = "aug:licenseClass",
                ["restrictions"] = "aug:restrictions",
                ["organCardNumber"] = "aug:organCardNumber",
                ["dateOfBirth"] = "schema:birthDate",
                ["givenName"] = "schema:givenName",
                ["familyName"] = "schema:familyName",
                ["address"] = "schema:address"
            }
        }
    };

    public JsonLdContextService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<JsonLdContextService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Dictionary<string, object>> GetContextAsync(
        string contextUrl,
        CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cache.TryGetValue($"context:{contextUrl}", out Dictionary<string, object>? cached))
        {
            return cached!;
        }

        // Check standard contexts
        if (StandardContexts.ContainsKey(contextUrl))
        {
            contextUrl = StandardContexts[contextUrl];
        }

        // Check Australian contexts
        if (AustralianContexts.ContainsKey(contextUrl))
        {
            var context = AustralianContexts[contextUrl] as Dictionary<string, object>;
            _cache.Set($"context:{contextUrl}", context, TimeSpan.FromHours(24));
            return context!;
        }

        // Fetch from URL
        try
        {
            var response = await _httpClient.GetAsync(contextUrl, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var context = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                    ?? new Dictionary<string, object>();

                // Cache for 24 hours
                _cache.Set($"context:{contextUrl}", context, TimeSpan.FromHours(24));

                _logger.LogInformation("Successfully fetched and cached context from {Url}", contextUrl);
                return context;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch context from {Url}", contextUrl);
        }

        // Return empty context if fetch fails
        return new Dictionary<string, object>();
    }

    public async Task<string> AddContextToCredentialAsync(
        Dictionary<string, object> credential,
        List<string> contextUrls,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentNullException.ThrowIfNull(contextUrls);

        // Ensure W3C VC context is first
        var contexts = new List<object>();
        if (!contextUrls.Contains(StandardContexts["vc"]))
        {
            contexts.Add(StandardContexts["vc"]);
        }

        // Add additional contexts
        contexts.AddRange(contextUrls);

        // Add Australian government context if dealing with AU credentials
        if (IsAustralianCredential(credential))
        {
            contexts.Add("https://standards.gov.au/credentials/v1");
        }

        credential["@context"] = contexts;

        return JsonSerializer.Serialize(credential, _jsonOptions);
    }

    public async Task<bool> ValidateContextAsync(
        string jsonLd,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonLd))
        {
            return false;
        }

        try
        {
            var doc = JsonDocument.Parse(jsonLd);

            // Check if @context exists
            if (!doc.RootElement.TryGetProperty("@context", out var contextElement))
            {
                _logger.LogWarning("JSON-LD document missing @context");
                return false;
            }

            // Check for required W3C VC context
            bool hasVcContext = false;

            if (contextElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var ctx in contextElement.EnumerateArray())
                {
                    if (ctx.ValueKind == JsonValueKind.String)
                    {
                        var url = ctx.GetString();
                        if (url == StandardContexts["vc"] || url == StandardContexts["vc2"])
                        {
                            hasVcContext = true;
                            break;
                        }
                    }
                }
            }
            else if (contextElement.ValueKind == JsonValueKind.String)
            {
                var url = contextElement.GetString();
                hasVcContext = (url == StandardContexts["vc"] || url == StandardContexts["vc2"]);
            }

            if (!hasVcContext)
            {
                _logger.LogWarning("JSON-LD document missing W3C VC context");
                return false;
            }

            // Validate credential structure
            if (!doc.RootElement.TryGetProperty("type", out _))
            {
                _logger.LogWarning("Credential missing 'type' property");
                return false;
            }

            if (!doc.RootElement.TryGetProperty("credentialSubject", out _))
            {
                _logger.LogWarning("Credential missing 'credentialSubject' property");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate JSON-LD context");
            return false;
        }
    }

    public async Task<Dictionary<string, object>> ExpandCredentialAsync(
        string jsonLd,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jsonLd);

        try
        {
            var doc = JsonDocument.Parse(jsonLd);
            var expanded = new Dictionary<string, object>();

            // Extract and expand based on context
            if (doc.RootElement.TryGetProperty("@context", out var contextElement))
            {
                var contexts = await ParseContextsAsync(contextElement, cancellationToken);

                // Apply context mappings to expand compact terms
                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    if (property.Name.StartsWith('@'))
                    {
                        expanded[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText())!;
                    }
                    else
                    {
                        var expandedName = ExpandTerm(property.Name, contexts);
                        expanded[expandedName] = JsonSerializer.Deserialize<object>(property.Value.GetRawText())!;
                    }
                }
            }
            else
            {
                // No context, return as-is
                expanded = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonLd)!;
            }

            return expanded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expand JSON-LD");
            throw new InvalidOperationException("Failed to expand JSON-LD", ex);
        }
    }

    public async Task<string> CompactCredentialAsync(
        Dictionary<string, object> expanded,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expanded);

        try
        {
            var compacted = new Dictionary<string, object>();

            // Use provided context or default
            context ??= AustralianContexts["au-gov"] as Dictionary<string, object>;

            if (context != null)
            {
                compacted["@context"] = context["@context"];

                // Compact terms based on context
                foreach (var kvp in expanded)
                {
                    if (kvp.Key.StartsWith('@'))
                    {
                        compacted[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        var compactedName = CompactTerm(kvp.Key, context);
                        compacted[compactedName] = kvp.Value;
                    }
                }
            }
            else
            {
                compacted = expanded;
            }

            return JsonSerializer.Serialize(compacted, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compact JSON-LD");
            throw new InvalidOperationException("Failed to compact JSON-LD", ex);
        }
    }

    private bool IsAustralianCredential(Dictionary<string, object> credential)
    {
        // Check if it's an Australian credential type
        if (credential.TryGetValue("type", out var typeObj))
        {
            var types = typeObj switch
            {
                string s => new[] { s },
                IEnumerable<object> list => list.Select(x => x.ToString()).ToArray(),
                _ => Array.Empty<string>()
            };

            return types.Any(t => t != null && (
                t.Contains("DriversLicense") ||
                t.Contains("ProofOfAge") ||
                t.Contains("WorkingWithChildrenCheck") ||
                t.Contains("Australian")));
        }

        return false;
    }

    private async Task<List<Dictionary<string, object>>> ParseContextsAsync(
        JsonElement contextElement,
        CancellationToken cancellationToken)
    {
        var contexts = new List<Dictionary<string, object>>();

        if (contextElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var ctx in contextElement.EnumerateArray())
            {
                if (ctx.ValueKind == JsonValueKind.String)
                {
                    var url = ctx.GetString()!;
                    var context = await GetContextAsync(url, cancellationToken);
                    contexts.Add(context);
                }
                else if (ctx.ValueKind == JsonValueKind.Object)
                {
                    var context = JsonSerializer.Deserialize<Dictionary<string, object>>(ctx.GetRawText())!;
                    contexts.Add(context);
                }
            }
        }
        else if (contextElement.ValueKind == JsonValueKind.String)
        {
            var url = contextElement.GetString()!;
            var context = await GetContextAsync(url, cancellationToken);
            contexts.Add(context);
        }

        return contexts;
    }

    private string ExpandTerm(string term, List<Dictionary<string, object>> contexts)
    {
        // Look for term in contexts and expand
        foreach (var context in contexts)
        {
            if (context.TryGetValue(term, out var expanded))
            {
                return expanded.ToString()!;
            }
        }
        return term;
    }

    private string CompactTerm(string expandedTerm, Dictionary<string, object> context)
    {
        // Look for expanded term in context and compact
        if (context.TryGetValue("@context", out var ctxObj) && ctxObj is Dictionary<string, object> ctx)
        {
            foreach (var kvp in ctx)
            {
                if (kvp.Value?.ToString() == expandedTerm)
                {
                    return kvp.Key;
                }
            }
        }
        return expandedTerm;
    }
}