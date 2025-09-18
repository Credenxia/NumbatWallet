using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.SharedKernel.Attributes;

/// <summary>
/// Attribute to mark properties with their data classification level.
/// Classification drives display and audit requirements, while tenant policies control actual protection.
/// Aligned with Australian Protective Security Policy Framework (PSPF) and Information Security Manual (ISM).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public class DataClassificationAttribute : Attribute
{
    /// <summary>
    /// The classification level of the data
    /// </summary>
    public DataClassification Classification { get; }

    /// <summary>
    /// Optional purpose or context for the field (e.g., "Identity", "Contact", "Financial")
    /// Used for compliance mapping and audit categorization
    /// </summary>
    public string? Purpose { get; }

    /// <summary>
    /// Whether this data requires encryption at rest and in transit
    /// </summary>
    public bool RequiresEncryption { get; }

    /// <summary>
    /// Whether access to this data requires audit logging
    /// </summary>
    public bool RequiresAudit { get; }

    /// <summary>
    /// Default retention period in days (-1 for indefinite)
    /// </summary>
    public int RetentionDays { get; }

    /// <summary>
    /// Creates a new DataClassificationAttribute with automatic configuration based on classification level
    /// </summary>
    /// <param name="classification">The data classification level</param>
    /// <param name="purpose">Optional purpose/context for the field</param>
    public DataClassificationAttribute(DataClassification classification, string? purpose = null)
    {
        Classification = classification;
        Purpose = purpose;

        // Auto-configure based on classification level per PSPF/ISM standards
        RequiresEncryption = classification >= DataClassification.OfficialSensitive;
        RequiresAudit = classification >= DataClassification.Official;

        // Default retention periods per PSPF guidelines
        RetentionDays = classification switch
        {
            DataClassification.Unofficial => 365,              // 1 year
            DataClassification.Official => 365 * 3,           // 3 years
            DataClassification.OfficialSensitive => 365 * 7,  // 7 years
            DataClassification.Protected => 365 * 25,         // 25 years
            DataClassification.Secret => -1,                  // Indefinite
            DataClassification.TopSecret => -1,               // Indefinite
            _ => 365 * 7                                      // Default to 7 years
        };
    }
}

/// <summary>
/// Attribute to specify handling caveats for classified information
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
public class HandlingCaveatsAttribute : Attribute
{
    /// <summary>
    /// The handling caveats that apply to this data
    /// </summary>
    public string[] Caveats { get; }

    /// <summary>
    /// Creates a new HandlingCaveatsAttribute
    /// </summary>
    /// <param name="caveats">Handling caveats (e.g., "FOR-OFFICIAL-USE-ONLY", "CABINET-IN-CONFIDENCE")</param>
    public HandlingCaveatsAttribute(params string[] caveats)
    {
        Caveats = caveats ?? Array.Empty<string>();
    }
}