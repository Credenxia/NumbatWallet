using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Commands.Certificates;

public class RevokeCertificateCommandHandler : ICommandHandler<RevokeCertificateCommand, RevokeCertificateCommandResult>
{
    private readonly ITenantCertificateRepository _certificateRepository;
    private readonly ILogger<RevokeCertificateCommandHandler> _logger;

    public RevokeCertificateCommandHandler(
        ITenantCertificateRepository certificateRepository,
        ILogger<RevokeCertificateCommandHandler> logger)
    {
        _certificateRepository = certificateRepository;
        _logger = logger;
    }

    public async Task<RevokeCertificateCommandResult> HandleAsync(
        RevokeCertificateCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get certificate
            var certificate = await _certificateRepository.GetByIdAsync(
                command.CertificateId,
                cancellationToken);

            if (certificate == null)
            {
                return new RevokeCertificateCommandResult
                {
                    Success = false,
                    ErrorMessage = "Certificate not found"
                };
            }

            if (certificate.IsRevoked)
            {
                return new RevokeCertificateCommandResult
                {
                    Success = false,
                    ErrorMessage = "Certificate is already revoked"
                };
            }

            var revokedCount = 1;

            // Revoke the certificate
            certificate.Revoke(command.Reason);
            await _certificateRepository.UpdateAsync(certificate, cancellationToken);

            // Optionally revoke related certificates (e.g., from same issuer)
            if (command.RevokeRelatedCertificates && !string.IsNullOrEmpty(certificate.SubjectDn))
            {
                var relatedCerts = await _certificateRepository.GetBySubjectDnAsync(
                    certificate.SubjectDn,
                    cancellationToken);

                foreach (var related in relatedCerts.Where(c => !c.IsRevoked && c.Id != certificate.Id))
                {
                    related.Revoke($"Related to revoked certificate: {command.Reason}");
                    await _certificateRepository.UpdateAsync(related, cancellationToken);
                    revokedCount++;
                }
            }

            _logger.LogInformation(
                "Certificate {CertificateId} revoked. Reason: {Reason}. Total revoked: {Count}",
                command.CertificateId, command.Reason, revokedCount);

            return new RevokeCertificateCommandResult
            {
                Success = true,
                CertificatesRevoked = revokedCount,
                RevokedAt = certificate.RevokedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to revoke certificate {CertificateId}",
                command.CertificateId);

            return new RevokeCertificateCommandResult
            {
                Success = false,
                ErrorMessage = "Failed to revoke certificate: " + ex.Message
            };
        }
    }
}
