namespace NumbatWallet.SharedKernel.Enums;

/// <summary>
/// Types of Personally Identifiable Information (PII)
/// </summary>
public enum PiiType
{
    /// <summary>
    /// Not PII
    /// </summary>
    None = 0,

    /// <summary>
    /// Email address
    /// </summary>
    Email,

    /// <summary>
    /// Phone number
    /// </summary>
    Phone,

    /// <summary>
    /// Person's name (first, last, or full)
    /// </summary>
    Name,

    /// <summary>
    /// Physical or postal address
    /// </summary>
    Address,

    /// <summary>
    /// Date of birth
    /// </summary>
    DateOfBirth,

    /// <summary>
    /// Tax file number, SSN, or similar
    /// </summary>
    TaxId,

    /// <summary>
    /// Payment card number
    /// </summary>
    PaymentCard,

    /// <summary>
    /// Bank account number
    /// </summary>
    AccountNumber,

    /// <summary>
    /// IP address
    /// </summary>
    IpAddress,

    /// <summary>
    /// Biometric data (fingerprints, face, etc.)
    /// </summary>
    BiometricData,

    /// <summary>
    /// Health or medical data
    /// </summary>
    HealthData,

    /// <summary>
    /// Government-issued ID (passport, driver's license, etc.)
    /// </summary>
    GovernmentId,

    /// <summary>
    /// National ID or similar identifier
    /// </summary>
    NationalId,

    /// <summary>
    /// Vehicle registration number
    /// </summary>
    VehicleRegistration
}