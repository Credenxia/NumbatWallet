using System.Text.RegularExpressions;
using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Results;

namespace NumbatWallet.Domain.ValueObjects;

public sealed partial class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = PhoneValidationRegex();

    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static Result<PhoneNumber> Create(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return Error.Validation("PhoneNumber.Empty", "Phone number cannot be empty.");
        }

        if (!PhoneRegex.IsMatch(phoneNumber))
        {
            return Error.Validation("PhoneNumber.Invalid", "Phone number must start with + and contain only digits (minimum 7 digits).");
        }

        return new PhoneNumber(phoneNumber);
    }

    public string GetCountryCode()
    {
        // Extract country code (1-3 digits after +)
        if (Value.Length > 1)
        {
            // Simple extraction - could be enhanced with libphonenumber
            if (Value.StartsWith("+1")) return "1";
            if (Value.StartsWith("+61")) return "61";
            if (Value.StartsWith("+44")) return "44";

            // Default: take first 2 digits after +
            return Value.Length > 2 ? Value[1..3] : Value[1..];
        }
        return string.Empty;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^\+\d{7,15}$", RegexOptions.Compiled)]
    private static partial Regex PhoneValidationRegex();
}