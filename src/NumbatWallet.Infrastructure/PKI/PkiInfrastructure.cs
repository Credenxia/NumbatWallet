using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Infrastructure.PKI;

/// <summary>
/// PKI Infrastructure configuration and services
/// POA: Phase 3 - PKI setup
/// </summary>
public static class PkiInfrastructure
{
    public static IServiceCollection AddPkiInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ICertificateAuthority, CertificateAuthority>();
        services.AddScoped<ICertificateService, CertificateService>();
        services.AddScoped<ITrustListService, TrustListService>();
        services.AddScoped<IOcspService, OcspService>();

        // Configure certificate stores
        var pkiConfig = configuration.GetSection("PKI");
        services.Configure<PkiOptions>(pkiConfig);

        return services;
    }
}

public class PkiOptions
{
    public string RootCertPath { get; set; } = string.Empty;
    public string IntermediateCertPath { get; set; } = string.Empty;
    public string TrustListUrl { get; set; } = "https://trust.numbatwallet.wa.gov.au/list.json";
    public string OcspUrl { get; set; } = "https://ocsp.numbatwallet.wa.gov.au";
    public bool ValidateChain { get; set; } = true;
    public bool CheckRevocation { get; set; } = true;
}

/// <summary>
/// Certificate Authority service
/// </summary>
public interface ICertificateAuthority
{
    Task<X509Certificate2> GetRootCertificateAsync(CancellationToken cancellationToken = default);
    Task<X509Certificate2> GetIntermediateCertificateAsync(CancellationToken cancellationToken = default);
    Task<X509Certificate2> IssueCredentialCertificateAsync(string subjectName, CancellationToken cancellationToken = default);
}

public class CertificateAuthority : ICertificateAuthority, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CertificateAuthority> _logger;
    private X509Certificate2? _rootCert;
    private X509Certificate2? _intermediateCert;
    private bool _disposed;

    public CertificateAuthority(
        IConfiguration configuration,
        ILogger<CertificateAuthority> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<X509Certificate2> GetRootCertificateAsync(CancellationToken cancellationToken = default)
    {
        if (_rootCert == null)
        {
            var certPath = _configuration["PKI:RootCertPath"];
            if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
            {
                _rootCert = new X509Certificate2(certPath);
            }
            else
            {
                // Generate self-signed root for POA
                _rootCert = GenerateRootCertificate();
            }
        }

        return Task.FromResult(_rootCert);
    }

    public Task<X509Certificate2> GetIntermediateCertificateAsync(CancellationToken cancellationToken = default)
    {
        if (_intermediateCert == null)
        {
            var certPath = _configuration["PKI:IntermediateCertPath"];
            if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
            {
                _intermediateCert = new X509Certificate2(certPath);
            }
            else
            {
                // Generate intermediate certificate for POA
                var rootCert = GetRootCertificateAsync(cancellationToken).Result;
                _intermediateCert = GenerateIntermediateCertificate(rootCert);
            }
        }

        return Task.FromResult(_intermediateCert);
    }

    public async Task<X509Certificate2> IssueCredentialCertificateAsync(
        string subjectName,
        CancellationToken cancellationToken = default)
    {
        var intermediateCert = await GetIntermediateCertificateAsync(cancellationToken);
        var cert = GenerateCredentialCertificate(subjectName, intermediateCert);

        _logger.LogInformation("Issued credential certificate for {Subject}", subjectName);
        return cert;
    }

    private X509Certificate2 GenerateRootCertificate()
    {
        using var rsa = RSA.Create(4096);
        var request = new CertificateRequest(
            "CN=NumbatWallet Root CA, O=Government of Western Australia, C=AU",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, true, 2, true));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                true));

        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(10));

        _logger.LogInformation("Generated self-signed root certificate");
        return new X509Certificate2(cert.Export(X509ContentType.Pfx));
    }

    private X509Certificate2 GenerateIntermediateCertificate(X509Certificate2 rootCert)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=NumbatWallet Intermediate CA, O=Government of Western Australia, C=AU",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, true, 1, true));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature,
                true));

        var serialNumber = new byte[16];
        RandomNumberGenerator.Fill(serialNumber);

        using var rootKey = rootCert.GetRSAPrivateKey();
        var cert = request.Create(
            rootCert.SubjectName,
            X509SignatureGenerator.CreateForRSA(rootKey!, RSASignaturePadding.Pkcs1),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(5),
            serialNumber);

        var certWithKey = cert.CopyWithPrivateKey(rsa);

        _logger.LogInformation("Generated intermediate certificate");
        return new X509Certificate2(certWithKey.Export(X509ContentType.Pfx));
    }

    private X509Certificate2 GenerateCredentialCertificate(
        string subjectName,
        X509Certificate2 issuerCert)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={subjectName}, O=NumbatWallet User, C=AU",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
                true));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    new Oid("1.3.6.1.5.5.7.3.2"), // Client Authentication
                    new Oid("1.3.6.1.4.1.311.10.3.12") // Document Signing
                },
                false));

        var serialNumber = new byte[16];
        RandomNumberGenerator.Fill(serialNumber);

        using var issuerKey = issuerCert.GetRSAPrivateKey();
        var cert = request.Create(
            issuerCert.SubjectName,
            X509SignatureGenerator.CreateForRSA(issuerKey!, RSASignaturePadding.Pkcs1),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(1),
            serialNumber);

        var certWithKey = cert.CopyWithPrivateKey(rsa);
        return new X509Certificate2(certWithKey.Export(X509ContentType.Pfx));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _rootCert?.Dispose();
                _intermediateCert?.Dispose();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Certificate service for validation and management
