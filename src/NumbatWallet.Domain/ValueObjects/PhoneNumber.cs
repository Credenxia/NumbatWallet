using NumbatWallet.SharedKernel.Base;
using NumbatWallet.SharedKernel.Utilities;
using PhoneNumbers;

namespace NumbatWallet.Domain.ValueObjects;

public class PhoneNumber : ValueObject
{
    private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

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

        try
        {
            // Clean the input - remove common formatting characters but keep + and digits
            var cleaned = phoneNumber.Trim();

            // If starts with 00, replace with +
            if (cleaned.StartsWith("00", StringComparison.Ordinal))
            {
                cleaned = string.Concat("+", cleaned.AsSpan(2));
            }

            // Parse the phone number
            PhoneNumbers.PhoneNumber parsedNumber;

            if (cleaned.StartsWith('+'))
            {
                // International format - parse without region
                parsedNumber = PhoneUtil.Parse(cleaned, null);
            }
            else
            {
                // National format - need a region hint
                // Default to AU if no country code provided (backward compatibility)
                var region = countryCode ?? "AU";

                // Convert country code to region if it's a number
                if (int.TryParse(region, out var _))
                {
                    // If numeric country code provided (like "61"), use default region
                    region = "AU";
                }

                parsedNumber = PhoneUtil.Parse(cleaned, region.ToUpperInvariant());
            }

            // Validate the parsed number
            if (!PhoneUtil.IsValidNumber(parsedNumber))
            {
                throw new ArgumentException($"Invalid phone number: {phoneNumber}", nameof(phoneNumber));
            }

            // Format as E164 for storage
            var e164Format = PhoneUtil.Format(parsedNumber, PhoneNumberFormat.E164);

            // Get the region code for this number
            var regionCode = PhoneUtil.GetRegionCodeForNumber(parsedNumber);

            return new PhoneNumber(e164Format, regionCode);
        }
        catch (NumberParseException ex)
        {
            throw new ArgumentException($"Invalid phone number format: {phoneNumber}. {ex.Message}", nameof(phoneNumber));
        }
    }

    public string GetFormatted(bool international = false)
    {
        try
        {
            // Parse the stored E164 number
            var parsedNumber = PhoneUtil.Parse(Value, null);

            // Format based on preference
            if (international)
            {
                return PhoneUtil.Format(parsedNumber, PhoneNumberFormat.INTERNATIONAL);
            }
            else
            {
                // Use national format for the number's region
                return PhoneUtil.Format(parsedNumber, PhoneNumberFormat.NATIONAL);
            }
        }
        catch
        {
            // Fallback to stored value if parsing fails
            return Value;
        }
    }

    public string GetRegion()
    {
        try
        {
            var parsedNumber = PhoneUtil.Parse(Value, null);
            return PhoneUtil.GetRegionCodeForNumber(parsedNumber);
        }
        catch
        {
            return CountryCode ?? "Unknown";
        }
    }

    public PhoneNumberType GetNumberType()
    {
        try
        {
            var parsedNumber = PhoneUtil.Parse(Value, null);
            return PhoneUtil.GetNumberType(parsedNumber);
        }
        catch
        {
            return PhoneNumberType.UNKNOWN;
        }
    }

    public bool IsMobile()
    {
        var type = GetNumberType();
        return type == PhoneNumberType.MOBILE || type == PhoneNumberType.FIXED_LINE_OR_MOBILE;
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}