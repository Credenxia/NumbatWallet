using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Infrastructure.Services.Mocks;

/// <summary>
/// Mock implementation of Trust List Service for development/testing
/// Simulates government/enterprise trust list management
/// POA: External dependency mock for trust list verification
/// </summary>
public class MockTrustListService : ITrustListService
{
    private readonly ILogger<MockTrustListService> _logger;
    private readonly Dictionary<string, TrustedEntity> _trustedEntities;
    private readonly Dictionary<string, X509Certificate2> _trustedCertificates;
    private readonly List<string> _revokedCertificates;

    public MockTrustListService(ILogger<MockTrustListService> logger)
    {
        _logger = logger;
        _trustedEntities = new Dictionary<string, TrustedEntity>();
        _trustedCertificates = new Dictionary<string, X509Certificate2>();
        _revokedCertificates = new List<string>();

        InitializeMockData();
    }

    private void InitializeMockData()
    {
        _logger.LogInformation("Initializing mock trust list data");

        // Add mock government entities
        AddMockGovernmentEntities();

        // Add mock enterprise partners
        AddMockEnterprisePartners();

        // Add mock healthcare providers
        AddMockHealthcareProviders();

        // Add mock educational institutions
        AddMockEducationalInstitutions();

        _logger.LogInformation("Mock trust list initialized with {Count} entities", _trustedEntities.Count);
    }

    private void AddMockGovernmentEntities()
    {
        var entities = new[]
        {
            new TrustedEntity
            {
                EntityId = "GOV-AU-001",
                Name = "Australian Government Department of Home Affairs",
                Type = EntityType.Government,
                Country = "AU",
                TrustLevel = TrustLevel.Full,
                ValidFrom = DateTime.UtcNow.AddYears(-2),
                ValidTo = DateTime.UtcNow.AddYears(3),
                Capabilities = new[] { "IssuePassport", "IssueVisa", "VerifyIdentity" }
            },
            new TrustedEntity
            {
                EntityId = "GOV-AU-002",
                Name = "Services Australia",
                Type = EntityType.Government,
                Country = "AU",
                TrustLevel = TrustLevel.Full,
                ValidFrom = DateTime.UtcNow.AddYears(-2),
                ValidTo = DateTime.UtcNow.AddYears(3),
                Capabilities = new[] { "IssueMedicare", "IssueDriverLicense", "VerifyIdentity" }
            },
            new TrustedEntity
            {
                EntityId = "GOV-WA-001",
                Name = "Government of Western Australia",
                Type = EntityType.Government,
                Country = "AU",
                State = "WA",
                TrustLevel = TrustLevel.Full,
                ValidFrom = DateTime.UtcNow.AddYears(-1),
                ValidTo = DateTime.UtcNow.AddYears(4),
                Capabilities = new[] { "IssueDriverLicense", "IssueProofOfAge", "VerifyAddress" }
            }
        };

        foreach (var entity in entities)
        {
            _trustedEntities[entity.EntityId] = entity;

            // Generate mock certificate for each entity
            var cert = GenerateMockCertificate(entity.Name, entity.EntityId);
            _trustedCertificates[entity.EntityId] = cert;
        }
    }

    private void AddMockEnterprisePartners()
    {
        var entities = new[]
        {
            new TrustedEntity
            {
                EntityId = "ENT-MINE-001",
                Name = "BHP Group Limited",
                Type = EntityType.Enterprise,
                Industry = "Mining",
                Country = "AU",
                TrustLevel = TrustLevel.High,
                ValidFrom = DateTime.UtcNow.AddMonths(-6),
                ValidTo = DateTime.UtcNow.AddYears(2),
                Capabilities = new[] { "IssueSiteAccess", "IssueWorkPermit", "VerifyCompetency" }
            },
            new TrustedEntity
            {
                EntityId = "ENT-MINE-002",
                Name = "Rio Tinto Group",
                Type = EntityType.Enterprise,
                Industry = "Mining",
                Country = "AU",
                TrustLevel = TrustLevel.High,
                ValidFrom = DateTime.UtcNow.AddMonths(-3),
                ValidTo = DateTime.UtcNow.AddYears(2),
                Capabilities = new[] { "IssueSiteAccess", "IssueTrainingCertificate", "VerifyQualification" }
            },
            new TrustedEntity
            {
                EntityId = "ENT-CONST-001",
                Name = "Multiplex Construction",
                Type = EntityType.Enterprise,
                Industry = "Construction",
                Country = "AU",
                TrustLevel = TrustLevel.Medium,
                ValidFrom = DateTime.UtcNow.AddMonths(-9),
                ValidTo = DateTime.UtcNow.AddYears(1),
                Capabilities = new[] { "IssueSiteAccess", "IssueWhiteCard", "VerifySafety" }
            }
        };

        foreach (var entity in entities)
        {
            _trustedEntities[entity.EntityId] = entity;
            var cert = GenerateMockCertificate(entity.Name, entity.EntityId);
            _trustedCertificates[entity.EntityId] = cert;
        }
    }

