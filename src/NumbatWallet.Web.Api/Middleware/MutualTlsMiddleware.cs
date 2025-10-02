using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Application.DomainServices;

namespace NumbatWallet.Web.Api.Middleware;

/// <summary>
/// Middleware for mutual TLS (mTLS) authentication using client certificates
/// </summary>
public class MutualTlsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MutualTlsMiddleware> _logger;
    private readonly MutualTlsOptions _options;

    public MutualTlsMiddleware(
        RequestDelegate next,
        ILogger<MutualTlsMiddleware> logger,
        IOptions<MutualTlsOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantCertificateRepository certificateRepository,
        ICertificateTrustStoreRepository trustStoreRepository,
        ICertificateValidationService validationService)
    {
        // Skip mTLS for excluded paths
        if (IsPathExcluded(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Check if client certificate is present
        var clientCert = await GetClientCertificateAsync(context);
        if (clientCert == null)
        {
            if (_options.RequireClientCertificate)
            {
                _logger.LogWarning("Client certificate missing for path: {Path}", context.Request.Path);
                context.Response.StatusCode = 401;
                context.Response.Headers.Append("WWW-Authenticate", "Certificate");
                await context.Response.WriteAsync("Client certificate required");
                return;
            }

            await _next(context);
            return;
        }

        // Validate certificate thumbprint against database
        var thumbprint = clientCert.Thumbprint;
        var tenantCert = await certificateRepository.GetByThumbprintAsync(thumbprint);

        if (tenantCert == null)
        {
            _logger.LogWarning("Unknown client certificate with thumbprint: {Thumbprint}", thumbprint);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Certificate not registered");
            return;
        }

        // Check if certificate is active
        if (!tenantCert.IsActive)
        {
            _logger.LogWarning("Inactive certificate used: {Thumbprint}", thumbprint);
            context.Response.StatusCode = 401;
            context.Response.Headers.Append("WWW-Authenticate", "Certificate");
            await context.Response.WriteAsync("Certificate is inactive");
            return;
        }

        // Check if certificate is expired
        if (tenantCert.IsExpired())
        {
            _logger.LogWarning("Expired certificate used: {Thumbprint}", thumbprint);
            context.Response.StatusCode = 401;
            context.Response.Headers.Append("WWW-Authenticate", "Certificate");
            await context.Response.WriteAsync("Certificate has expired");
            return;
        }

        // Check if certificate is revoked
        if (tenantCert.RevokedAt.HasValue)
        {
            _logger.LogWarning("Revoked certificate used: {Thumbprint}", thumbprint);
            context.Response.StatusCode = 401;
            context.Response.Headers.Append("WWW-Authenticate", "Certificate");
            await context.Response.WriteAsync("Certificate has been revoked");
            return;
        }

        // Check trust level requirement
        if (!string.IsNullOrEmpty(_options.MinimumTrustLevel))
        {
            var minimumTrustLevel = ParseTrustLevel(_options.MinimumTrustLevel);
            if (tenantCert.TrustLevel < minimumTrustLevel)
            {
                _logger.LogWarning("Certificate trust level {ActualLevel} is below required {RequiredLevel}",
                    tenantCert.TrustLevel, minimumTrustLevel);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Certificate trust level insufficient");
                return;
            }
        }

        // Get trust store for tenant
        var trustStore = await trustStoreRepository.GetActiveByTenantIdAsync(tenantCert.TenantId);
        if (trustStore == null)
        {
            _logger.LogWarning("No active trust store for tenant: {TenantId}", tenantCert.TenantId);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Trust store not configured");
            return;
        }

        // Validate certificate chain and revocation status
        if (_options.ValidateCertificateChain)
        {
            var validationResult = await validationService.ValidateCertificateAsync(
                tenantCert,
                trustStore);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Certificate validation failed: {Errors}",
                    string.Join(", ", validationResult.Errors));
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync($"Certificate validation failed: {validationResult.Errors.FirstOrDefault()}");
                return;
            }
        }

        // Add certificate info to context for downstream use
        context.Items["ClientCertificate"] = clientCert;
        context.Items["TenantCertificate"] = tenantCert;
        context.Items["TenantId"] = tenantCert.TenantId;
        context.Items["CertificateThumbprint"] = thumbprint;
        context.Items["CertificateTrustLevel"] = tenantCert.TrustLevel;

        // Update certificate last used timestamp
        tenantCert.UpdateLastUsed();
        await certificateRepository.UpdateAsync(tenantCert);

        _logger.LogInformation("mTLS authentication successful for certificate: {Thumbprint}", thumbprint);

        await _next(context);
    }

    private async Task<X509Certificate2?> GetClientCertificateAsync(HttpContext context)
    {
        // Try to get certificate from connection
        var clientCert = context.Connection.ClientCertificate;
        if (clientCert != null)
        {
            return clientCert;
        }

        // Try to get certificate from header (for proxied scenarios)
        if (_options.AllowCertificateForwarding &&
            context.Request.Headers.TryGetValue(_options.CertificateHeaderName, out var certHeader))
        {
            try
            {
                var certBytes = Convert.FromBase64String(certHeader.ToString());
                return X509CertificateLoader.LoadCertificate(certBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse forwarded certificate");
            }
        }

        // Try to renegotiate for client certificate
        if (_options.AllowRenegotiation)
        {
            try
            {
                await context.Connection.GetClientCertificateAsync();
                return context.Connection.ClientCertificate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to renegotiate for client certificate");
            }
        }

        return null;
    }

    private bool IsPathExcluded(PathString path)
    {
        return _options.ExcludedPaths.Any(p => path.StartsWithSegments(p));
    }

    private static Domain.Entities.CertificateTrustLevel ParseTrustLevel(string level)
    {
        return level.ToLowerInvariant() switch
        {
            "low" => Domain.Entities.CertificateTrustLevel.Low,
            "medium" => Domain.Entities.CertificateTrustLevel.Medium,
            "high" => Domain.Entities.CertificateTrustLevel.High,
            _ => Domain.Entities.CertificateTrustLevel.Low
        };
    }
}

public class MutualTlsOptions
{
    /// <summary>
    /// Whether to require client certificates for all requests (except excluded paths)
    /// </summary>
    public bool RequireClientCertificate { get; set; } = false;

    /// <summary>
    /// Whether to validate the complete certificate chain
    /// </summary>
    public bool ValidateCertificateChain { get; set; } = true;

    /// <summary>
    /// Whether to allow certificate forwarding from proxy headers
    /// </summary>
    public bool AllowCertificateForwarding { get; set; } = false;

    /// <summary>
    /// Header name for forwarded certificates
    /// </summary>
    public string CertificateHeaderName { get; set; } = "X-Client-Cert";

    /// <summary>
    /// Whether to allow TLS renegotiation to request client certificate
    /// </summary>
    public bool AllowRenegotiation { get; set; } = false;

    /// <summary>
    /// Paths that don't require mTLS authentication
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/swagger",
        "/.well-known",
        "/api/v1/auth/token"
    };

    /// <summary>
    /// Minimum trust level required for certificates
    /// </summary>
    public string MinimumTrustLevel { get; set; } = "Low";
}
