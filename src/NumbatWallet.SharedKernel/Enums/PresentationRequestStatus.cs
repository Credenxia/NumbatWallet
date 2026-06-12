namespace NumbatWallet.SharedKernel.Enums;

/// <summary>
/// Lifecycle of an OID4VP-style presentation request (verifier asks, holder submits once).
/// </summary>
public enum PresentationRequestStatus
{
    /// <summary>Created and waiting for the holder to submit a presentation.</summary>
    Pending = 0,

    /// <summary>Satisfied by exactly one submitted presentation (one-shot; replays rejected).</summary>
    Fulfilled = 1,

    /// <summary>The request lifetime elapsed before a valid submission.</summary>
    Expired = 2
}
