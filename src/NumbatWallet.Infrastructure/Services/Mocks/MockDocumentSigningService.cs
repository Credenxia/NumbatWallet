using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Infrastructure.Services.Mocks;

/// <summary>
/// Mock implementation of Document Signing Service for development/testing
/// Simulates Document Signing Certificate (DSC) operations for ePassports and secure documents
/// POA: External dependency mock for document signing and verification
/// </summary>
public class MockDocumentSigningService : IDocumentSigningService
{
    private readonly ILogger<MockDocumentSigningService> _logger;
    private readonly Dictionary<string, DocumentSigningCertificate> _signingCertificates;
    private readonly Dictionary<string, SignedDocument> _signedDocuments;
    private readonly List<SignatureAuditLog> _auditLogs;

    public MockDocumentSigningService(ILogger<MockDocumentSigningService> logger)
    {
        _logger = logger;
        _signingCertificates = new Dictionary<string, DocumentSigningCertificate>();
        _signedDocuments = new Dictionary<string, SignedDocument>();
        _auditLogs = new List<SignatureAuditLog>();

        InitializeMockCertificates();
    }

    private void InitializeMockCertificates()
    {
        _logger.LogInformation("Initializing mock document signing certificates");

        // Create mock DSCs for different document types
        CreatePassportSigningCertificate();
        CreateVisaSigningCertificate();
        CreateDriverLicenseSigningCertificate();
        CreateHealthRecordSigningCertificate();
        CreateEducationalCredentialSigningCertificate();

        _logger.LogInformation("Initialized {Count} document signing certificates", _signingCertificates.Count);
    }

    private void CreatePassportSigningCertificate()
    {
        var cert = GenerateMockSigningCertificate(
            "Australian Passport Office DSC",
            "DSC-PASSPORT-AU-001",
            DocumentType.Passport);

        _signingCertificates[cert.CertificateId] = cert;
    }

    private void CreateVisaSigningCertificate()
    {
        var cert = GenerateMockSigningCertificate(
            "Department of Home Affairs Visa DSC",
            "DSC-VISA-AU-001",
            DocumentType.Visa);

        _signingCertificates[cert.CertificateId] = cert;
    }

    private void CreateDriverLicenseSigningCertificate()
    {
        var cert = GenerateMockSigningCertificate(
            "WA Department of Transport DSC",
            "DSC-LICENSE-WA-001",
            DocumentType.DriverLicense);

        _signingCertificates[cert.CertificateId] = cert;
    }

    private void CreateHealthRecordSigningCertificate()
    {
        var cert = GenerateMockSigningCertificate(
            "Australian Digital Health Agency DSC",
            "DSC-HEALTH-AU-001",
            DocumentType.HealthRecord);

        _signingCertificates[cert.CertificateId] = cert;
    }

    private void CreateEducationalCredentialSigningCertificate()
    {
        var cert = GenerateMockSigningCertificate(
            "Australian Qualifications Framework DSC",
            "DSC-EDUCATION-AU-001",
            DocumentType.EducationalCredential);

        _signingCertificates[cert.CertificateId] = cert;
    }

