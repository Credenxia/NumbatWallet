using System.Security.Cryptography.X509Certificates;
using NumbatWallet.Application.DomainServices;

namespace NumbatWallet.Application.Services;

/// <summary>
/// Extension methods for certificate validation
/// </summary>
public static class CertificateValidationExtensions
{
    public static Task<CertificateValidationResult> ValidateCertificateAsync(
        this ICertificateValidationService service,
        X509Certificate2 certificate)
    {
        // Create a simple validation result based on X509 chain validation
        var result = new CertificateValidationResult
        {
            ValidatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            // Check if certificate is expired
            if (certificate.NotAfter < DateTime.UtcNow)
            {
                result.Errors.Add("Certificate is expired");
                result.IsValid = false;
                return Task.FromResult(result);
            }

            if (certificate.NotBefore > DateTime.UtcNow)
            {
                result.Errors.Add("Certificate is not yet valid");
                result.IsValid = false;
                return Task.FromResult(result);
            }

            // Build and validate chain
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

            bool chainValid = chain.Build(certificate);
            result.ChainValid = chainValid;

            if (!chainValid)
            {
                foreach (var element in chain.ChainElements)
                {
                    foreach (var status in element.ChainElementStatus)
                    {
                        if (status.Status != X509ChainStatusFlags.NoError)
                        {
                            result.Errors.Add($"Chain validation error: {status.StatusInformation}");
                        }
                    }
                }
            }

            result.IsValid = chainValid && result.Errors.Count == 0;
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
            return Task.FromResult(result);
        }
    }

    public static Task<CertificateValidationResult> ValidateChainAsync(
        this ICertificateValidationService service,
        X509Certificate2 certificate)
    {
        return service.ValidateCertificateAsync(certificate);
    }

    public static Task<RevocationStatus> CheckRevocationStatusAsync(
        this ICertificateValidationService service,
        X509Certificate2 certificate)
    {
        try
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;

            if (!chain.Build(certificate))
            {
                foreach (var element in chain.ChainElements)
                {
                    foreach (var status in element.ChainElementStatus)
                    {
                        if (status.Status == X509ChainStatusFlags.Revoked)
                        {
                            return Task.FromResult(RevocationStatus.Revoked);
                        }
                        if (status.Status == X509ChainStatusFlags.RevocationStatusUnknown)
                        {
                            return Task.FromResult(RevocationStatus.Unknown);
                        }
                    }
                }
            }

            return Task.FromResult(RevocationStatus.Good);
        }
        catch
        {
            return Task.FromResult(RevocationStatus.Unknown);
        }
    }
}

public enum RevocationStatus
{
    Good,
    Revoked,
    Unknown
}
