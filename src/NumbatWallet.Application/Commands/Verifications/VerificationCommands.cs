using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Commands.Verifications;

// Command to create a verification request
public record CreateVerificationRequestCommand(
    Guid TenantId,
    string RequestorDid,
    string[] RequestedAttributes,
    string Purpose,
    DateTime ExpiresAt,
    string? CallbackUrl,
    Dictionary<string, string>? Metadata) : ICommand<VerificationRequestDto>;

// Command to verify a credential
public record VerifyCredentialCommand(
    string CredentialData,
    string VerifierDid,
    Guid? VerificationRequestId,
    bool CheckRevocation,
    bool CheckExpiry,
    string[]? RequiredClaims) : ICommand<VerificationResultDto>;

// Command to create a verifiable presentation
public record CreatePresentationCommand(
    Guid TenantId,
    string HolderDid,
    Guid[] CredentialIds,
    Guid? VerificationRequestId,
    bool SelectiveDisclosure,
    string[]? DisclosedClaims,
    string? Nonce,
    DateTime? ExpiresAt) : ICommand<PresentationDto>;

// DTOs for verification
public class VerificationRequestDto
{
    public Guid Id { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string RequestorDid { get; set; } = string.Empty;
    public string[] RequestedAttributes { get; set; } = Array.Empty<string>();
    public string Purpose { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? QrCode { get; set; }
    public string? DeepLink { get; set; }
}

public class VerificationResultDto
{
    public bool IsValid { get; set; }
    public Guid? CredentialId { get; set; }
    public DateTime VerifiedAt { get; set; }
    public string VerifierDid { get; set; } = string.Empty;
    public VerificationCheck[] Checks { get; set; } = Array.Empty<VerificationCheck>();
    public Dictionary<string, object>? VerifiedClaims { get; set; }
}

public class VerificationCheck
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Message { get; set; }
}

public class PresentationDto
{
    public Guid Id { get; set; }
    public string PresentationData { get; set; } = string.Empty;
    public string HolderDid { get; set; } = string.Empty;
    public Guid[] CredentialIds { get; set; } = Array.Empty<Guid>();
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Proof { get; set; } = string.Empty;
    public Dictionary<string, object>? DisclosedClaims { get; set; }
}