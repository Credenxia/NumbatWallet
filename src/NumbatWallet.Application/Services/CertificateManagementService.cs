using System.Security.Cryptography.X509Certificates;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Services;

public class CertificateManagementService : ICertificateManagementService
{
    private readonly ITenantCertificateRepository _certificateRepository;
    private readonly ICurrentTenantService _currentTenantService;

    public CertificateManagementService(
        ITenantCertificateRepository certificateRepository,
        ICurrentTenantService currentTenantService)
    {
        _certificateRepository = certificateRepository;
        _currentTenantService = currentTenantService;
    }

    private Guid GetCurrentTenantId()
    {
        var tenantIdStr = _currentTenantService.TenantId;
        if (string.IsNullOrWhiteSpace(tenantIdStr) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return Guid.Empty;
        }
        return tenantId;
    }

    public async Task<IEnumerable<CertificateDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty)
        {
            return Enumerable.Empty<CertificateDto>();
        }

        var certificates = await _certificateRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        return certificates.Select(MapToDto);
    }

    public async Task<CertificateDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var certificate = await _certificateRepository.GetByIdAsync(id, cancellationToken);
        var tenantId = GetCurrentTenantId();
        if (certificate == null || certificate.TenantId != tenantId)
        {
            return null;
        }

        return MapToDto(certificate);
    }

    public async Task<CertificateDto?> GetByThumbprintAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        var certificate = await _certificateRepository.GetByThumbprintAsync(thumbprint, cancellationToken);
        var tenantId = GetCurrentTenantId();
        if (certificate == null || certificate.TenantId != tenantId)
        {
            return null;
        }

        return MapToDto(certificate);
    }

    public async Task<IEnumerable<CertificateDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty)
        {
            return Enumerable.Empty<CertificateDto>();
        }

        var certificates = await _certificateRepository.GetActiveCertificatesAsync(tenantId, cancellationToken);
        return certificates.Select(MapToDto);
    }

    public async Task<IEnumerable<CertificateDto>> GetExpiringAsync(int daysBeforeExpiry, CancellationToken cancellationToken = default)
    {
        var certificates = await _certificateRepository.GetExpiringCertificatesAsync(daysBeforeExpiry, cancellationToken);
        var tenantId = GetCurrentTenantId();

        return certificates
            .Where(c => c.TenantId == tenantId)
            .Select(MapToDto);
    }

    public async Task<CertificateDto> UploadAsync(CertificateUploadDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("No tenant context available");
        }

        // Parse the certificate
        X509Certificate2 cert;
        try
        {
            // Try Base64 first
            var certBytes = Convert.FromBase64String(dto.CertificateData);
            cert = X509CertificateLoader.LoadCertificate(certBytes);
        }
        catch
        {
            // Try PEM format
            cert = X509Certificate2.CreateFromPem(dto.CertificateData);
        }

        // Check if certificate already exists
        var existingCert = await _certificateRepository.GetByThumbprintAsync(cert.Thumbprint, cancellationToken);
        if (existingCert != null)
        {
            throw new InvalidOperationException($"Certificate with thumbprint {cert.Thumbprint} already exists");
        }

        // Parse purpose
        if (!Enum.TryParse<CertificatePurpose>(dto.Purpose, true, out var purpose))
        {
            throw new ArgumentException($"Invalid certificate purpose: {dto.Purpose}");
        }

        // Create entity
        var certificate = new TenantCertificate(
            tenantId,
            Convert.ToBase64String(cert.Export(X509ContentType.Cert)),
            cert.Thumbprint,
            cert.SubjectName.Name ?? string.Empty,
            cert.IssuerName.Name ?? string.Empty,
            cert.NotBefore,
            cert.NotAfter,
            purpose
        );

        // Set trust level if provided
        if (!string.IsNullOrWhiteSpace(dto.TrustLevel) &&
            Enum.TryParse<CertificateTrustLevel>(dto.TrustLevel, true, out var trustLevel))
        {
            certificate.UpdateTrustLevel(trustLevel);
        }

        await _certificateRepository.AddAsync(certificate, cancellationToken);

        return MapToDto(certificate);
    }

    public async Task RevokeAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var certificate = await _certificateRepository.GetByIdAsync(id, cancellationToken);
        var tenantId = GetCurrentTenantId();
        if (certificate == null || certificate.TenantId != tenantId)
        {
            throw new InvalidOperationException("Certificate not found");
        }

        certificate.Revoke(reason);
        await _certificateRepository.UpdateAsync(certificate, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var certificate = await _certificateRepository.GetByIdAsync(id, cancellationToken);
        var tenantId = GetCurrentTenantId();
        if (certificate == null || certificate.TenantId != tenantId)
        {
            throw new InvalidOperationException("Certificate not found");
        }

        await _certificateRepository.DeleteAsync(certificate, cancellationToken);
    }

    public async Task<bool> ValidateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var certificate = await _certificateRepository.GetByIdAsync(id, cancellationToken);
        var tenantId = GetCurrentTenantId();
        if (certificate == null || certificate.TenantId != tenantId)
        {
            return false;
        }

        // Basic validation
        if (certificate.IsExpired() || certificate.IsRevoked)
        {
            return false;
        }

        try
        {
            // Load and validate X.509 certificate
            var certBytes = Convert.FromBase64String(certificate.CertificateData);
            using var cert = X509CertificateLoader.LoadCertificate(certBytes);

            // Validate dates
            var now = DateTime.UtcNow;
            if (now < cert.NotBefore || now > cert.NotAfter)
            {
                return false;
            }

            // Validate chain (basic check)
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // Simplified for now
            return chain.Build(cert);
        }
        catch
        {
            return false;
        }
    }

    public async Task<CertificateStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty)
        {
            return new CertificateStatisticsDto();
        }

        var certificates = await _certificateRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        var certList = certificates.ToList();
        var now = DateTimeOffset.UtcNow;

        return new CertificateStatisticsDto
        {
            TotalCertificates = certList.Count,
            ActiveCertificates = certList.Count(c => c.IsActive && !c.IsExpired() && !c.IsRevoked),
            ExpiredCertificates = certList.Count(c => c.IsExpired()),
            RevokedCertificates = certList.Count(c => c.IsRevoked),
            ExpiringIn30Days = certList.Count(c =>
                !c.IsExpired() && !c.IsRevoked && c.ValidTo <= now.AddDays(30)),
            ExpiringIn90Days = certList.Count(c =>
                !c.IsExpired() && !c.IsRevoked && c.ValidTo <= now.AddDays(90))
        };
    }

    public async Task<IEnumerable<CertificateDto>> GetFilteredAsync(
        string? searchTerm = null,
        string? purpose = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty)
        {
            return Enumerable.Empty<CertificateDto>();
        }

        var certificates = await _certificateRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        var query = certificates.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(c =>
                c.SubjectDn.ToLowerInvariant().Contains(term) ||
                c.Thumbprint.ToLowerInvariant().Contains(term) ||
                c.IssuerDn.ToLowerInvariant().Contains(term));
        }

        // Apply purpose filter
        if (!string.IsNullOrWhiteSpace(purpose) &&
            Enum.TryParse<CertificatePurpose>(purpose, true, out var purposeEnum))
        {
            query = query.Where(c => c.Purpose == purposeEnum);
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = status.ToLowerInvariant() switch
            {
                "active" => query.Where(c => c.IsActive && !c.IsExpired() && !c.IsRevoked),
                "expired" => query.Where(c => c.IsExpired()),
                "revoked" => query.Where(c => c.IsRevoked),
                "expiring" => query.Where(c => !c.IsExpired() && !c.IsRevoked &&
                    c.ValidTo <= DateTimeOffset.UtcNow.AddDays(30)),
                _ => query
            };
        }

        return query.Select(MapToDto);
    }

    private static CertificateDto MapToDto(TenantCertificate certificate)
    {
        var now = DateTimeOffset.UtcNow;
        var daysUntilExpiry = (int)(certificate.ValidTo - now).TotalDays;

        return new CertificateDto
        {
            Id = certificate.Id,
            TenantId = certificate.TenantId,
            Thumbprint = certificate.Thumbprint,
            SubjectDn = certificate.SubjectDn,
            IssuerDn = certificate.IssuerDn,
            ValidFrom = certificate.ValidFrom,
            ValidTo = certificate.ValidTo,
            IsActive = certificate.IsActive,
            Purpose = certificate.Purpose.ToString(),
            TrustLevel = certificate.TrustLevel.ToString(),
            CreatedAt = certificate.CreatedAt,
            RevokedAt = certificate.RevokedAt,
            RevocationReason = certificate.RevocationReason,
            LastUsedAt = certificate.LastUsedAt,
            UsageCount = certificate.UsageCount,
            SerialNumber = certificate.SerialNumber,
            IsExpired = certificate.IsExpired(),
            IsRevoked = certificate.IsRevoked,
            DaysUntilExpiry = daysUntilExpiry
        };
    }
}
