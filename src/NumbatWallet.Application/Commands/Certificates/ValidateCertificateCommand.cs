using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Commands.Certificates;

public record ValidateCertificateCommand(
    Guid CertificateId,
    bool CheckRevocation = true,
    bool ValidateChain = true
) : ICommand<ValidateCertificateCommandResult>;

public record ValidateCertificateCommandResult
{
    public bool IsValid { get; init; }
    public bool IsExpired { get; init; }
    public bool IsRevoked { get; init; }
    public bool ChainValid { get; init; }
    public List<string> ValidationErrors { get; init; } = new();
    public DateTimeOffset? ValidFrom { get; init; }
    public DateTimeOffset? ValidTo { get; init; }
    public string? Thumbprint { get; init; }
}