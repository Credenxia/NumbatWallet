namespace NumbatWallet.Application.DTOs;

public class IdentityVerificationDto
{
    public string DocumentType { get; set; } = string.Empty; // e.g., "Passport", "DriverLicense"
    public string DocumentNumber { get; set; } = string.Empty;
    public string? DocumentImage { get; set; } // Base64 encoded image
    public string? SelfieImage { get; set; } // Base64 encoded selfie for biometric matching
    public Dictionary<string, string> AdditionalData { get; set; } = new();
}