    private void AddMockHealthcareProviders()
    {
        var entities = new[]
        {
            new TrustedEntity
            {
                EntityId = "HEALTH-001",
                Name = "Royal Perth Hospital",
                Type = EntityType.Healthcare,
                Country = "AU",
                State = "WA",
                TrustLevel = TrustLevel.High,
                ValidFrom = DateTime.UtcNow.AddYears(-1),
                ValidTo = DateTime.UtcNow.AddYears(3),
                Capabilities = new[] { "IssueHealthRecord", "IssueVaccination", "VerifyMedical" }
            },
            new TrustedEntity
            {
                EntityId = "HEALTH-002",
                Name = "Australian Medical Association WA",
                Type = EntityType.Healthcare,
                Country = "AU",
                State = "WA",
                TrustLevel = TrustLevel.High,
                ValidFrom = DateTime.UtcNow.AddYears(-2),
                ValidTo = DateTime.UtcNow.AddYears(5),
                Capabilities = new[] { "IssuePractitionerLicense", "VerifyQualification" }
            }
        };

        foreach (var entity in entities)
        {
            _trustedEntities[entity.EntityId] = entity;
            var cert = GenerateMockCertificate(entity.Name, entity.EntityId);
            _trustedCertificates[entity.EntityId] = cert;
        }
    }

    private void AddMockEducationalInstitutions()
    {
        var entities = new[]
        {
            new TrustedEntity
            {
                EntityId = "EDU-001",
                Name = "University of Western Australia",
                Type = EntityType.Education,
                Country = "AU",
                State = "WA",
                TrustLevel = TrustLevel.High,
                ValidFrom = DateTime.UtcNow.AddYears(-5),
                ValidTo = DateTime.UtcNow.AddYears(10),
                Capabilities = new[] { "IssueDegree", "IssueTranscript", "VerifyEnrollment" }
            },
            new TrustedEntity
            {
                EntityId = "EDU-002",
                Name = "TAFE Western Australia",
                Type = EntityType.Education,
                Country = "AU",
                State = "WA",
                TrustLevel = TrustLevel.High,
                ValidFrom = DateTime.UtcNow.AddYears(-3),
                ValidTo = DateTime.UtcNow.AddYears(7),
                Capabilities = new[] { "IssueCertificate", "IssueDiploma", "VerifyCompetency" }
            }
        };

        foreach (var entity in entities)
        {
            _trustedEntities[entity.EntityId] = entity;
            var cert = GenerateMockCertificate(entity.Name, entity.EntityId);
            _trustedCertificates[entity.EntityId] = cert;
        }
    }

