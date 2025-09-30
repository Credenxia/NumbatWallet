using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Infrastructure.Services.Mocks;

/// <summary>
/// Mock ICAO PKD (Public Key Directory) service for development and testing
/// Simulates CSCA certificates and Master List functionality
/// POA-125: Set up IACA root certificates (Mock implementation)
/// </summary>
public class MockIcaoService : IIcaoService
{
    private readonly ILogger<MockIcaoService> _logger;
    private readonly Dictionary<string, X509Certificate2> _cscaCertificates;
    private readonly Dictionary<string, X509Certificate2> _dscCertificates;
    private readonly List<MasterListEntry> _masterList;
    private X509Certificate2? _masterListSigningCert;

    public MockIcaoService(ILogger<MockIcaoService> logger)
    {
        _logger = logger;
        _cscaCertificates = new Dictionary<string, X509Certificate2>();
        _dscCertificates = new Dictionary<string, X509Certificate2>();
        _masterList = new List<MasterListEntry>();

        InitializeMockCertificates();
        _logger.LogInformation("Mock ICAO Service initialized with test certificates");
    }

    public async Task<IEnumerable<X509Certificate2>> GetCscaCertificatesAsync(
        string? countryCode = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken); // Simulate network delay

        if (string.IsNullOrEmpty(countryCode))
        {
            _logger.LogInformation("Returning all CSCA certificates");
            return _cscaCertificates.Values.ToList();
        }

        _logger.LogInformation("Returning CSCA certificates for country: {Country}", countryCode);
        return _cscaCertificates
            .Where(kvp => kvp.Key.StartsWith(countryCode, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Value)
            .ToList();
    }

    public async Task<X509Certificate2?> GetDocumentSignerCertificateAsync(
        string issuerId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate network delay

        if (_dscCertificates.TryGetValue(issuerId, out var cert))
        {
            _logger.LogInformation("Found DSC for issuer: {IssuerId}", issuerId);
            return cert;
        }

        _logger.LogWarning("DSC not found for issuer: {IssuerId}", issuerId);
        return null;
    }

