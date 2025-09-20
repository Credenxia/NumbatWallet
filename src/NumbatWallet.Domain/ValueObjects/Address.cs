using NumbatWallet.SharedKernel.Base;
using NumbatWallet.SharedKernel.Utilities;

namespace NumbatWallet.Domain.ValueObjects;

/// <summary>
/// Physical address value object
/// </summary>
public class Address : ValueObject
{
    private Address(
        string streetLine1,
        string? streetLine2,
        string city,
        string state,
        string postalCode,
        string country)
    {
        StreetLine1 = streetLine1;
        StreetLine2 = streetLine2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    // ORM constructor
    private Address()
    {
        StreetLine1 = string.Empty;
        StreetLine2 = null;
        City = string.Empty;
        State = string.Empty;
        PostalCode = string.Empty;
        Country = string.Empty;
    }

    public string StreetLine1 { get; private set; }
    public string? StreetLine2 { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }

    public static Address Create(
        string streetLine1,
        string? streetLine2,
        string city,
        string state,
        string postalCode,
        string country = "AU")
    {
        Guard.AgainstNullOrWhiteSpace(streetLine1, nameof(streetLine1));
        Guard.AgainstNullOrWhiteSpace(city, nameof(city));
        Guard.AgainstNullOrWhiteSpace(state, nameof(state));
        Guard.AgainstNullOrWhiteSpace(postalCode, nameof(postalCode));
        Guard.AgainstNullOrWhiteSpace(country, nameof(country));

        // Validate Australian postal code if country is AU
        if (country.Equals("AU", StringComparison.OrdinalIgnoreCase))
        {
            if (postalCode.Length != 4 || !int.TryParse(postalCode, out _))
            {
                throw new ArgumentException("Australian postal code must be 4 digits", nameof(postalCode));
            }
        }

        return new Address(
            streetLine1.Trim(),
            streetLine2?.Trim(),
            city.Trim(),
            state.Trim().ToUpperInvariant(),
            postalCode.Trim(),
            country.Trim().ToUpperInvariant());
    }

    public string GetFullAddress()
    {
        var lines = new List<string> { StreetLine1 };

        if (!string.IsNullOrWhiteSpace(StreetLine2))
        {
            lines.Add(StreetLine2);
        }

        lines.Add($"{City}, {State} {PostalCode}");
        lines.Add(Country);

        return string.Join(Environment.NewLine, lines);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return StreetLine1.ToLowerInvariant();
        yield return StreetLine2?.ToLowerInvariant() ?? string.Empty;
        yield return City.ToLowerInvariant();
        yield return State.ToLowerInvariant();
        yield return PostalCode.ToLowerInvariant();
        yield return Country.ToLowerInvariant();
    }

    public override string ToString() => GetFullAddress();
}