    private X509Certificate2 GenerateMockCertificate(string subjectName, string entityId)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={subjectName}, O=Mock Trust List, C=AU",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add extensions
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyCertSign,
                true));

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, false, 0, true));

        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        // Add custom extension for entity ID
        var entityIdExtension = new X509Extension(
            new Oid("1.3.6.1.4.1.99999.1"), // Mock OID for entity ID
            System.Text.Encoding.UTF8.GetBytes(entityId),
            false);
        request.CertificateExtensions.Add(entityIdExtension);

        // Create self-signed certificate
        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(5));

        return cert;
    }

    public async Task<bool> IsEntityTrustedAsync(string entityId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate network delay

        if (_trustedEntities.TryGetValue(entityId, out var entity))
        {
            var isTrusted = entity.ValidFrom <= DateTime.UtcNow && entity.ValidTo >= DateTime.UtcNow;
            _logger.LogInformation("Entity {EntityId} trust check: {IsTrusted}", entityId, isTrusted);
            return isTrusted;
        }

        _logger.LogWarning("Entity {EntityId} not found in trust list", entityId);
        return false;
    }

    public async Task<TrustVerificationResult> VerifyCertificateAsync(
        X509Certificate2 certificate,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(20, cancellationToken); // Simulate verification delay

        var result = new TrustVerificationResult
        {
            Certificate = certificate,
            CheckedAt = DateTime.UtcNow
        };

        // Check if certificate is in revocation list
        if (_revokedCertificates.Contains(certificate.Thumbprint))
        {
            result.IsTrusted = false;
            result.Reason = "Certificate has been revoked";
            result.TrustLevel = TrustLevel.None;
            _logger.LogWarning("Certificate {Thumbprint} is revoked", certificate.Thumbprint);
            return result;
        }

        // Check if issuer is trusted
        var issuerName = certificate.Issuer;
        var trustedEntity = _trustedEntities.Values
            .FirstOrDefault(e => certificate.Issuer.Contains(e.Name));

        if (trustedEntity != null)
        {
            result.IsTrusted = true;
            result.TrustLevel = trustedEntity.TrustLevel;
            result.EntityId = trustedEntity.EntityId;
            result.EntityName = trustedEntity.Name;
            result.Capabilities = trustedEntity.Capabilities.ToList();

            _logger.LogInformation("Certificate {Thumbprint} verified as trusted, issued by {Entity}",
                certificate.Thumbprint, trustedEntity.Name);
        }
        else
        {
            result.IsTrusted = false;
            result.Reason = "Issuer not in trust list";
            result.TrustLevel = TrustLevel.None;

            _logger.LogWarning("Certificate {Thumbprint} issuer not found in trust list", certificate.Thumbprint);
        }

        return result;
    }

    public async Task<IEnumerable<TrustedEntity>> GetTrustedEntitiesAsync(
        EntityType? type = null,
        string? country = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(15, cancellationToken); // Simulate query delay

        var query = _trustedEntities.Values.AsEnumerable();

        if (type.HasValue)
        {
            query = query.Where(e => e.Type == type.Value);
        }

        if (!string.IsNullOrEmpty(country))
        {
            query = query.Where(e => e.Country == country);
        }

        var results = query.ToList();
        _logger.LogInformation("Retrieved {Count} trusted entities (type: {Type}, country: {Country})",
            results.Count, type, country);

        return results;
    }

    public async Task<bool> AddToTrustListAsync(
        TrustedEntity entity,
        X509Certificate2 certificate,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(30, cancellationToken); // Simulate add operation delay

        if (_trustedEntities.ContainsKey(entity.EntityId))
        {
            _logger.LogWarning("Entity {EntityId} already exists in trust list", entity.EntityId);
            return false;
        }

        _trustedEntities[entity.EntityId] = entity;
        _trustedCertificates[entity.EntityId] = certificate;

        _logger.LogInformation("Added entity {EntityId} ({Name}) to trust list",
            entity.EntityId, entity.Name);

        return true;
    }

    public async Task<bool> RemoveFromTrustListAsync(string entityId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(25, cancellationToken); // Simulate remove operation delay

        if (_trustedEntities.Remove(entityId))
        {
            _trustedCertificates.Remove(entityId);
            _logger.LogInformation("Removed entity {EntityId} from trust list", entityId);
            return true;
        }

        _logger.LogWarning("Entity {EntityId} not found for removal", entityId);
        return false;
    }

    public async Task<bool> RevokeCertificateAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        await Task.Delay(20, cancellationToken); // Simulate revocation delay

        if (!_revokedCertificates.Contains(thumbprint))
        {
            _revokedCertificates.Add(thumbprint);
            _logger.LogInformation("Certificate {Thumbprint} has been revoked", thumbprint);
            return true;
        }

        _logger.LogWarning("Certificate {Thumbprint} was already revoked", thumbprint);
        return false;
    }

    public async Task<TrustListStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate statistics calculation

        var stats = new TrustListStatistics
        {
            TotalEntities = _trustedEntities.Count,
            TotalCertificates = _trustedCertificates.Count,
            RevokedCertificates = _revokedCertificates.Count,
            EntitiesByType = _trustedEntities.Values
                .GroupBy(e => e.Type)
                .ToDictionary(g => g.Key, g => g.Count()),
            EntitiesByCountry = _trustedEntities.Values
                .GroupBy(e => e.Country)
                .ToDictionary(g => g.Key, g => g.Count()),
            LastUpdated = DateTime.UtcNow
        };

        _logger.LogInformation("Trust list statistics: {Entities} entities, {Certs} certificates, {Revoked} revoked",
            stats.TotalEntities, stats.TotalCertificates, stats.RevokedCertificates);

        return stats;
    }
}

// Supporting types for Trust List Service
public interface ITrustListService
{
    Task<bool> IsEntityTrustedAsync(string entityId, CancellationToken cancellationToken = default);
    Task<TrustVerificationResult> VerifyCertificateAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default);
    Task<IEnumerable<TrustedEntity>> GetTrustedEntitiesAsync(EntityType? type = null, string? country = null, CancellationToken cancellationToken = default);
    Task<bool> AddToTrustListAsync(TrustedEntity entity, X509Certificate2 certificate, CancellationToken cancellationToken = default);
    Task<bool> RemoveFromTrustListAsync(string entityId, CancellationToken cancellationToken = default);
    Task<bool> RevokeCertificateAsync(string thumbprint, CancellationToken cancellationToken = default);
    Task<TrustListStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public class TrustedEntity
{
    public string EntityId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EntityType Type { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? State { get; set; }
    public string? Industry { get; set; }
    public TrustLevel TrustLevel { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string[] Capabilities { get; set; } = Array.Empty<string>();
}

public class TrustVerificationResult
{
    public X509Certificate2 Certificate { get; set; } = null!;
    public bool IsTrusted { get; set; }
    public TrustLevel TrustLevel { get; set; }
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? Reason { get; set; }
    public List<string> Capabilities { get; set; } = new();
    public DateTime CheckedAt { get; set; }
}

public class TrustListStatistics
{
    public int TotalEntities { get; set; }
    public int TotalCertificates { get; set; }
    public int RevokedCertificates { get; set; }
    public Dictionary<EntityType, int> EntitiesByType { get; set; } = new();
    public Dictionary<string, int> EntitiesByCountry { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public enum EntityType
{
    Government,
    Enterprise,
    Healthcare,
    Education,
    Financial,
    Other
}

public enum TrustLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Full = 4
}