    public async Task<MasterList> GetMasterListAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken); // Simulate network delay

        _logger.LogInformation("Returning master list with {Count} entries", _masterList.Count);

        return new MasterList
        {
            Version = "1.0.0",
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            NextUpdate = DateTime.UtcNow.AddDays(29),
            Entries = _masterList,
            Signature = GenerateMasterListSignature()
        };
    }

    public async Task<bool> ValidateCertificateChainAsync(
        X509Certificate2 certificate,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate validation delay

        try
        {
            // Find the issuer CSCA
            var issuerName = certificate.Issuer;
            var csca = _cscaCertificates.Values.FirstOrDefault(c => c.Subject == issuerName);

            if (csca == null)
            {
                _logger.LogWarning("CSCA not found for issuer: {Issuer}", issuerName);
                return false;
            }

            // Build chain (simplified for mock)
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // Mock doesn't check CRL
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            chain.ChainPolicy.ExtraStore.Add(csca);

            var isValid = chain.Build(certificate);

            _logger.LogInformation("Certificate chain validation result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating certificate chain");
            return false;
        }
    }

    public async Task<bool> VerifyMasterListSignatureAsync(
        byte[] masterListData,
        byte[] signature,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(30, cancellationToken); // Simulate verification delay

        if (_masterListSigningCert == null)
        {
            _logger.LogWarning("Master list signing certificate not available");
            return false;
        }

        try
        {
            using var rsa = _masterListSigningCert.GetRSAPublicKey();
            if (rsa == null)
            {
                return false;
            }

            var isValid = rsa.VerifyData(
                masterListData,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            _logger.LogInformation("Master list signature verification: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying master list signature");
            return false;
        }
    }

    public async Task<CrlInfo> GetCrlAsync(
        string crlDistributionPoint,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken); // Simulate network delay

        _logger.LogInformation("Returning mock CRL for: {CrlDP}", crlDistributionPoint);

        // Return mock CRL info
        return new CrlInfo
        {
            DistributionPoint = crlDistributionPoint,
            ThisUpdate = DateTime.UtcNow.AddDays(-7),
            NextUpdate = DateTime.UtcNow.AddDays(7),
            RevokedCertificates = GenerateMockRevokedCertificates(),
            IssuerName = "CN=Mock CSCA, C=AU"
        };
    }

    public async Task<bool> ImportCscaCertificateAsync(
        byte[] certificateData,
        string countryCode,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);

        try
        {
            var cert = X509CertificateLoader.LoadCertificate(certificateData);
            var key = $"{countryCode}_{cert.Thumbprint}";

            _cscaCertificates[key] = cert;
            _masterList.Add(new MasterListEntry
            {
                CountryCode = countryCode,
                CertificateThumbprint = cert.Thumbprint,
                Subject = cert.Subject,
                ValidFrom = cert.NotBefore,
                ValidTo = cert.NotAfter,
                KeyUsage = "Certificate Sign, CRL Sign"
            });

            _logger.LogInformation("Imported CSCA certificate for {Country}: {Subject}",
                countryCode, cert.Subject);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import CSCA certificate");
            return false;
        }
    }

    public async Task<DocumentVerificationResult> VerifyDocumentAsync(
        byte[] documentData,
        byte[] signature,
        string issuerId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        var result = new DocumentVerificationResult
        {
            IsValid = false,
            VerifiedAt = DateTime.UtcNow,
            IssuerId = issuerId
        };

        try
        {
            // Get DSC for issuer
            var dsc = await GetDocumentSignerCertificateAsync(issuerId, cancellationToken);
            if (dsc == null)
            {
                result.Errors.Add($"Document Signer Certificate not found for issuer: {issuerId}");
                return result;
            }

            // Validate DSC chain
            var chainValid = await ValidateCertificateChainAsync(dsc, cancellationToken);
            if (!chainValid)
            {
                result.Errors.Add("DSC certificate chain validation failed");
                return result;
            }

            // Verify document signature
            using var rsa = dsc.GetRSAPublicKey();
            if (rsa == null)
            {
                result.Errors.Add("Unable to extract public key from DSC");
                return result;
            }

            result.IsValid = rsa.VerifyData(
                documentData,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            if (result.IsValid)
            {
                result.SignerCertificate = dsc;
                result.CertificateChain = await BuildCertificateChainAsync(dsc, cancellationToken);
                _logger.LogInformation("Document verification successful for issuer: {Issuer}", issuerId);
            }
            else
            {
                result.Errors.Add("Document signature verification failed");
                _logger.LogWarning("Document verification failed for issuer: {Issuer}", issuerId);
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Verification error: {ex.Message}");
            _logger.LogError(ex, "Error verifying document");
        }

        return result;
    }

    #region Private Helper Methods

    private void InitializeMockCertificates()
    {
        // Create mock CSCA for Australia
        var auCsca = GenerateSelfSignedCertificate(
            "CN=Australian CSCA, O=Commonwealth of Australia, C=AU",
            KeyUsage.KeyCertSign | KeyUsage.CrlSign,
            true);
        _cscaCertificates["AU_" + auCsca.Thumbprint] = auCsca;

        // Create mock CSCA for New Zealand
        var nzCsca = GenerateSelfSignedCertificate(
            "CN=New Zealand CSCA, O=Government of New Zealand, C=NZ",
            KeyUsage.KeyCertSign | KeyUsage.CrlSign,
            true);
        _cscaCertificates["NZ_" + nzCsca.Thumbprint] = nzCsca;

        // Create mock DSCs
        var auDsc1 = GenerateCertificate(
            "CN=AU Transport DSC 001, O=Department of Transport, C=AU",
            KeyUsage.DigitalSignature | KeyUsage.NonRepudiation,
            auCsca);
        _dscCertificates["AU-TRANSPORT-001"] = auDsc1;

        var auDsc2 = GenerateCertificate(
            "CN=AU Health DSC 001, O=Department of Health, C=AU",
            KeyUsage.DigitalSignature | KeyUsage.NonRepudiation,
            auCsca);
        _dscCertificates["AU-HEALTH-001"] = auDsc2;

        // Create master list signing certificate
        _masterListSigningCert = GenerateSelfSignedCertificate(
            "CN=ICAO Master List Signer, O=ICAO, C=INT",
            KeyUsage.DigitalSignature,
            false);

        // Populate master list
        foreach (var kvp in _cscaCertificates)
        {
            _masterList.Add(new MasterListEntry
            {
                CountryCode = kvp.Key.Split('_')[0],
                CertificateThumbprint = kvp.Value.Thumbprint,
                Subject = kvp.Value.Subject,
                ValidFrom = kvp.Value.NotBefore,
                ValidTo = kvp.Value.NotAfter,
                KeyUsage = "Certificate Sign, CRL Sign"
            });
        }
    }

    private X509Certificate2 GenerateSelfSignedCertificate(
        string subjectName,
        KeyUsage keyUsage,
        bool isCa)
    {
        using var rsa = RSA.Create(4096);
        var request = new CertificateRequest(
            subjectName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add extensions
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension((X509KeyUsageFlags)keyUsage, critical: true));

        if (isCa)
        {
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(
                    certificateAuthority: true,
                    hasPathLengthConstraint: true,
                    pathLengthConstraint: 2,
                    critical: true));
        }

        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));

        var certificate = request.CreateSelfSigned(
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow.AddYears(10));

        return X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pfx, "mock"),
            "mock",
            X509KeyStorageFlags.Exportable);
    }

    private X509Certificate2 GenerateCertificate(
        string subjectName,
        KeyUsage keyUsage,
        X509Certificate2 issuer)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            subjectName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add extensions
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension((X509KeyUsageFlags)keyUsage, critical: true));

        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));

        // Create serial number
        var serialNumber = new byte[16];
        RandomNumberGenerator.Fill(serialNumber);

        // Sign with issuer
        using var issuerKey = issuer.GetRSAPrivateKey();
        var certificate = request.Create(
            issuer.SubjectName,
            X509SignatureGenerator.CreateForRSA(issuerKey!, RSASignaturePadding.Pkcs1),
            DateTime.UtcNow.AddMonths(-1),
            DateTime.UtcNow.AddYears(2),
            serialNumber);

        return X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pfx, "mock"),
            "mock",
            X509KeyStorageFlags.Exportable);
    }

    private byte[] GenerateMasterListSignature()
    {
        if (_masterListSigningCert == null)
        {
            return Array.Empty<byte>();
        }

        var dataToSign = Encoding.UTF8.GetBytes($"MasterList_v1.0.0_{DateTime.UtcNow:yyyyMMdd}");

        using var rsa = _masterListSigningCert.GetRSAPrivateKey();
        if (rsa == null)
        {
            return Array.Empty<byte>();
        }

        return rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    private List<RevokedCertificate> GenerateMockRevokedCertificates()
    {
        return new List<RevokedCertificate>
        {
            new RevokedCertificate
            {
                SerialNumber = "123456789ABCDEF0",
                RevocationDate = DateTime.UtcNow.AddDays(-30),
                Reason = RevocationReason.KeyCompromise
            },
            new RevokedCertificate
            {
                SerialNumber = "FEDCBA9876543210",
                RevocationDate = DateTime.UtcNow.AddDays(-15),
                Reason = RevocationReason.Superseded
            }
        };
    }

    private async Task<List<X509Certificate2>> BuildCertificateChainAsync(
        X509Certificate2 certificate,
        CancellationToken cancellationToken)
    {
        var chain = new List<X509Certificate2> { certificate };

        // Find issuer CSCA
        var issuerName = certificate.Issuer;
        var csca = _cscaCertificates.Values.FirstOrDefault(c => c.Subject == issuerName);

        if (csca != null)
        {
            chain.Add(csca);
        }

        return chain;
    }

    #endregion
}

