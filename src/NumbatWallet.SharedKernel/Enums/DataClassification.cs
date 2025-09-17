namespace NumbatWallet.SharedKernel.Enums;

/// <summary>
/// Data classification levels aligned with Australian Government PSPF/ISM standards
/// </summary>
public enum DataClassification
{
    /// <summary>
    /// Public, non-government data that can be freely shared
    /// </summary>
    Unofficial = 0,

    /// <summary>
    /// Routine government business information
    /// </summary>
    Official = 1,

    /// <summary>
    /// Sensitive information including PII and commercially sensitive data
    /// Requires enhanced protection measures
    /// </summary>
    OfficialSensitive = 2,

    /// <summary>
    /// High-value information requiring strong access controls
    /// Includes financial data, health records, legal documents
    /// </summary>
    Protected = 3,

    /// <summary>
    /// National security information (not used in POA)
    /// </summary>
    Secret = 4,

    /// <summary>
    /// Highest classification level (not used in POA)
    /// </summary>
    TopSecret = 5
}