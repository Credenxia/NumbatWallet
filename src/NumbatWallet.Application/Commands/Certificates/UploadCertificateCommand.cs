using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Application.Commands.Certificates;

public record UploadCertificateCommand(
    Guid TenantId,
    string CertificateData,
    CertificatePurpose Purpose,
    string? PrivateKeyPassword = null
) : ICommand<UploadCertificateCommandResult>
{
    public string? RequestedBy { get; init; }
}

public class UploadCertificateCommandResult
{
    public Guid CertificateId { get; init; }
    public string Thumbprint { get; init; } = string.Empty;
    public string SubjectDn { get; init; } = string.Empty;
    public DateTimeOffset ValidFrom { get; init; }
    public DateTimeOffset ValidTo { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}