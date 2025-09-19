using System.Text.Json.Serialization;

namespace NumbatWallet.Domain.Credentials;

/// <summary>
/// Represents a Credential Manifest according to the DIF specification
/// https://identity.foundation/credential-manifest/
/// </summary>
public class CredentialManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("issuer")]
    public ManifestIssuer Issuer { get; set; } = new();

    [JsonPropertyName("output_descriptors")]
    public List<OutputDescriptor> OutputDescriptors { get; set; } = new();

    [JsonPropertyName("presentation_definition")]
    public PresentationDefinition? PresentationDefinition { get; set; }

    [JsonPropertyName("format")]
    public Dictionary<string, object>? Format { get; set; }

    public IEnumerable<string> GetSupportedFormats()
    {
        var formats = new HashSet<string>();

        foreach (var descriptor in OutputDescriptors)
        {
            if (!string.IsNullOrEmpty(descriptor.Format))
            {
                formats.Add(descriptor.Format);
            }
        }

        if (formats.Count == 0)
        {
            // Default formats if none specified
            formats.Add("jwt_vc");
            formats.Add("ldp_vc");
        }

        return formats;
    }
}

public class ManifestIssuer
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("styles")]
    public Dictionary<string, object>? Styles { get; set; }
}

public class OutputDescriptor
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("display")]
    public DisplayMetadata? Display { get; set; }

    [JsonPropertyName("styles")]
    public Dictionary<string, object>? Styles { get; set; }
}

public class DisplayMetadata
{
    [JsonPropertyName("title")]
    public LocalizedString? Title { get; set; }

    [JsonPropertyName("subtitle")]
    public LocalizedString? Subtitle { get; set; }

    [JsonPropertyName("description")]
    public LocalizedString? Description { get; set; }

    [JsonPropertyName("properties")]
    public List<DisplayProperty>? Properties { get; set; }
}

public class LocalizedString
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }
}

public class DisplayProperty
{
    [JsonPropertyName("path")]
    public string[] Path { get; set; } = Array.Empty<string>();

    [JsonPropertyName("schema")]
    public SchemaReference? Schema { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("value")]
    public object? Value { get; set; }
}

public class PresentationDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("purpose")]
    public string? Purpose { get; set; }

    [JsonPropertyName("input_descriptors")]
    public List<InputDescriptor> InputDescriptors { get; set; } = new();

    [JsonPropertyName("format")]
    public Dictionary<string, object>? Format { get; set; }
}

public class InputDescriptor
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("purpose")]
    public string? Purpose { get; set; }

    [JsonPropertyName("schema")]
    public List<SchemaReference> Schema { get; set; } = new();

    [JsonPropertyName("constraints")]
    public Constraints? Constraints { get; set; }
}

public class SchemaReference
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool? Required { get; set; }
}

public class Constraints
{
    [JsonPropertyName("fields")]
    public List<FieldConstraint> Fields { get; set; } = new();

    [JsonPropertyName("limit_disclosure")]
    public string? LimitDisclosure { get; set; }
}

public class FieldConstraint
{
    [JsonPropertyName("path")]
    public string[] Path { get; set; } = Array.Empty<string>();

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("purpose")]
    public string? Purpose { get; set; }

    [JsonPropertyName("filter")]
    public Dictionary<string, object>? Filter { get; set; }

    [JsonPropertyName("optional")]
    public bool? Optional { get; set; }
}