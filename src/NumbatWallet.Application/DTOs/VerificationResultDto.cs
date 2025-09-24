namespace NumbatWallet.Application.DTOs;

public class VerificationResultDto
{
    public bool IsValid { get; set; }
    public VerificationChecksDto Checks { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Claims { get; set; } = new();
}

public class VerificationChecksDto
{
    public bool Signature { get; set; }
    public bool Expiry { get; set; }
    public bool Revocation { get; set; }
    public bool Schema { get; set; }
    public bool Issuer { get; set; }
}

public class VerificationOptionsDto
{
    public bool CheckRevocation { get; set; } = true;
    public bool CheckExpiry { get; set; } = true;
    public bool CheckSignature { get; set; } = true;
    public bool CheckSchema { get; set; } = true;
    public bool RequireTrustChain { get; set; } = false;
}
