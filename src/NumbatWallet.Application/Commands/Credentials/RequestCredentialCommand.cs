using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Commands.Credentials;

/// <summary>
/// Command to request a credential from an issuer
/// </summary>
public record RequestCredentialCommand(
    Guid WalletId,
    Guid IssuerId,
    string CredentialType,
    Dictionary<string, object> RequestedClaims,
    string? Justification = null) : ICommand<CredentialRequestDto>;

/// <summary>
/// Result DTO for credential request
/// </summary>
public class CredentialRequestDto
{
    public Guid RequestId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? IssuanceId { get; set; }
}