    private DocumentSigningCertificate GenerateMockSigningCertificate(
        string name,
        string certificateId,
        DocumentType documentType)
    {
        using var rsa = RSA.Create(4096); // Use 4096-bit key for document signing
        var request = new CertificateRequest(
            $"CN={name}, O=NumbatWallet Mock DSC, C=AU",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add extensions for document signing
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
                true));

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));

        // Add extended key usage for document signing
        var oids = new OidCollection
        {
            new Oid("1.3.6.1.5.5.7.3.36") // Document Signing
        };
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(oids, true));

        // Create self-signed certificate
        var x509Cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow.AddYears(3));

        return new DocumentSigningCertificate
        {
            CertificateId = certificateId,
            Name = name,
            Certificate = x509Cert,
            DocumentType = documentType,
            IssuedAt = DateTime.UtcNow.AddDays(-30),
            ExpiresAt = DateTime.UtcNow.AddYears(3),
            IsActive = true,
            SignatureAlgorithm = "RSA-SHA256",
            KeySize = 4096
        };
    }

    public async Task<SignedDocument> SignDocumentAsync(
        byte[] documentData,
        string certificateId,
        SigningOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate signing delay

        if (!_signingCertificates.TryGetValue(certificateId, out var signingCert))
        {
            throw new InvalidOperationException($"Signing certificate {certificateId} not found");
        }

        if (!signingCert.IsActive)
        {
            throw new InvalidOperationException($"Signing certificate {certificateId} is not active");
        }

        options ??= new SigningOptions();

        // Generate document hash
        var documentHash = ComputeHash(documentData, options.HashAlgorithm);

        // Create signature
        byte[] signature;
        using (var rsa = signingCert.Certificate.GetRSAPrivateKey())
        {
            if (rsa == null)
            {
                throw new InvalidOperationException("Private key not available for signing");
            }

            signature = rsa.SignHash(
                documentHash,
                GetHashAlgorithmName(options.HashAlgorithm),
                RSASignaturePadding.Pkcs1);
        }

        // Create signed document
        var signedDoc = new SignedDocument
        {
            DocumentId = Guid.NewGuid().ToString(),
            DocumentHash = Convert.ToBase64String(documentHash),
            Signature = Convert.ToBase64String(signature),
            SigningCertificateId = certificateId,
            SigningCertificateThumbprint = signingCert.Certificate.Thumbprint,
            SignedAt = DateTime.UtcNow,
            SignatureAlgorithm = $"RSA-{options.HashAlgorithm}",
            DocumentType = signingCert.DocumentType,
            Metadata = options.Metadata ?? new Dictionary<string, string>()
        };

        // Add timestamp if requested
        if (options.IncludeTimestamp)
        {
            signedDoc.TimestampToken = GenerateTimestampToken(documentHash);
            signedDoc.TimestampAuthority = "Mock TSA";
        }

        // Store signed document
        _signedDocuments[signedDoc.DocumentId] = signedDoc;

        // Log audit entry
        LogSigningAudit(signedDoc, signingCert, "Document signed successfully");

        _logger.LogInformation("Document signed with certificate {CertId}, document ID: {DocId}",
            certificateId, signedDoc.DocumentId);

        return signedDoc;
    }

    public async Task<VerificationResult> VerifySignatureAsync(
        byte[] documentData,
        string signature,
        string certificateId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(30, cancellationToken); // Simulate verification delay

        var result = new VerificationResult
        {
            VerifiedAt = DateTime.UtcNow
        };

        try
        {
            if (!_signingCertificates.TryGetValue(certificateId, out var signingCert))
            {
                result.IsValid = false;
                result.Reason = $"Signing certificate {certificateId} not found";
                return result;
            }

            // Compute document hash
            var documentHash = ComputeHash(documentData, "SHA256");

            // Verify signature
            using (var rsa = signingCert.Certificate.GetRSAPublicKey())
            {
                if (rsa == null)
                {
                    result.IsValid = false;
                    result.Reason = "Public key not available for verification";
                    return result;
                }

                var signatureBytes = Convert.FromBase64String(signature);

                result.IsValid = rsa.VerifyHash(
                    documentHash,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                if (result.IsValid)
                {
                    result.SignerName = signingCert.Name;
                    result.SignerCertificateId = certificateId;
                    result.CertificateValidFrom = signingCert.IssuedAt;
                    result.CertificateValidTo = signingCert.ExpiresAt;

                    // Check certificate validity
                    var now = DateTime.UtcNow;
                    if (now < signingCert.IssuedAt || now > signingCert.ExpiresAt)
                    {
                        result.IsValid = false;
                        result.Reason = "Certificate is not within valid time range";
                    }
                }
                else
                {
                    result.Reason = "Signature verification failed";
                }
            }

            _logger.LogInformation("Signature verification for certificate {CertId}: {IsValid}",
                certificateId, result.IsValid);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during signature verification");
            result.IsValid = false;
            result.Reason = $"Verification error: {ex.Message}";
            return result;
        }
    }

    public async Task<SignedDocument?> GetSignedDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate retrieval delay

        if (_signedDocuments.TryGetValue(documentId, out var document))
        {
            _logger.LogInformation("Retrieved signed document {DocId}", documentId);
            return document;
        }

        _logger.LogWarning("Signed document {DocId} not found", documentId);
        return null;
    }

    public async Task<IEnumerable<DocumentSigningCertificate>> GetSigningCertificatesAsync(
        DocumentType? documentType = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(15, cancellationToken); // Simulate query delay

        var query = _signingCertificates.Values.AsEnumerable();

        if (documentType.HasValue)
        {
            query = query.Where(c => c.DocumentType == documentType.Value);
        }

        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        var results = query.ToList();
        _logger.LogInformation("Retrieved {Count} signing certificates", results.Count);

        return results;
    }

    public async Task<bool> RevokeCertificateAsync(
        string certificateId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(25, cancellationToken); // Simulate revocation delay

        if (_signingCertificates.TryGetValue(certificateId, out var cert))
        {
            cert.IsActive = false;
            cert.RevocationReason = reason;
            cert.RevokedAt = DateTime.UtcNow;

            _logger.LogWarning("Certificate {CertId} revoked: {Reason}", certificateId, reason);
            return true;
        }

        _logger.LogError("Certificate {CertId} not found for revocation", certificateId);
        return false;
    }

    public async Task<IEnumerable<SignatureAuditLog>> GetAuditLogsAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? certificateId = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(20, cancellationToken); // Simulate query delay

        var query = _auditLogs.AsEnumerable();

        if (from.HasValue)
        {
            query = query.Where(log => log.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(log => log.Timestamp <= to.Value);
        }

        if (!string.IsNullOrEmpty(certificateId))
        {
            query = query.Where(log => log.CertificateId == certificateId);
        }

        var results = query.OrderByDescending(log => log.Timestamp).ToList();
        _logger.LogInformation("Retrieved {Count} audit logs", results.Count);

        return results;
    }

    private byte[] ComputeHash(byte[] data, string algorithm)
    {
        using var hasher = GetHashAlgorithm(algorithm);
        return hasher.ComputeHash(data);
    }

    private HashAlgorithm GetHashAlgorithm(string algorithm)
    {
        return algorithm.ToUpperInvariant() switch
        {
            "SHA256" => SHA256.Create(),
            "SHA384" => SHA384.Create(),
            "SHA512" => SHA512.Create(),
            _ => SHA256.Create()
        };
    }

    private HashAlgorithmName GetHashAlgorithmName(string algorithm)
    {
        return algorithm.ToUpperInvariant() switch
        {
            "SHA256" => HashAlgorithmName.SHA256,
            "SHA384" => HashAlgorithmName.SHA384,
            "SHA512" => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256
        };
    }

    private string GenerateTimestampToken(byte[] documentHash)
    {
        // Generate mock timestamp token
        var timestamp = new
        {
            Hash = Convert.ToBase64String(documentHash),
            Timestamp = DateTime.UtcNow,
            Nonce = Guid.NewGuid().ToString(),
            Authority = "Mock TSA"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(timestamp);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private void LogSigningAudit(SignedDocument document, DocumentSigningCertificate certificate, string action)
    {
        var audit = new SignatureAuditLog
        {
            LogId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Action = action,
            DocumentId = document.DocumentId,
            CertificateId = certificate.CertificateId,
            CertificateName = certificate.Name,
            DocumentType = document.DocumentType,
            Success = true
        };

        _auditLogs.Add(audit);
    }
}

// Supporting types for Document Signing Service
public interface IDocumentSigningService
{
    Task<SignedDocument> SignDocumentAsync(byte[] documentData, string certificateId, SigningOptions? options = null, CancellationToken cancellationToken = default);
    Task<VerificationResult> VerifySignatureAsync(byte[] documentData, string signature, string certificateId, CancellationToken cancellationToken = default);
    Task<SignedDocument?> GetSignedDocumentAsync(string documentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DocumentSigningCertificate>> GetSigningCertificatesAsync(DocumentType? documentType = null, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<bool> RevokeCertificateAsync(string certificateId, string reason, CancellationToken cancellationToken = default);
    Task<IEnumerable<SignatureAuditLog>> GetAuditLogsAsync(DateTime? from = null, DateTime? to = null, string? certificateId = null, CancellationToken cancellationToken = default);
}

public class DocumentSigningCertificate
{
    public string CertificateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public X509Certificate2 Certificate { get; set; } = null!;
    public DocumentType DocumentType { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string SignatureAlgorithm { get; set; } = string.Empty;
    public int KeySize { get; set; }
    public string? RevocationReason { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public class SignedDocument
{
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string SigningCertificateId { get; set; } = string.Empty;
    public string SigningCertificateThumbprint { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    public string SignatureAlgorithm { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string? TimestampToken { get; set; }
    public string? TimestampAuthority { get; set; }
}

public class SigningOptions
{
    public string HashAlgorithm { get; set; } = "SHA256";
    public bool IncludeTimestamp { get; set; } = true;
    public Dictionary<string, string>? Metadata { get; set; }
}

public class VerificationResult
{
    public bool IsValid { get; set; }
    public string? Reason { get; set; }
    public string? SignerName { get; set; }
    public string? SignerCertificateId { get; set; }
    public DateTime? CertificateValidFrom { get; set; }
    public DateTime? CertificateValidTo { get; set; }
    public DateTime VerifiedAt { get; set; }
}

public class SignatureAuditLog
{
    public string LogId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string CertificateId { get; set; } = string.Empty;
    public string CertificateName { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public bool Success { get; set; }
}

public enum DocumentType
{
    Passport,
    Visa,
    DriverLicense,
    HealthRecord,
    EducationalCredential,
    WorkPermit,
    BirthCertificate,
    Other
}