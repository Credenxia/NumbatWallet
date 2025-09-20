using NumbatWallet.SharedKernel.Base;
using NumbatWallet.SharedKernel.Utilities;
using System.Text.RegularExpressions;

namespace NumbatWallet.Domain.ValueObjects;

/// <summary>
/// Credential Schema Identifier value object
/// </summary>
public class CredentialSchemaId : ValueObject
{
    private static readonly Regex SchemaIdRegex = new(
        @"^https?://[a-zA-Z0-9.-]+(/[a-zA-Z0-9._~:/?#\[\]@!$&'()*+,;=-]*)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private CredentialSchemaId(string value)
    {
        Value = value;
        Uri = new Uri(value);
    }

    // ORM constructor
    private CredentialSchemaId()
    {
        Value = string.Empty;
        Uri = new Uri("http://example.com");
    }

    public string Value { get; private set; }
    public Uri Uri { get; private set; }
    public string Host => Uri?.Host ?? string.Empty;
    public string Path => Uri?.AbsolutePath ?? string.Empty;

    public static CredentialSchemaId Create(string schemaId)
    {
        Guard.AgainstNullOrWhiteSpace(schemaId, nameof(schemaId));

        if (!SchemaIdRegex.IsMatch(schemaId))
        {
            throw new ArgumentException($"Invalid schema ID format: {schemaId}", nameof(schemaId));
        }

        if (!Uri.TryCreate(schemaId, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"Schema ID must be a valid absolute URI: {schemaId}", nameof(schemaId));
        }

        return new CredentialSchemaId(schemaId);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(CredentialSchemaId schemaId) => schemaId.Value;
}