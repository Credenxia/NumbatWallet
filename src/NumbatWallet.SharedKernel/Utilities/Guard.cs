using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace NumbatWallet.SharedKernel.Utilities;

/// <summary>
/// Guard clauses for parameter validation
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws if input is null
    /// </summary>
    public static T AgainstNull<T>(T? input, [CallerArgumentExpression("input")] string? parameterName = null) where T : class
    {
        if (input is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return input;
    }

    /// <summary>
    /// Throws if string is null or whitespace
    /// </summary>
    public static string AgainstNullOrWhiteSpace(string? input, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException($"Required input {parameterName} was empty.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Throws if string is null or empty
    /// </summary>
    public static string AgainstNullOrEmpty(string? input, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException($"Required input {parameterName} was empty.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Throws if input is less than minimum
    /// </summary>
    public static int AgainstOutOfRange(int input, int minimum, int maximum, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (input < minimum || input > maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName, input, $"Value must be between {minimum} and {maximum}.");
        }

        return input;
    }

    /// <summary>
    /// Throws if input is less than minimum
    /// </summary>
    public static decimal AgainstOutOfRange(decimal input, decimal minimum, decimal maximum, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (input < minimum || input > maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName, input, $"Value must be between {minimum} and {maximum}.");
        }

        return input;
    }

    /// <summary>
    /// Throws if input is negative
    /// </summary>
    public static int AgainstNegative(int input, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (input < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, input, "Value cannot be negative.");
        }

        return input;
    }

    /// <summary>
    /// Throws if input is negative
    /// </summary>
    public static decimal AgainstNegative(decimal input, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (input < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, input, "Value cannot be negative.");
        }

        return input;
    }

    /// <summary>
    /// Throws if input is zero
    /// </summary>
    public static int AgainstZero(int input, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (input == 0)
        {
            throw new ArgumentException("Value cannot be zero.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Throws if input is zero
    /// </summary>
    public static decimal AgainstZero(decimal input, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (input == 0)
        {
            throw new ArgumentException("Value cannot be zero.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Throws if input does not match regex pattern
    /// </summary>
    public static string AgainstInvalidFormat(string input, string pattern, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        AgainstNullOrWhiteSpace(input, parameterName);

        if (!Regex.IsMatch(input, pattern))
        {
            throw new ArgumentException($"Input {parameterName} was not in the required format.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Throws if string exceeds maximum length
    /// </summary>
    public static string AgainstMaxLength(string input, int maxLength, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        AgainstNullOrWhiteSpace(input, parameterName);

        if (input.Length > maxLength)
        {
            throw new ArgumentException($"Input {parameterName} exceeded maximum length of {maxLength}.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Throws if collection is null or empty
    /// </summary>
    public static IEnumerable<T> AgainstNullOrEmpty<T>(IEnumerable<T>? input, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (input is null || !input.Any())
        {
            throw new ArgumentException($"Required input {parameterName} was empty.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Throws if date is in the future
    /// </summary>
    public static DateTime AgainstFutureDate(DateTime input, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (input > DateTime.UtcNow)
        {
            throw new ArgumentException($"Date {parameterName} cannot be in the future.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Throws if date is in the past
    /// </summary>
    public static DateTime AgainstPastDate(DateTime input, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (input < DateTime.UtcNow)
        {
            throw new ArgumentException($"Date {parameterName} cannot be in the past.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Throws if date is in the future
    /// </summary>
    public static DateTimeOffset AgainstFutureDate(DateTimeOffset input, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (input > DateTimeOffset.UtcNow)
        {
            throw new ArgumentException($"Date {parameterName} cannot be in the future.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Throws if date is in the past
    /// </summary>
    public static DateTimeOffset AgainstPastDate(DateTimeOffset input, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (input < DateTimeOffset.UtcNow)
        {
            throw new ArgumentException($"Date {parameterName} cannot be in the past.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Throws if Guid is empty
    /// </summary>
    public static Guid AgainstEmptyGuid(Guid input, [CallerArgumentExpression("input")] string? parameterName = null)
    {
        if (input == Guid.Empty)
        {
            throw new ArgumentException($"Guid {parameterName} cannot be empty.", parameterName);
        }

        return input;
    }
}