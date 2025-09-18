using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for redacting sensitive information based on PII type
/// </summary>
public interface IRedactionService
{
    /// <summary>
    /// Redacts a value based on its PII type and pattern
    /// </summary>
    /// <param name="value">The value to redact</param>
    /// <param name="piiType">The PII type</param>
    /// <param name="pattern">The redaction pattern to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The redacted value</returns>
    Task<string> RedactAsync(
        string value,
        PiiType piiType,
        RedactionPattern pattern,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Redacts multiple values in bulk
    /// </summary>
    /// <param name="values">Dictionary of values to redact with their PII types</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of redacted values</returns>
    Task<Dictionary<string, string>> RedactBulkAsync(
        Dictionary<string, (string Value, PiiType PiiType, RedactionPattern Pattern)> values,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default redaction pattern for a PII type
    /// </summary>
    /// <param name="piiType">The PII type</param>
    /// <returns>The default redaction pattern</returns>
    RedactionPattern GetDefaultPattern(PiiType piiType);
}