#region Supporting Classes

public interface IIcaoService
{
    Task<IEnumerable<X509Certificate2>> GetCscaCertificatesAsync(string? countryCode = null, CancellationToken cancellationToken = default);
    Task<X509Certificate2?> GetDocumentSignerCertificateAsync(string issuerId, CancellationToken cancellationToken = default);
    Task<MasterList> GetMasterListAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidateCertificateChainAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default);
    Task<bool> VerifyMasterListSignatureAsync(byte[] masterListData, byte[] signature, CancellationToken cancellationToken = default);
    Task<CrlInfo> GetCrlAsync(string crlDistributionPoint, CancellationToken cancellationToken = default);
    Task<bool> ImportCscaCertificateAsync(byte[] certificateData, string countryCode, CancellationToken cancellationToken = default);
    Task<DocumentVerificationResult> VerifyDocumentAsync(byte[] documentData, byte[] signature, string issuerId, CancellationToken cancellationToken = default);
}

public class MasterList
{
    public string Version { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public DateTime NextUpdate { get; set; }
    public List<MasterListEntry> Entries { get; set; } = new();
    public byte[] Signature { get; set; } = Array.Empty<byte>();
}

public class MasterListEntry
{
    public string CountryCode { get; set; } = string.Empty;
    public string CertificateThumbprint { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string KeyUsage { get; set; } = string.Empty;
}

public class CrlInfo
{
    public string DistributionPoint { get; set; } = string.Empty;
    public DateTime ThisUpdate { get; set; }
    public DateTime NextUpdate { get; set; }
    public List<RevokedCertificate> RevokedCertificates { get; set; } = new();
    public string IssuerName { get; set; } = string.Empty;
}

public class RevokedCertificate
{
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime RevocationDate { get; set; }
    public RevocationReason Reason { get; set; }
}

public enum RevocationReason
{
    Unspecified,
    KeyCompromise,
    CaCompromise,
    AffiliationChanged,
    Superseded,
    CessationOfOperation,
    CertificateHold,
    RemoveFromCrl,
    PrivilegeWithdrawn,
    AaCompromise
}

public class DocumentVerificationResult
{
    public bool IsValid { get; set; }
    public DateTime VerifiedAt { get; set; }
    public string IssuerId { get; set; } = string.Empty;
    public X509Certificate2? SignerCertificate { get; set; }
    public List<X509Certificate2> CertificateChain { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

[Flags]
public enum KeyUsage
{
    None = 0,
    DigitalSignature = 1,
    NonRepudiation = 2,
    KeyEncipherment = 4,
    DataEncipherment = 8,
    KeyAgreement = 16,
    KeyCertSign = 32,
    CrlSign = 64,
    EncipherOnly = 128,
    DecipherOnly = 256
}

#endregion
