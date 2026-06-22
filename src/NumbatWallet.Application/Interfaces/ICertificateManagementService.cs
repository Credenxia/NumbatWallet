using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for managing tenant certificates (upload, revoke, validate)
/// </summary>
public interface ICertificateManagementService
{
    /// <summary>
    /// Get all certificates for the current tenant
    /// </summary>
    Task<IEnumerable<CertificateDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a certificate by ID
    /// </summary>
    Task<CertificateDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a certificate by thumbprint
    /// </summary>
    Task<CertificateDto?> GetByThumbprintAsync(string thumbprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active certificates for the current tenant
    /// </summary>
    Task<IEnumerable<CertificateDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get certificates expiring within the specified days
    /// </summary>
    Task<IEnumerable<CertificateDto>> GetExpiringAsync(int daysBeforeExpiry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload and parse a new certificate
    /// </summary>
    Task<CertificateDto> UploadAsync(CertificateUploadDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a certificate
    /// </summary>
    Task RevokeAsync(Guid id, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a certificate
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate certificate chain and revocation status
    /// </summary>
    Task<bool> ValidateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get certificate statistics for the current tenant
    /// </summary>
    Task<CertificateStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get filtered certificates
    /// </summary>
    Task<IEnumerable<CertificateDto>> GetFilteredAsync(
        string? searchTerm = null,
        string? purpose = null,
        string? status = null,
        CancellationToken cancellationToken = default);
}
