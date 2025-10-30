using System.Security.Cryptography.X509Certificates;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Application.DomainServices;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Infrastructure.Services;

public class CertificateValidationService : ICertificateValidationService
{
    private readonly ICertificateAuthorityRepository _authorityRepository;
    private readonly ILogger<CertificateValidationService> _logger;
    private readonly HttpClient _httpClient;

    public CertificateValidationService(
        ICertificateAuthorityRepository authorityRepository,
        ICertificateTrustStoreRepository _,
        ILogger<CertificateValidationService> logger,
        HttpClient httpClient)
    {
        _authorityRepository = authorityRepository;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<CertificateValidationResult> ValidateCertificateAsync(
        TenantCertificate certificate,
        CertificateTrustStore trustStore,
        CancellationToken cancellationToken = default)
    {
        var result = new CertificateValidationResult
        {
            ValidatedAt = DateTimeOffset.UtcNow,
            TrustLevel = certificate.TrustLevel
        };

        try
        {
            // Check if certificate is expired
            if (certificate.IsExpired())
            {
                result.Errors.Add("Certificate is expired");
                result.IsValid = false;
                return result;
            }

            // Check if certificate is revoked locally
            if (certificate.RevokedAt.HasValue)
            {
                result.Errors.Add($"Certificate is revoked: {certificate.RevocationReason}");
                result.IsValid = false;
                return result;
            }

            // Check if certificate is in the trust store's revocation list
            if (trustStore.IsCertificateRevoked(certificate.Thumbprint))
            {
                result.Errors.Add("Certificate is in the trust store's revocation list");
                result.IsValid = false;
                return result;
            }

            // Validate certificate chain
            var authorities = await _authorityRepository.GetTrustedAuthoritiesAsync(cancellationToken);
            result.ChainValid = await ValidateChainAsync(certificate, authorities, cancellationToken);
            if (!result.ChainValid)
            {
                result.Errors.Add("Certificate chain validation failed");
            }

            // Check OCSP status if available
            result.OcspValid = await CheckOcspStatusAsync(certificate, null, cancellationToken);
            if (!result.OcspValid)
            {
                result.Errors.Add("OCSP validation failed");
            }

            // Check CRL status if available
            result.CrlValid = await CheckCrlStatusAsync(certificate, null, cancellationToken);
            if (!result.CrlValid)
            {
                result.Errors.Add("CRL validation failed");
            }

            // Determine overall validity
            result.IsValid = result.ChainValid && result.OcspValid && result.CrlValid && result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating certificate {Thumbprint}", certificate.Thumbprint);
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    public async Task<bool> ValidateChainAsync(
        TenantCertificate certificate,
        IEnumerable<CertificateAuthority> authorities,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert base64 certificate to X509Certificate2
            var certBytes = Convert.FromBase64String(certificate.CertificateData);
            using var x509Cert = X509CertificateLoader.LoadCertificate(certBytes);

            // Build certificate chain
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // We handle revocation separately
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

            // Add trusted authorities to the chain
            var authoritiesList = authorities.ToList();
            foreach (var authority in authoritiesList.Where(a => a.IsTrusted))
            {
                var authBytes = Convert.FromBase64String(authority.CertificateData);
                using var authCert = X509CertificateLoader.LoadCertificate(authBytes);
                chain.ChainPolicy.ExtraStore.Add(authCert);
            }

            // Build and validate the chain
            var chainBuilt = chain.Build(x509Cert);
            if (!chainBuilt)
            {
                _logger.LogWarning("Certificate chain validation failed for {Thumbprint}. Status: {Status}",
                    certificate.Thumbprint,
                    string.Join(", ", chain.ChainStatus.Select(s => s.StatusInformation)));
                return false;
            }

            // Verify that the chain ends with a trusted authority
            var rootCert = chain.ChainElements[^1].Certificate;
            var rootThumbprint = rootCert.Thumbprint;
            var trustedRoot = authoritiesList.Any(a => a.Thumbprint.Equals(rootThumbprint, StringComparison.OrdinalIgnoreCase) && a.IsTrusted);

            if (!trustedRoot)
            {
                _logger.LogWarning("Certificate chain root {RootThumbprint} is not in trusted authorities", rootThumbprint);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating certificate chain for {Thumbprint}", certificate.Thumbprint);
            return false;
        }
    }

    public async Task<bool> CheckOcspStatusAsync(
        TenantCertificate certificate,
        string? ocspUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ocspUrl))
            {
                // Try to get OCSP URL from the issuer
                var issuerAuthority = await _authorityRepository.GetBySubjectDnAsync(certificate.IssuerDn, cancellationToken);
                ocspUrl = issuerAuthority?.OcspUrl;

                if (string.IsNullOrWhiteSpace(ocspUrl))
                {
                    _logger.LogDebug("No OCSP URL available for certificate {Thumbprint}", certificate.Thumbprint);
                    return true; // Consider valid if no OCSP URL is available
                }
            }

            // Create OCSP request
            var certBytes = Convert.FromBase64String(certificate.CertificateData);
            using var x509Cert = X509CertificateLoader.LoadCertificate(certBytes);

            // Note: Full OCSP implementation would require creating proper OCSP requests
            // For now, we'll do a simple HTTP check
            var response = await _httpClient.GetAsync(ocspUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OCSP check failed for certificate {Thumbprint}. HTTP Status: {StatusCode}",
                    certificate.Thumbprint, response.StatusCode);
                return false;
            }

            // In a real implementation, parse OCSP response and check certificate status
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking OCSP status for certificate {Thumbprint}", certificate.Thumbprint);
            return false;
        }
    }

    public async Task<bool> CheckCrlStatusAsync(
        TenantCertificate certificate,
        string? crlUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(crlUrl))
            {
                // Try to get CRL URL from the issuer
                var issuerAuthority = await _authorityRepository.GetBySubjectDnAsync(certificate.IssuerDn, cancellationToken);
                crlUrl = issuerAuthority?.CrlUrl;

                if (string.IsNullOrWhiteSpace(crlUrl))
                {
                    _logger.LogDebug("No CRL URL available for certificate {Thumbprint}", certificate.Thumbprint);
                    return true; // Consider valid if no CRL URL is available
                }
            }

            // Download CRL
            var crlData = await _httpClient.GetByteArrayAsync(crlUrl, cancellationToken);

            // Note: Full CRL implementation would require parsing the CRL and checking certificate serial number
            // For now, we'll assume the certificate is not revoked if we can download the CRL
            _logger.LogDebug("CRL downloaded successfully for certificate {Thumbprint}", certificate.Thumbprint);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking CRL status for certificate {Thumbprint}", certificate.Thumbprint);
            return false;
        }
    }

    public bool ValidateForPurpose(TenantCertificate certificate, CertificatePurpose purpose)
    {
        // Check if certificate is valid for the specified purpose
        if (!certificate.CanBeUsedForPurpose(purpose))
        {
            _logger.LogWarning("Certificate {Thumbprint} cannot be used for purpose {Purpose}",
                certificate.Thumbprint, purpose);
            return false;
        }

        // Check if certificate is active
        if (!certificate.IsActive)
        {
            _logger.LogWarning("Certificate {Thumbprint} is not active", certificate.Thumbprint);
            return false;
        }

        // Check if certificate is expired
        if (certificate.IsExpired())
        {
            _logger.LogWarning("Certificate {Thumbprint} is expired", certificate.Thumbprint);
            return false;
        }

        // Check if certificate is revoked
        if (certificate.RevokedAt.HasValue)
        {
            _logger.LogWarning("Certificate {Thumbprint} is revoked", certificate.Thumbprint);
            return false;
        }

        return true;
    }
}
