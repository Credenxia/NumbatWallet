namespace NumbatWallet.SharedKernel.Enums;

/// <summary>
/// Defines the action to take when purging data based on retention policy
/// </summary>
public enum PurgeAction
{
    /// <summary>
    /// Completely remove the record from the database
    /// </summary>
    Delete = 0,

    /// <summary>
    /// Replace sensitive data with anonymous values
    /// </summary>
    Anonymize = 1,

    /// <summary>
    /// Move data to cold storage/archive
    /// </summary>
    Archive = 2,

    /// <summary>
    /// Reduce data accuracy (e.g., keep only year from date of birth)
    /// </summary>
    ReduceAccuracy = 3,

    /// <summary>
    /// Crypto-shred by destroying encryption keys
    /// </summary>
    CryptoShred = 4,

    /// <summary>
    /// Mark as deleted but retain for compliance
    /// </summary>
    SoftDelete = 5
}