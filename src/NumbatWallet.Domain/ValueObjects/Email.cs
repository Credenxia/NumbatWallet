using System.Text.RegularExpressions;
using NumbatWallet.SharedKernel.Base;
using NumbatWallet.SharedKernel.Utilities;

namespace NumbatWallet.Domain.ValueObjects;

public class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private Email(string value)
    {
        Value = value;
    }

    // EF Core constructor
    private Email()
    {
        Value = string.Empty;
    }

    public string Value { get; private set; }
    public string Domain => Value.Contains('@') ? Value.Split('@')[1] : string.Empty;
    public string LocalPart => Value.Contains('@') ? Value.Split('@')[0] : string.Empty;

    public static Email Create(string email)
    {
        Guard.AgainstNullOrWhiteSpace(email, nameof(email));

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalizedEmail))
        {
            throw new ArgumentException($"Invalid email format: {email}", nameof(email));
        }

        return new Email(normalizedEmail);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}