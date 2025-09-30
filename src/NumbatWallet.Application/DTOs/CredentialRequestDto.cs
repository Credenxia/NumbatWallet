namespace NumbatWallet.Application.DTOs;

/// <summary>
/// Request DTOs for credential operations
/// </summary>
public class IssueCredentialRequestDto
{
    public Guid WalletId { get; set; }
    public Guid IssuerId { get; set; }
    public string CredentialType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public Dictionary<string, object> Claims { get; set; } = new();
    public DateTime? ExpiryDate { get; set; }
    public string? CredentialSchema { get; set; }
    public string? CredentialContext { get; set; }
    public bool IsRevocable { get; set; } = true;
}

public class VerifyCredentialRequestDto
{
    public string Credential { get; set; } = string.Empty;
    public string CredentialData { get; set; } = string.Empty;
    public string? CredentialFormat { get; set; }
    public bool CheckRevocation { get; set; } = true;
    public bool CheckExpiry { get; set; } = true;
    public bool CheckSignature { get; set; } = true;
    public string? ExpectedIssuer { get; set; }
    public VerificationOptionsDto? VerificationOptions { get; set; }
}

public class ShareCredentialRequestDto
{
    public Guid CredentialId { get; set; }
    public string RecipientId { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public SharePurpose Purpose { get; set; }
    public TimeSpan? AccessDuration { get; set; }
    public int ExpiryHours { get; set; } = 24;
    public bool RequireAuthentication { get; set; } = true;
    public string? SelectiveDisclosure { get; set; }
}

public enum SharePurpose
{
    Verification,
    Employment,
    Education,
    Financial,
    Healthcare,
    Government,
    Other
}

public class ShareCredentialResultDto
{
    public bool IsSuccess { get; set; }
    public string? ShareToken { get; set; }
    public string? ShareUrl { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RevokeCredentialRequestDto
{
    public required string Reason { get; set; }
}

public class RequestCredentialDto
{
    public Guid WalletId { get; set; }
    public Guid IssuerId { get; set; }
    public required string CredentialType { get; set; }
    public Dictionary<string, object>? RequestedClaims { get; set; }
    public string? Justification { get; set; }
}

public class CredentialRequestResponseDto
{
    public Guid RequestId { get; set; }
    public required string Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? Message { get; set; }
}

// Extension methods for DTO conversions
public static class VerificationOptionsDtoExtensions
{
    public static Dictionary<string, object> ToDictionary(this VerificationOptionsDto dto)
    {
        return new Dictionary<string, object>
        {
            ["checkRevocation"] = dto.CheckRevocation,
            ["checkExpiry"] = dto.CheckExpiry,
            ["checkSignature"] = dto.CheckSignature,
            ["checkSchema"] = dto.CheckSchema,
            ["requireTrustChain"] = dto.RequireTrustChain
        };
    }
}