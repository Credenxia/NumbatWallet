namespace NumbatWallet.SharedKernel.Enums;

/// <summary>
/// Defines when and how data purging should occur
/// </summary>
public enum PurgeStrategy
{
    /// <summary>
    /// Purge immediately when retention period expires
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// Purge during scheduled maintenance window
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Purge only when manually triggered
    /// </summary>
    OnDemand = 2,

    /// <summary>
    /// Purge in batches to minimize performance impact
    /// </summary>
    Batched = 3,

    /// <summary>
    /// Never purge (regulatory requirement)
    /// </summary>
    Never = 4
}