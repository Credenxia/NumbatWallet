using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Services;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.Services;

namespace NumbatWallet.Application.Commands.Certificates;

public class UploadCertificateCommandHandler : ICommandHandler<UploadCertificateCommand, UploadCertificateCommandResult>
{
    private readonly ITenantCertificateRepository _certificateRepository;
    private readonly ICertificateValidationService _validationService;
    private readonly ILogger<UploadCertificateCommandHandler> _logger;

    public UploadCertificateCommandHandler(
        ITenantCertificateRepository certificateRepository,
        ICertificateValidationService validationService,
        ILogger<UploadCertificateCommandHandler> logger)
    {
        _certificateRepository = certificateRepository;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<UploadCertificateCommandResult> HandleAsync(
        UploadCertificateCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse and validate the certificate
            byte[] certBytes = Convert.FromBase64String(command.CertificateData);
            using var x509Cert = X509CertificateLoader.LoadCertificate(certBytes);

            // Basic validation (expiry check)
            var validationResult = await _validationService.ValidateCertificateAsync(x509Cert);
            if (!validationResult.IsValid)
            {
                return new UploadCertificateCommandResult
                {
                    Success = false,
                    ErrorMessage = string.Join(", ", validationResult.Errors)
                };
            }

            // Check for duplicate
            var existing = await _certificateRepository.GetByThumbprintAsync(
                x509Cert.Thumbprint,
                cancellationToken);

            if (existing != null)
            {
                return new UploadCertificateCommandResult
                {
                    Success = false,
                    ErrorMessage = "Certificate with this thumbprint already exists"
                };
            }

            // Create domain entity
            var certificate = new TenantCertificate(
                command.TenantId,
                command.CertificateData,
                x509Cert.Thumbprint,
                x509Cert.SubjectName.Name,
                x509Cert.IssuerName.Name,
                new DateTimeOffset(x509Cert.NotBefore, TimeSpan.Zero),
                new DateTimeOffset(x509Cert.NotAfter, TimeSpan.Zero),
                command.Purpose);

            // Save to repository
            await _certificateRepository.AddAsync(certificate, cancellationToken);

            _logger.LogInformation(
                "Certificate uploaded successfully for tenant {TenantId}. Thumbprint: {Thumbprint}",
                command.TenantId, certificate.Thumbprint);

            return new UploadCertificateCommandResult
            {
                Success = true,
                CertificateId = certificate.Id,
                Thumbprint = certificate.Thumbprint,
                SubjectDn = certificate.SubjectDn,
                ValidFrom = certificate.ValidFrom,
                ValidTo = certificate.ValidTo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to upload certificate for tenant {TenantId}",
                command.TenantId);

            return new UploadCertificateCommandResult
            {
                Success = false,
                ErrorMessage = "Failed to upload certificate: " + ex.Message
            };
        }
    }
}
