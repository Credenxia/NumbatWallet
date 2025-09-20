using NumbatWallet.SharedKernel.Base;
using NumbatWallet.SharedKernel.Utilities;
using System.Text.RegularExpressions;

namespace NumbatWallet.Domain.ValueObjects;

/// <summary>
/// Decentralized Identifier (DID) value object
/// Follows W3C DID specification
/// </summary>
public class Did : ValueObject
{
    private static readonly Regex DidRegex = new(
        @"^did:([a-z0-9]+):((?:[a-zA-Z0-9._-]+|%[0-9a-fA-F]{2})+)$",
        RegexOptions.Compiled);

    private Did(string value)
    {
        Value = value;
        var parts = value.Split(':');
        Method = parts[1];
        MethodSpecificId = parts[2];
    }

    // ORM constructor
    private Did()
    {
        Value = string.Empty;
        Method = string.Empty;
        MethodSpecificId = string.Empty;
    }

    public string Value { get; private set; }
    public string Method { get; private set; }
    public string MethodSpecificId { get; private set; }

    public static Did Create(string did)
    {
        Guard.AgainstNullOrWhiteSpace(did, nameof(did));

        if (!DidRegex.IsMatch(did))
        {
            throw new ArgumentException($"Invalid DID format: {did}. Must follow pattern did:method:id", nameof(did));
        }

        return new Did(did);
    }

    public static Did Generate(string method, string id)
    {
        Guard.AgainstNullOrWhiteSpace(method, nameof(method));
        Guard.AgainstNullOrWhiteSpace(id, nameof(id));

        var did = $"did:{method.ToLowerInvariant()}:{id}";
        return Create(did);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(Did did) => did.Value;
}