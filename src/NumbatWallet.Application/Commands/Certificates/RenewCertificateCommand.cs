using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Commands.Certificates;

public record RenewCertificateCommand(
    Guid CertificateId,
    string NewCertificateData,
    bool AutoRotate = true,
    int GracePeriodDays = 7
) : ICommand<RenewCertificateCommandResult>
{
    public string? RenewedBy { get; init; }
}

public record RenewCertificateCommandResult
{
    public bool Success { get; init; }
    public Guid? NewCertificateId { get; init; }
    public Guid? OldCertificateId { get; init; }
    public DateTimeOffset? RenewalDate { get; init; }
    public DateTimeOffset? OldCertificateExpiryDate { get; init; }
    public string? ErrorMessage { get; init; }
    public CertificateRenewalStatus Status { get; init; }
}

public enum CertificateRenewalStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    RollbackRequired
}