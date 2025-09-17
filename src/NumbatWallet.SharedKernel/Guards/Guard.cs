using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NumbatWallet.SharedKernel.Guards;

public static class Guard
{
    public static T Against<T>(
        [DoesNotReturnIf(false)] bool condition,
        T value,
        string message,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (!condition)
        {
            throw new ArgumentException(message, paramName);
        }

        return value;
    }

    public static T AgainstNull<T>(
        [NotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return value;
    }

    public static string AgainstNullOrEmpty(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Value cannot be null or empty.", paramName);
        }

        return value;
    }

    public static string AgainstNullOrWhiteSpace(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        }

        return value;
    }

    public static T AgainstOutOfRange<T>(
        T value,
        T minValue,
        T maxValue,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(minValue) < 0 || value.CompareTo(maxValue) > 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value,
                $"Value must be between {minValue} and {maxValue}.");
        }

        return value;
    }

    public static Guid AgainstEmptyGuid(
        Guid value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be an empty GUID.", paramName);
        }

        return value;
    }

    public static T AgainstNegative<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>, IComparable
    {
        var zero = (T)Convert.ChangeType(0, typeof(T));
        if (value.CompareTo(zero) < 0)
        {
            throw new ArgumentException("Value cannot be negative.", paramName);
        }

        return value;
    }

    public static T AgainstZero<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>, IComparable
    {
        var zero = (T)Convert.ChangeType(0, typeof(T));
        if (value.CompareTo(zero) == 0)
        {
            throw new ArgumentException("Value cannot be zero.", paramName);
        }

        return value;
    }
}