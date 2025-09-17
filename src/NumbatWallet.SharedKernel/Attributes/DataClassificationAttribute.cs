using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.SharedKernel.Attributes;

/// <summary>
/// Attribute to mark properties with their data classification level.
/// Classification drives display and audit requirements, while tenant policies control actual protection.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
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
    /// Creates a new DataClassificationAttribute
    /// </summary>
    /// <param name="classification">The data classification level</param>
    /// <param name="purpose">Optional purpose/context for the field</param>
    public DataClassificationAttribute(DataClassification classification, string? purpose = null)
    {
        Classification = classification;
        Purpose = purpose;
    }
}