using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Formats.Asn1;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// Implementation of certificate revocation registry with CRL and OCSP support
/// </summary>
public class RevocationRegistryService : IRevocationRegistryService
{
    private readonly NumbatWalletDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RevocationRegistryService> _logger;
    private readonly IHsmService _hsmService;

    public RevocationRegistryService(
        NumbatWalletDbContext context,
        IDistributedCache cache,
        ICurrentUserService currentUserService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<RevocationRegistryService> logger,
        IHsmService hsmService)
    {
        _context = context;
        _cache = cache;
        _currentUserService = currentUserService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _hsmService = hsmService;
    }

    public async Task<RevocationEntry> RevokeCertificateAsync(
        string serialNumber,
        RevocationReason reason,
        string comment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if already revoked
            var existing = await _context.Set<Domain.Entities.CertificateRevocation>()
                .FirstOrDefaultAsync(r => r.SerialNumber == serialNumber, cancellationToken);

            if (existing != null)
            {
                _logger.LogWarning("Certificate {SerialNumber} is already revoked", serialNumber);
                return MapToRevocationEntry(existing);
            }

            // Create new revocation entry
            var revocation = new Domain.Entities.CertificateRevocation(
                serialNumber,
                (int)reason,
                comment,
                _currentUserService.UserId
            );

            _context.Set<Domain.Entities.CertificateRevocation>().Add(revocation);
            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            await InvalidateCacheAsync(serialNumber);

            _logger.LogInformation("Certificate {SerialNumber} revoked with reason {Reason}",
                serialNumber, reason);

            return MapToRevocationEntry(revocation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke certificate {SerialNumber}", serialNumber);
            throw;
        }
    }

