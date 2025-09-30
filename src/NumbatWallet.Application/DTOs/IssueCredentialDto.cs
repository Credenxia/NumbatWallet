namespace NumbatWallet.Application.DTOs;

public class IssueCredentialDto
{
    public required Guid WalletId { get; set; }
    public required string Type { get; set; } // DRIVERS_LICENSE, PROOF_OF_AGE, etc.
    public required Dictionary<string, object> Data { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? IssuerId { get; set; }
    public string Schema { get; set; } = "https://www.w3.org/2018/credentials/v1";
}
