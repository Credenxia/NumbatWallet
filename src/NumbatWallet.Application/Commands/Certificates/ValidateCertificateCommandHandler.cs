using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Services;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Application.DomainServices;

namespace NumbatWallet.Application.Commands.Certificates;

public class ValidateCertificateCommandHandler : ICommandHandler<ValidateCertificateCommand, ValidateCertificateCommandResult>
{
    private readonly ITenantCertificateRepository _certificateRepository;
    private readonly ICertificateValidationService _validationService;
    private readonly ILogger<ValidateCertificateCommandHandler> _logger;

    public ValidateCertificateCommandHandler(
        ITenantCertificateRepository certificateRepository,
        ICertificateValidationService validationService,
        ILogger<ValidateCertificateCommandHandler> logger)
    {
        _certificateRepository = certificateRepository;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ValidateCertificateCommandResult> HandleAsync(
        ValidateCertificateCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get certificate from repository
            var certificate = await _certificateRepository.GetByIdAsync(
                command.CertificateId,
                cancellationToken);

            if (certificate == null)
            {
                return new ValidateCertificateCommandResult
                {
                    IsValid = false,
                    ValidationErrors = new List<string> { "Certificate not found" }
                };
            }

            // Parse X509 certificate
            var certBytes = Convert.FromBase64String(certificate.CertificateData);
            using var x509Cert = X509CertificateLoader.LoadCertificate(certBytes);

            // Perform validation
            var validationResult = await _validationService.ValidateCertificateAsync(x509Cert);

            var result = new ValidateCertificateCommandResult
            {
                IsValid = validationResult.IsValid && !certificate.IsRevoked && !certificate.IsExpired(),
                IsExpired = certificate.IsExpired(),
                IsRevoked = certificate.IsRevoked,
                ChainValid = validationResult.IsValid,
                ValidationErrors = validationResult.Errors.ToList(),
                ValidFrom = certificate.ValidFrom,
                ValidTo = certificate.ValidTo,
                Thumbprint = certificate.Thumbprint
            };

            // Check additional validation requirements
            if (command.CheckRevocation && !certificate.IsRevoked)
            {
                var revocationStatus = await _validationService.CheckRevocationStatusAsync(x509Cert);
                if (revocationStatus == Services.RevocationStatus.Revoked)
                {
                    result = result with
                    {
                        IsRevoked = true,
                        IsValid = false,
                        ValidationErrors = result.ValidationErrors.Concat(new[] { "Certificate has been revoked" }).ToList()
                    };

                    // Update certificate status in database
                    certificate.Revoke("Certificate revoked by CRL/OCSP check");
                    await _certificateRepository.UpdateAsync(certificate, cancellationToken);
                }
            }

            if (command.ValidateChain)
            {
                var chainResult = await _validationService.ValidateChainAsync(x509Cert);
                if (!chainResult.IsValid)
                {
                    result = result with
                    {
                        ChainValid = false,
                        IsValid = false,
                        ValidationErrors = result.ValidationErrors.Concat(chainResult.Errors).ToList()
                    };
                }
            }

            _logger.LogInformation(
                "Certificate validation completed for {CertificateId}. Valid: {IsValid}",
                command.CertificateId, result.IsValid);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to validate certificate {CertificateId}",
                command.CertificateId);

            return new ValidateCertificateCommandResult
            {
                IsValid = false,
                ValidationErrors = new List<string> { "Validation failed: " + ex.Message }
            };
        }
    }
}