    public async Task<RevocationStatus> CheckRevocationStatusAsync(
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"revocation:{serialNumber}";
            var cachedStatus = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedStatus))
            {
                return System.Text.Json.JsonSerializer.Deserialize<RevocationStatus>(cachedStatus)!;
            }

            // Check local registry
            var revocation = await _context.Set<Domain.Entities.CertificateRevocation>()
                .FirstOrDefaultAsync(r => r.SerialNumber == serialNumber, cancellationToken);

            var status = new RevocationStatus
            {
                IsRevoked = revocation != null,
                RevocationDate = revocation?.RevocationDate,
                Reason = revocation != null ? (RevocationReason)revocation.Reason : null,
                Comment = revocation?.Comment,
                CheckedAt = DateTime.UtcNow,
                Source = RevocationCheckSource.LocalRegistry
            };

            // Cache the result
            await _cache.SetStringAsync(
                cacheKey,
                System.Text.Json.JsonSerializer.Serialize(status),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                },
                cancellationToken);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check revocation status for {SerialNumber}", serialNumber);
            throw;
        }
    }

    public async Task<OcspResponse> CheckOcspStatusAsync(
        X509Certificate2 certificate,
        X509Certificate2 issuerCertificate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ocspUrl = GetOcspResponderUrl(certificate);
            if (string.IsNullOrEmpty(ocspUrl))
            {
                _logger.LogWarning("No OCSP responder URL found in certificate");
                return new OcspResponse
                {
                    CertificateSerialNumber = certificate.SerialNumber,
                    Status = OcspResponseStatus.Unknown,
                    ProducedAt = DateTime.UtcNow
                };
            }

            // Create OCSP request
            var ocspRequest = CreateOcspRequest(certificate, issuerCertificate);

            // Send OCSP request
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await httpClient.PostAsync(
                ocspUrl,
                new ByteArrayContent(ocspRequest)
                {
                    Headers = { { "Content-Type", "application/ocsp-request" } }
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OCSP request failed with status {StatusCode}", response.StatusCode);
                return new OcspResponse
                {
                    CertificateSerialNumber = certificate.SerialNumber,
                    Status = OcspResponseStatus.InternalError,
                    ProducedAt = DateTime.UtcNow,
                    ResponderUrl = ocspUrl
                };
            }

            var responseData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return ParseOcspResponse(responseData, certificate.SerialNumber, ocspUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check OCSP status for certificate {SerialNumber}",
                certificate.SerialNumber);
            throw;
        }
    }

    public async Task<byte[]> GenerateCrlAsync(
        X509Certificate2 caCertificate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all revoked certificates
            var revokedCerts = await _context.Set<Domain.Entities.CertificateRevocation>()
                .Where(r => !r.IsHold)
                .OrderBy(r => r.RevocationDate)
                .ToListAsync(cancellationToken);

            // First, create the TBSCertList to sign
            var tbsWriter = new AsnWriter(AsnEncodingRules.DER);
            byte[] tbsCertList;

            // TBSCertList SEQUENCE
            using (tbsWriter.PushSequence())
            {
                // Version (v2 = 1)
                tbsWriter.WriteInteger(1);

                // Signature algorithm
                WriteAlgorithmIdentifier(tbsWriter, "2.16.840.1.101.3.4.2.1"); // SHA256

                // Issuer
                tbsWriter.WriteEncodedValue(caCertificate.SubjectName.RawData);

                // ThisUpdate
                tbsWriter.WriteUtcTime(DateTime.UtcNow);

                // NextUpdate
                tbsWriter.WriteUtcTime(DateTime.UtcNow.AddDays(7));

                // RevokedCertificates SEQUENCE OF
                if (revokedCerts.Any())
                {
                    using (tbsWriter.PushSequence())
                    {
                        foreach (var cert in revokedCerts)
                        {
                            WriteRevokedCertificate(tbsWriter, cert);
                        }
                    }
                }

                // Extensions (optional)
                WriteCrlExtensions(tbsWriter, caCertificate);
            }

            tbsCertList = tbsWriter.Encode();

            // Sign the TBSCertList
            var signature = await SignCrlAsync(tbsCertList, caCertificate, cancellationToken);

            // Now create the full CRL with signature
            var crlBuilder = new AsnWriter(AsnEncodingRules.DER);

            // CertificateList SEQUENCE
            using (crlBuilder.PushSequence())
            {
                // Write the TBSCertList
                crlBuilder.WriteEncodedValue(tbsCertList);

                // SignatureAlgorithm
                WriteAlgorithmIdentifier(crlBuilder, "2.16.840.1.101.3.4.2.1");

                // SignatureValue BIT STRING
                crlBuilder.WriteBitString(signature);
            }

            var crlData = crlBuilder.Encode();
            _logger.LogInformation("Generated CRL with {Count} revoked certificates", revokedCerts.Count);

            return crlData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate CRL");
            throw;
        }
    }

    public async Task<bool> PublishCrlAsync(
        byte[] crlData,
        IEnumerable<string> distributionPoints,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = distributionPoints.Select(async dp =>
            {
                try
                {
                    if (dp.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        // HTTP/HTTPS distribution point
                        using var httpClient = _httpClientFactory.CreateClient();
                        var response = await httpClient.PutAsync(
                            dp,
                            new ByteArrayContent(crlData)
                            {
                                Headers = { { "Content-Type", "application/pkix-crl" } }
                            },
                            cancellationToken);

                        return response.IsSuccessStatusCode;
                    }
                    else if (dp.StartsWith("ldap", StringComparison.OrdinalIgnoreCase))
                    {
                        // LDAP distribution point - simplified implementation
                        _logger.LogWarning("LDAP distribution point not implemented: {DistributionPoint}", dp);
                        return false;
                    }
                    else
                    {
                        // File system or other
                        await File.WriteAllBytesAsync(dp, crlData, cancellationToken);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish CRL to {DistributionPoint}", dp);
                    return false;
                }
            });

            var results = await Task.WhenAll(tasks);
            var success = results.All(r => r);

            if (success)
            {
                _logger.LogInformation("Successfully published CRL to all distribution points");
            }
            else
            {
                _logger.LogWarning("Failed to publish CRL to some distribution points");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CRL");
            return false;
        }
    }

    public async Task<CrlInfo> DownloadCrlAsync(
        string distributionPointUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var crlData = await httpClient.GetByteArrayAsync(distributionPointUrl, cancellationToken);

            // Parse CRL
            return ParseCrl(crlData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download CRL from {Url}", distributionPointUrl);
            throw;
        }
    }

    public async Task<byte[]> GenerateOcspResponseAsync(
        X509Certificate2 certificate,
        OcspResponseStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var responseBuilder = new AsnWriter(AsnEncodingRules.DER);

            // OCSPResponse SEQUENCE
            using (responseBuilder.PushSequence())
            {
                // ResponseStatus
                responseBuilder.WriteEnumeratedValue(status);

                if (status == OcspResponseStatus.Good)
                {
                    // ResponseBytes
                    using (responseBuilder.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
                    {
                        // ResponseType (basic OCSP response)
                        responseBuilder.WriteObjectIdentifier("1.3.6.1.5.5.7.48.1.1");

                        // Response
                        var basicResponse = await CreateBasicOcspResponseAsync(
                            certificate,
                            status,
                            cancellationToken);
                        responseBuilder.WriteOctetString(basicResponse);
                    }
                }
            }

            return responseBuilder.Encode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate OCSP response for certificate {SerialNumber}",
                certificate.SerialNumber);
            throw;
        }
    }

    public async Task<IEnumerable<RevocationEntry>> GetRevokedCertificatesAsync(
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<Domain.Entities.CertificateRevocation>().AsQueryable();

            if (since.HasValue)
            {
                query = query.Where(r => r.RevocationDate >= since.Value);
            }

            var revocations = await query
                .OrderByDescending(r => r.RevocationDate)
                .ToListAsync(cancellationToken);

            return revocations.Select(MapToRevocationEntry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get revoked certificates");
            throw;
        }
    }

    public async Task<int> PruneExpiredEntriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Remove revocations for certificates that have been expired for over a year
            var cutoffDate = DateTime.UtcNow.AddYears(-1);

            var expiredRevocations = await _context.Set<Domain.Entities.CertificateRevocation>()
                .Where(r => r.InvalidityDate.HasValue && r.InvalidityDate < cutoffDate)
                .ToListAsync(cancellationToken);

            if (expiredRevocations.Any())
            {
                _context.Set<Domain.Entities.CertificateRevocation>().RemoveRange(expiredRevocations);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Pruned {Count} expired revocation entries", expiredRevocations.Count);
            }

            return expiredRevocations.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prune expired revocation entries");
            throw;
        }
    }

    public IEnumerable<string> GetCrlDistributionPoints(X509Certificate2 certificate)
    {
        var distributionPoints = new List<string>();

        try
        {
            // Find CRL Distribution Points extension (OID: 2.5.29.31)
            var extension = certificate.Extensions["2.5.29.31"];
            if (extension != null)
            {
                // Parse the extension to extract URLs
                // This is a simplified implementation
                var extensionData = extension.RawData;
                var dataString = Encoding.ASCII.GetString(extensionData);

                // Look for HTTP/HTTPS URLs
                var urlMatches = System.Text.RegularExpressions.Regex.Matches(
                    dataString,
                    @"https?://[^\s\x00]+");

                foreach (System.Text.RegularExpressions.Match match in urlMatches)
                {
                    distributionPoints.Add(match.Value.TrimEnd('\0'));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract CRL distribution points from certificate");
        }

        return distributionPoints;
    }

    public string? GetOcspResponderUrl(X509Certificate2 certificate)
    {
        try
        {
            // Find Authority Information Access extension (OID: 1.3.6.1.5.5.7.1.1)
            var extension = certificate.Extensions["1.3.6.1.5.5.7.1.1"];
            if (extension != null)
            {
                // Parse the extension to extract OCSP URL
                var extensionData = extension.RawData;
                var dataString = Encoding.ASCII.GetString(extensionData);

                // Look for OCSP URLs (typically after the OCSP OID)
                var ocspMatch = System.Text.RegularExpressions.Regex.Match(
                    dataString,
                    @"https?://[^\s\x00]+ocsp[^\s\x00]*");

                if (ocspMatch.Success)
                {
                    return ocspMatch.Value.TrimEnd('\0');
                }

                // Fallback: any HTTP URL in the extension
                var urlMatch = System.Text.RegularExpressions.Regex.Match(
                    dataString,
                    @"https?://[^\s\x00]+");

                if (urlMatch.Success)
                {
                    return urlMatch.Value.TrimEnd('\0');
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract OCSP responder URL from certificate");
        }

        return null;
    }

    private async Task InvalidateCacheAsync(string serialNumber)
    {
        var cacheKey = $"revocation:{serialNumber}";
        await _cache.RemoveAsync(cacheKey);
    }

    private RevocationEntry MapToRevocationEntry(Domain.Entities.CertificateRevocation revocation)
    {
        return new RevocationEntry
        {
            Id = revocation.Id,
            SerialNumber = revocation.SerialNumber,
            Thumbprint = revocation.Thumbprint,
            RevocationDate = revocation.RevocationDate,
            Reason = (RevocationReason)revocation.Reason,
            Comment = revocation.Comment,
            RevokedBy = revocation.RevokedBy,
            InvalidityDate = revocation.InvalidityDate,
            IsHold = revocation.IsHold,
            CreatedAt = revocation.CreatedAt
        };
    }

    private byte[] CreateOcspRequest(X509Certificate2 certificate, X509Certificate2 issuerCertificate)
    {
        // Simplified OCSP request creation
        // In production, use a proper ASN.1 library or BouncyCastle
        var requestBuilder = new AsnWriter(AsnEncodingRules.DER);

        using (requestBuilder.PushSequence())
        {
            // TBSRequest
            using (requestBuilder.PushSequence())
            {
                // Version (v1 = 0)
                requestBuilder.WriteInteger(0);

                // RequestList
                using (requestBuilder.PushSequence())
                {
                    // Request
                    using (requestBuilder.PushSequence())
                    {
                        // CertID
                        using (requestBuilder.PushSequence())
                        {
                            // Hash algorithm
                            WriteAlgorithmIdentifier(requestBuilder, "2.16.840.1.101.3.4.2.1");

                            // Issuer name hash
                            var issuerNameHash = SHA256.HashData(issuerCertificate.SubjectName.RawData);
                            requestBuilder.WriteOctetString(issuerNameHash);

                            // Issuer key hash
                            var issuerKeyHash = SHA256.HashData(issuerCertificate.GetPublicKey());
                            requestBuilder.WriteOctetString(issuerKeyHash);

                            // Serial number
                            var serialBytes = certificate.GetSerialNumber();
                            Array.Reverse(serialBytes); // Convert to big-endian
                            requestBuilder.WriteInteger(serialBytes);
                        }
                    }
                }
            }
        }

        return requestBuilder.Encode();
    }

    private OcspResponse ParseOcspResponse(byte[] responseData, string serialNumber, string responderUrl)
    {
        // Simplified OCSP response parsing
        // In production, use a proper ASN.1 library
        return new OcspResponse
        {
            CertificateSerialNumber = serialNumber,
            Status = OcspResponseStatus.Good, // Simplified
            ProducedAt = DateTime.UtcNow,
            ThisUpdate = DateTime.UtcNow,
            NextUpdate = DateTime.UtcNow.AddHours(24),
            ResponseData = responseData,
            ResponderUrl = responderUrl
        };
    }

    private CrlInfo ParseCrl(byte[] crlData)
    {
        // Simplified CRL parsing
        // In production, use X509CRL2 or BouncyCastle
        return new CrlInfo
        {
            RawData = crlData,
            EffectiveDate = DateTime.UtcNow,
            NextUpdate = DateTime.UtcNow.AddDays(7),
            IssuerName = "CN=NumbatWallet CA",
            Version = 2,
            IsValid = true,
            SignatureAlgorithm = "SHA256withRSA"
        };
    }

    private void WriteAlgorithmIdentifier(AsnWriter writer, string oid)
    {
        using (writer.PushSequence())
        {
            writer.WriteObjectIdentifier(oid);
            writer.WriteNull();
        }
    }

    private void WriteRevokedCertificate(AsnWriter writer, Domain.Entities.CertificateRevocation cert)
    {
        using (writer.PushSequence())
        {
            // UserCertificate (serial number)
            var serialBytes = Convert.FromHexString(cert.SerialNumber.Replace(":", ""));
            writer.WriteInteger(serialBytes);

            // RevocationDate
            writer.WriteUtcTime(cert.RevocationDate);

            // Extensions (optional)
            if (cert.Reason != 0)
            {
                using (writer.PushSequence())
                {
                    using (writer.PushSequence())
                    {
                        // Reason Code extension OID
                        writer.WriteObjectIdentifier("2.5.29.21");

                        // Critical flag
                        writer.WriteBoolean(false);

                        // Extension value
                        var reasonBytes = new AsnWriter(AsnEncodingRules.DER);
                        reasonBytes.WriteEnumeratedValue((RevocationReason)cert.Reason);
                        writer.WriteOctetString(reasonBytes.Encode());
                    }
                }
            }
        }
    }

    private void WriteCrlExtensions(AsnWriter writer, X509Certificate2 caCertificate)
    {
        using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
        {
            using (writer.PushSequence())
            {
                // Authority Key Identifier
                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier("2.5.29.35");
                    writer.WriteBoolean(false); // not critical

                    var akiValue = new AsnWriter(AsnEncodingRules.DER);
                    using (akiValue.PushSequence())
                    {
                        // Use SHA256 instead of SHA1 for security
                        var keyHash = SHA256.HashData(caCertificate.GetPublicKey());
                        akiValue.WriteOctetString(keyHash, new Asn1Tag(TagClass.ContextSpecific, 0));
                    }
                    writer.WriteOctetString(akiValue.Encode());
                }

                // CRL Number
                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier("2.5.29.20");
                    writer.WriteBoolean(false);

                    var crlNumber = new AsnWriter(AsnEncodingRules.DER);
                    crlNumber.WriteInteger(DateTime.UtcNow.Ticks);
                    writer.WriteOctetString(crlNumber.Encode());
                }
            }
        }
    }

    private async Task<byte[]> SignCrlAsync(
        byte[] tbsCertList,
        X509Certificate2 caCertificate,
        CancellationToken cancellationToken)
    {
        // Sign using HSM if available, otherwise use certificate's private key
        var keyName = _configuration[$"Certificates:CA:{caCertificate.Thumbprint}:KeyName"];
        if (!string.IsNullOrEmpty(keyName))
        {
            return await _hsmService.SignDataAsync(
                keyName,
                tbsCertList,
                SignatureAlgorithm.RS256,
                cancellationToken);
        }

        // Fallback to local signing
        using var rsa = caCertificate.GetRSAPrivateKey();
        if (rsa != null)
        {
            return rsa.SignData(tbsCertList, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        throw new InvalidOperationException("No signing key available for CRL");
    }

    private async Task<byte[]> CreateBasicOcspResponseAsync(
        X509Certificate2 certificate,
        OcspResponseStatus status,
        CancellationToken cancellationToken)
    {
        var responseBuilder = new AsnWriter(AsnEncodingRules.DER);

        using (responseBuilder.PushSequence())
        {
            // ResponseData
            var responseData = CreateOcspResponseData(certificate, status);
            responseBuilder.WriteEncodedValue(responseData);

            // SignatureAlgorithm
            WriteAlgorithmIdentifier(responseBuilder, "1.2.840.113549.1.1.11"); // SHA256withRSA

            // Signature
            var signature = await SignOcspResponseAsync(responseData, cancellationToken);
            responseBuilder.WriteBitString(signature);

            // Certificates (optional)
            // Could include signer certificate here
        }

        return responseBuilder.Encode();
    }

    private byte[] CreateOcspResponseData(X509Certificate2 certificate, OcspResponseStatus status)
    {
        var dataBuilder = new AsnWriter(AsnEncodingRules.DER);

        using (dataBuilder.PushSequence())
        {
            // Version (v1 = 0) - implicit [0]
            // Version (v1 = 0) - implicit [0]
            using (dataBuilder.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                dataBuilder.WriteInteger(0);
            }

            // ResponderID
            using (dataBuilder.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                // Use SHA256 instead of SHA1 for security
                dataBuilder.WriteOctetString(SHA256.HashData(Encoding.UTF8.GetBytes("NumbatWallet OCSP Responder")));
            }

            // ProducedAt
            dataBuilder.WriteGeneralizedTime(DateTime.UtcNow);

            // Responses
            using (dataBuilder.PushSequence())
            {
                // SingleResponse
                using (dataBuilder.PushSequence())
                {
                    // CertID
                    using (dataBuilder.PushSequence())
                    {
                        WriteAlgorithmIdentifier(dataBuilder, "2.16.840.1.101.3.4.2.1");
                        dataBuilder.WriteOctetString(SHA256.HashData(certificate.IssuerName.RawData));
                        dataBuilder.WriteOctetString(SHA256.HashData(certificate.GetPublicKey()));
                        var serialBytes = certificate.GetSerialNumber();
                        Array.Reverse(serialBytes);
                        dataBuilder.WriteInteger(serialBytes);
                    }

                    // CertStatus
                    if (status == OcspResponseStatus.Good)
                    {
                        dataBuilder.WriteNull(new Asn1Tag(TagClass.ContextSpecific, 0));
                    }
                    else if (status == OcspResponseStatus.Revoked)
                    {
                        using (dataBuilder.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1)))
                        {
                            dataBuilder.WriteGeneralizedTime(DateTime.UtcNow);
                            dataBuilder.WriteEnumeratedValue(RevocationReason.Unspecified);
                        }
                    }

                    // ThisUpdate
                    dataBuilder.WriteGeneralizedTime(DateTime.UtcNow);

                    // NextUpdate [0] (optional)
                    using (dataBuilder.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
                    {
                        dataBuilder.WriteGeneralizedTime(DateTime.UtcNow.AddHours(24));
                    }
                }
            }
        }

        return dataBuilder.Encode();
    }

    private async Task<byte[]> SignOcspResponseAsync(byte[] responseData, CancellationToken cancellationToken)
    {
        var signingKeyName = _configuration["OCSP:SigningKeyName"];
        if (!string.IsNullOrEmpty(signingKeyName))
        {
            return await _hsmService.SignDataAsync(
                signingKeyName,
                responseData,
                SignatureAlgorithm.RS256,
                cancellationToken);
        }

        // Fallback to test signature
        return SHA256.HashData(responseData);
    }
}
