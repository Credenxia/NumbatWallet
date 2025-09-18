using System.Text.RegularExpressions;
using NumbatWallet.SharedKernel.Base;
using NumbatWallet.SharedKernel.Utilities;

namespace NumbatWallet.Domain.ValueObjects;

public class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = new(
        @"^\+[1-9]\d{7,14}$",  // Must start with +, country code, then 7-14 more digits (8-15 total)
        RegexOptions.Compiled);

    private PhoneNumber(string value, string? countryCode)
    {
        Value = value;
        CountryCode = countryCode;
    }

    // EF Core constructor
    private PhoneNumber()
    {
        Value = string.Empty;
        CountryCode = null;
    }

    public string Value { get; private set; }
    public string? CountryCode { get; private set; }

    public static PhoneNumber Create(string phoneNumber, string? countryCode = null)
    {
        Guard.AgainstNullOrWhiteSpace(phoneNumber, nameof(phoneNumber));

        // Check if the original contains any letters (invalid)
        if (Regex.IsMatch(phoneNumber, @"[a-zA-Z]"))
        {
            throw new ArgumentException($"Invalid phone number format: {phoneNumber}", nameof(phoneNumber));
        }

        // Remove spaces, dashes, parentheses but keep + and digits
        var cleaned = Regex.Replace(phoneNumber, @"[^\d+]", "");

        // Ensure it starts with + for international format
        if (!cleaned.StartsWith('+'))
        {
            // If no + and country code provided, add it
            if (!string.IsNullOrEmpty(countryCode))
            {
                cleaned = $"+{countryCode}{cleaned}";
            }
            else
            {
                // Phone numbers must have international format
                throw new ArgumentException($"Invalid phone number format: {phoneNumber}", nameof(phoneNumber));
            }
        }

        if (!PhoneRegex.IsMatch(cleaned))
        {
            throw new ArgumentException($"Invalid phone number format: {phoneNumber}", nameof(phoneNumber));
        }

        return new PhoneNumber(cleaned, countryCode);
    }

    public string GetFormatted()
    {
        // Handle US numbers (+1 followed by 10 digits)
        if (Value.StartsWith("+1", StringComparison.Ordinal) && Value.Length == 12)
        {
            var withoutCountryCode = Value.Substring(2);
            return $"+1 {withoutCountryCode.Substring(0, 3)} {withoutCountryCode.Substring(3, 3)} {withoutCountryCode.Substring(6, 4)}";
        }

        // Handle Australian numbers (+61 followed by 9 digits)
        if (Value.StartsWith("+61", StringComparison.Ordinal) && Value.Length == 12)
        {
            return $"+61 {Value.Substring(3, 3)} {Value.Substring(6, 3)} {Value.Substring(9, 3)}";
        }

        // For other formats, return as-is
        return Value;
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}