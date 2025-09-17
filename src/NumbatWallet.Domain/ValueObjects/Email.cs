using System.Text.RegularExpressions;
using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Results;

namespace NumbatWallet.Domain.ValueObjects;

public sealed partial class Email : ValueObject
{
    private static readonly Regex EmailRegex = EmailValidationRegex();

    public string Value { get; }

    private Email(string value)
    {
        Value = value.ToLowerInvariant();
    }

    public static Result<Email> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Error.Validation("Email.Empty", "Email cannot be empty.");
        }

        if (!EmailRegex.IsMatch(email))
        {
            return Error.Validation("Email.Invalid", "Email format is invalid.");
        }

        return new Email(email);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$", RegexOptions.Compiled)]
    private static partial Regex EmailValidationRegex();
}