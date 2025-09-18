using System.Text.RegularExpressions;
using NumbatWallet.SharedKernel.Base;
using NumbatWallet.SharedKernel.Utilities;

namespace NumbatWallet.Domain.ValueObjects;

public class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    private PhoneNumber(string value, string? countryCode)
    {
        Value = value;
        CountryCode = countryCode;
    }

    public string Value { get; }
    public string? CountryCode { get; }

    public static PhoneNumber Create(string phoneNumber, string? countryCode = null)
    {
        Guard.AgainstNullOrWhiteSpace(phoneNumber, nameof(phoneNumber));

        // Remove all non-digit characters except the leading +
        var cleaned = Regex.Replace(phoneNumber, @"[^\d+]", "");

        // Ensure it starts with + if country code is provided
        if (!string.IsNullOrEmpty(countryCode) && !cleaned.StartsWith("+"))
        {
            cleaned = $"+{countryCode}{cleaned}";
        }

        if (!PhoneRegex.IsMatch(cleaned))
        {
            throw new ArgumentException($"Invalid phone number format: {phoneNumber}", nameof(phoneNumber));
        }

        return new PhoneNumber(cleaned, countryCode);
    }

    public string GetFormatted()
    {
        if (Value.Length == 10) // US format
        {
            return $"({Value[0..3]}) {Value[3..6]}-{Value[6..10]}";
        }
        return Value;
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}