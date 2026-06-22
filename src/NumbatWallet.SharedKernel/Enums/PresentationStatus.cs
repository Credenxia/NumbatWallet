namespace NumbatWallet.SharedKernel.Enums;

/// <summary>
/// Lifecycle status of a credential presentation.
/// </summary>
public enum PresentationStatus
{
    /// <summary>Presented to a verifier but not yet verified.</summary>
    Pending = 0,

    /// <summary>Verified at least once by a verifier.</summary>
    Verified = 1,

    /// <summary>The presentation (token) lifetime has elapsed.</summary>
    Expired = 2,

    /// <summary>Rejected; can no longer be verified.</summary>
    Rejected = 3
}
