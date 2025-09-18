using NumbatWallet.Application.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Infrastructure.Services;

public class RedactionService : IRedactionService
{
    private const string RedactionChar = "*";

    public async Task<string> RedactAsync(
        string value,
        PiiType piiType,
        RedactionPattern pattern,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var redacted = pattern switch
        {
            RedactionPattern.ShowLastFour => ShowLastFour(value),
            RedactionPattern.ShowFirstThree => ShowFirstThree(value),
            RedactionPattern.ShowDomain => ShowDomain(value),
            RedactionPattern.Full => FullRedaction(value),
            _ => value
        };

        await Task.CompletedTask;
        return redacted;
    }

    public async Task<Dictionary<string, string>> RedactBulkAsync(
        Dictionary<string, (string Value, PiiType PiiType, RedactionPattern Pattern)> values,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, string>();

        foreach (var kvp in values)
        {
            results[kvp.Key] = await RedactAsync(
                kvp.Value.Value,
                kvp.Value.PiiType,
                kvp.Value.Pattern,
                cancellationToken);
        }

        return results;
    }

    public RedactionPattern GetDefaultPattern(PiiType piiType)
    {
        return piiType switch
        {
            PiiType.Email => RedactionPattern.ShowDomain,
            PiiType.Phone => RedactionPattern.ShowLastFour,
            PiiType.Name => RedactionPattern.ShowFirstThree,
            PiiType.Address => RedactionPattern.Full,
            PiiType.DateOfBirth => RedactionPattern.Full,
            PiiType.TaxId => RedactionPattern.ShowLastFour,
            PiiType.PaymentCard => RedactionPattern.ShowLastFour,
            PiiType.AccountNumber => RedactionPattern.ShowLastFour,
            PiiType.IpAddress => RedactionPattern.ShowFirstThree,
            PiiType.BiometricData => RedactionPattern.Full,
            PiiType.HealthData => RedactionPattern.Full,
            PiiType.GovernmentId => RedactionPattern.ShowLastFour,
            _ => RedactionPattern.Full
        };
    }

    private string ShowLastFour(string value)
    {
        if (value.Length <= 4)
        {
            return new string('*', value.Length);
        }

        var visiblePart = value.Substring(value.Length - 4);
        var redactedPart = new string('*', value.Length - 4);
        return redactedPart + visiblePart;
    }

    private string ShowFirstThree(string value)
    {
        if (value.Length <= 3)
        {
            return value;
        }

        var visiblePart = value.Substring(0, 3);
        var redactedPart = new string('*', value.Length - 3);
        return visiblePart + redactedPart;
    }

    private string ShowDomain(string value)
    {
        // For email addresses, show domain
        var atIndex = value.IndexOf('@');
        if (atIndex > 0 && atIndex < value.Length - 1)
        {
            var domain = value.Substring(atIndex);
            var redactedLocal = new string('*', Math.Min(atIndex, 3));
            return redactedLocal + domain;
        }

        // For URLs, show domain
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return $"***{uri.Host}***";
        }

        // Fallback to showing last part
        return ShowLastFour(value);
    }

    private string FullRedaction(string value)
    {
        // Preserve length for context but redact all
        return new string('*', Math.Min(value.Length, 3));
    }
}