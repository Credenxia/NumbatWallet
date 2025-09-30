using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Services;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Application.DomainServices;

namespace NumbatWallet.Application.Commands.Certificates;

public class RenewCertificateCommandHandler : ICommandHandler<RenewCertificateCommand, RenewCertificateCommandResult>
{
    private readonly ITenantCertificateRepository _certificateRepository;
    private readonly ICertificateValidationService _validationService;
    private readonly ILogger<RenewCertificateCommandHandler> _logger;

    public RenewCertificateCommandHandler(
        ITenantCertificateRepository certificateRepository,
        ICertificateValidationService validationService,
        ILogger<RenewCertificateCommandHandler> logger)
    {
        _certificateRepository = certificateRepository;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<RenewCertificateCommandResult> HandleAsync(
        RenewCertificateCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the existing certificate
            var oldCertificate = await _certificateRepository.GetByIdAsync(
                command.CertificateId,
                cancellationToken);

            if (oldCertificate == null)
            {
                return new RenewCertificateCommandResult
                {
                    Success = false,
                    Status = CertificateRenewalStatus.Failed,
                    ErrorMessage = "Certificate not found"
                };
            }

            // Check if certificate is eligible for renewal
            if (oldCertificate.IsRevoked)
            {
                return new RenewCertificateCommandResult
                {
                    Success = false,
                    Status = CertificateRenewalStatus.Failed,
                    ErrorMessage = "Cannot renew a revoked certificate"
                };
            }

            // Parse and validate the new certificate
            byte[] certBytes = Convert.FromBase64String(command.NewCertificateData);
            using var x509Cert = X509CertificateLoader.LoadCertificate(certBytes);

            // Validate the new certificate
            var validationResult = await _validationService.ValidateCertificateAsync(x509Cert);
            if (!validationResult.IsValid)
            {
                return new RenewCertificateCommandResult
                {
                    Success = false,
                    Status = CertificateRenewalStatus.Failed,
                    ErrorMessage = $"New certificate validation failed: {string.Join(", ", validationResult.Errors)}"
                };
            }

            // Verify the new certificate is for the same entity
            if (x509Cert.SubjectName.Name != oldCertificate.SubjectDn)
            {
                return new RenewCertificateCommandResult
                {
                    Success = false,
                    Status = CertificateRenewalStatus.Failed,
                    ErrorMessage = "New certificate subject does not match the original"
                };
            }

            // Create the new certificate entity
            var newCertificate = new TenantCertificate(
                oldCertificate.TenantId,
                command.NewCertificateData,
                x509Cert.Thumbprint,
                x509Cert.SubjectName.Name,
                x509Cert.IssuerName.Name,
                new DateTimeOffset(x509Cert.NotBefore, TimeSpan.Zero),
                new DateTimeOffset(x509Cert.NotAfter, TimeSpan.Zero),
                oldCertificate.Purpose);

            // Copy trust level from old certificate
            newCertificate.UpdateTrustLevel(oldCertificate.TrustLevel);

            // Save the new certificate
            await _certificateRepository.AddAsync(newCertificate, cancellationToken);

            // Handle auto-rotation
            if (command.AutoRotate)
            {
                // Calculate grace period end date
                var gracePeriodEnd = DateTimeOffset.UtcNow.AddDays(command.GracePeriodDays);

                // If old certificate expires before grace period, keep it active
                if (oldCertificate.ValidTo > DateTimeOffset.UtcNow)
                {
                    _logger.LogInformation(
                        "Certificate {OldCertId} will remain active until {ExpiryDate} during grace period",
                        oldCertificate.Id, oldCertificate.ValidTo);
                }
                else
                {
                    // Old certificate is expired or expiring soon, deactivate it
                    oldCertificate.Deactivate();
                    await _certificateRepository.UpdateAsync(oldCertificate, cancellationToken);
                }
            }
            else
            {
                // Manual rotation - deactivate old certificate immediately
                oldCertificate.Deactivate();
                await _certificateRepository.UpdateAsync(oldCertificate, cancellationToken);
            }

            _logger.LogInformation(
                "Certificate renewed successfully. Old: {OldId}, New: {NewId}",
                oldCertificate.Id, newCertificate.Id);

            return new RenewCertificateCommandResult
            {
                Success = true,
                Status = CertificateRenewalStatus.Completed,
                NewCertificateId = newCertificate.Id,
                OldCertificateId = oldCertificate.Id,
                RenewalDate = DateTimeOffset.UtcNow,
                OldCertificateExpiryDate = oldCertificate.ValidTo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to renew certificate {CertificateId}",
                command.CertificateId);

            return new RenewCertificateCommandResult
            {
                Success = false,
                Status = CertificateRenewalStatus.Failed,
                ErrorMessage = $"Certificate renewal failed: {ex.Message}"
            };
        }
    }
}
