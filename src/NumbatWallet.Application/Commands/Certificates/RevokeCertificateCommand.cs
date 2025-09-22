using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Commands.Certificates;

public record RevokeCertificateCommand(
    Guid CertificateId,
    string Reason,
    bool RevokeRelatedCertificates = false
) : ICommand<RevokeCertificateCommandResult>
{
    public string? RevokedBy { get; init; }
}

public class RevokeCertificateCommandResult
{
    public bool Success { get; init; }
    public int CertificatesRevoked { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