/// </summary>
public interface ICertificateService
{
    Task<bool> ValidateCertificateAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default);
    Task<bool> IsRevokedAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default);
    Task RevokeCertificateAsync(string serialNumber, string reason, CancellationToken cancellationToken = default);
}

public class CertificateService : ICertificateService
{
    private readonly ICertificateAuthority _ca;
    private readonly ILogger<CertificateService> _logger;
    private readonly HashSet<string> _revokedCertificates = new();

    public CertificateService(
        ICertificateAuthority ca,
        ILogger<CertificateService> logger)
    {
        _ca = ca;
        _logger = logger;
    }

    public async Task<bool> ValidateCertificateAsync(
        X509Certificate2 certificate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check expiration
            if (certificate.NotAfter < DateTime.UtcNow)
            {
                _logger.LogWarning("Certificate {Subject} has expired", certificate.Subject);
                return false;
            }

            // Check if revoked
            if (await IsRevokedAsync(certificate, cancellationToken))
            {
                _logger.LogWarning("Certificate {Subject} is revoked", certificate.Subject);
                return false;
            }

            // Build and validate chain
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // For POA
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

            var rootCert = await _ca.GetRootCertificateAsync(cancellationToken);
            chain.ChainPolicy.ExtraStore.Add(rootCert);

            var intermediateCert = await _ca.GetIntermediateCertificateAsync(cancellationToken);
            chain.ChainPolicy.ExtraStore.Add(intermediateCert);

            var isValid = chain.Build(certificate);

            if (!isValid)
            {
                foreach (var status in chain.ChainStatus)
                {
                    _logger.LogWarning("Chain validation issue: {Status} - {Info}",
                        status.Status, status.StatusInformation);
                }
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating certificate");
            return false;
        }
    }

    public Task<bool> IsRevokedAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default)
    {
        var isRevoked = _revokedCertificates.Contains(certificate.SerialNumber ?? "");
        return Task.FromResult(isRevoked);
    }

    public Task RevokeCertificateAsync(
        string serialNumber,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _revokedCertificates.Add(serialNumber);
        _logger.LogInformation("Certificate {SerialNumber} revoked: {Reason}", serialNumber, reason);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Trust list service for managing trusted issuers
/// </summary>
public interface ITrustListService
{
    Task<IEnumerable<string>> GetTrustedIssuersAsync(CancellationToken cancellationToken = default);
    Task<bool> IsTrustedIssuerAsync(string issuerDid, CancellationToken cancellationToken = default);
    Task AddTrustedIssuerAsync(string issuerDid, CancellationToken cancellationToken = default);
}

public class TrustListService : ITrustListService
{
    private readonly HashSet<string> _trustedIssuers = new()
    {
        "did:web:wa.gov.au",
        "did:web:numbatwallet.wa.gov.au",
        "did:web:transport.wa.gov.au"
    };

    private readonly ILogger<TrustListService> _logger;

    public TrustListService(ILogger<TrustListService> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<string>> GetTrustedIssuersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_trustedIssuers.AsEnumerable());
    }

    public Task<bool> IsTrustedIssuerAsync(string issuerDid, CancellationToken cancellationToken = default)
    {
        var isTrusted = _trustedIssuers.Contains(issuerDid);
        _logger.LogInformation("Issuer {Issuer} trust status: {IsTrusted}", issuerDid, isTrusted);
        return Task.FromResult(isTrusted);
    }

    public Task AddTrustedIssuerAsync(string issuerDid, CancellationToken cancellationToken = default)
    {
        _trustedIssuers.Add(issuerDid);
        _logger.LogInformation("Added trusted issuer: {Issuer}", issuerDid);
        return Task.CompletedTask;
    }
}

/// <summary>
/// OCSP service for certificate status checking
/// </summary>
public interface IOcspService
{
    Task<CertificateStatus> CheckStatusAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default);
}

public class OcspService : IOcspService
{
    private readonly ILogger<OcspService> _logger;

    public OcspService(ILogger<OcspService> logger)
    {
        _logger = logger;
    }

    public Task<CertificateStatus> CheckStatusAsync(
        X509Certificate2 certificate,
        CancellationToken cancellationToken = default)
    {
        // Mock OCSP check for POA
        _logger.LogInformation("OCSP check for certificate {Subject}", certificate.Subject);
        return Task.FromResult(CertificateStatus.Good);
    }
}

public enum CertificateStatus
{
    Good,
    Revoked,
    Unknown,
    ServerError
}