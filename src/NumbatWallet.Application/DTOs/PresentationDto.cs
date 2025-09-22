namespace NumbatWallet.Application.DTOs;

public class PresentationDto
{
    public Guid PresentationId { get; set; }
    public Guid CredentialId { get; set; }
    public string VerifierDid { get; set; } = string.Empty;
    public List<string> DisclosedAttributes { get; set; } = new();
    public string ProofToken { get; set; } = string.Empty;
    public DateTime PresentedAt { get; set; }
}

public class CreatePresentationDto
{
    public required Guid CredentialId { get; set; }
    public required string VerifierDid { get; set; }
    public required List<string> DisclosedAttributes { get; set; }
    public string? Challenge { get; set; }
}
