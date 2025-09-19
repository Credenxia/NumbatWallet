using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Web.Api.Rest.Common;

/// <summary>
/// Maps internal DTOs to DTP-compatible response formats
/// </summary>
public static class DtpResponseMapper
{
    /// <summary>
    /// Convert WalletDto to DTP wallet format
    /// </summary>
    public static object MapToDtpWallet(WalletDto wallet)
    {
        return new
        {
            walletId = wallet.Id,
            holderName = wallet.PersonName,
            walletName = wallet.Name,
            status = MapWalletStatus(wallet.Status),
            isActive = wallet.IsActive,
            isSuspended = wallet.IsSuspended,
            credentialCount = wallet.CredentialCount,
            metadata = wallet.Metadata,
            createdAt = wallet.CreatedAt.ToString("O"),
            updatedAt = wallet.UpdatedAt.ToString("O")
        };
    }

    /// <summary>
    /// Convert CredentialDto to DTP credential format
    /// </summary>
    public static object MapToDtpCredential(CredentialDto credential)
    {
        return new
        {
            credentialId = credential.Id,
            holderId = credential.HolderId,
            issuerId = credential.IssuerId,
            type = MapCredentialType(credential.Type),
            credentialSubject = credential.CredentialSubject,
            issuanceDate = credential.IssuanceDate.ToString("O"),
            expirationDate = credential.ExpirationDate?.ToString("O"),
            status = MapCredentialStatus(credential.Status),
            proof = credential.Proof,
            metadata = credential.Metadata,
            isRevoked = credential.IsRevoked,
            revocationDate = credential.RevocationDate?.ToString("O"),
            revocationReason = credential.RevocationReason
        };
    }

    /// <summary>
    /// Convert verification result to DTP format
    /// </summary>
    public static object MapToDtpVerificationResult(bool isValid, string credentialId, Dictionary<string, object>? claims = null)
    {
        return new
        {
            verificationResult = new
            {
                isValid,
                credentialId,
                verifiedAt = DateTime.UtcNow.ToString("O"),
                claims = claims ?? new Dictionary<string, object>(),
                verifier = "NumbatWallet DTP Adapter"
            }
        };
    }

    /// <summary>
    /// Map wallet status to DTP format
    /// </summary>
    private static string MapWalletStatus(string status)
    {
        return status.ToUpperInvariant() switch
        {
            "ACTIVE" => "ACTIVE",
            "SUSPENDED" => "SUSPENDED",
            "LOCKED" => "LOCKED",
            "ARCHIVED" => "ARCHIVED",
            _ => "UNKNOWN"
        };
    }

    /// <summary>
    /// Map credential type to DTP format
    /// </summary>
    private static string MapCredentialType(string type)
    {
        return type.ToUpperInvariant() switch
        {
            "DRIVERSLICENSE" => "DRIVERS_LICENSE",
            "PROOFOFAGE" => "PROOF_OF_AGE",
            "SENIORCARD" => "SENIOR_CARD",
            "STUDENTCARD" => "STUDENT_CARD",
            "WORKINGWITHCHILDREN" => "WORKING_WITH_CHILDREN",
            "PROOFOFIDENTITY" => "PROOF_OF_IDENTITY",
            "PROOFOFRESIDENCE" => "PROOF_OF_RESIDENCE",
            "DIGITALPASSPORT" => "DIGITAL_PASSPORT",
            _ => "CUSTOM"
        };
    }

    /// <summary>
    /// Map credential status to DTP format
    /// </summary>
    private static string MapCredentialStatus(string status)
    {
        return status.ToUpperInvariant() switch
        {
            "ACTIVE" => "ACTIVE",
            "EXPIRED" => "EXPIRED",
            "REVOKED" => "REVOKED",
            "SUSPENDED" => "SUSPENDED",
            "PENDING" => "PENDING",
            _ => "UNKNOWN"
        };
    }

    /// <summary>
    /// Convert error to DTP error format
    /// </summary>
    public static object MapToDtpError(string errorCode, string errorMessage, string? details = null)
    {
        return new
        {
            error = new
            {
                code = errorCode,
                message = errorMessage,
                details,
                timestamp = DateTime.UtcNow.ToString("O"),
                source = "NumbatWallet.DTP.Adapter"
            }
        };
    }
}