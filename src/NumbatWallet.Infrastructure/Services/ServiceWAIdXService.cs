using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// Real implementation of WA IdX Service using OIDC/OAuth2
/// Connects to ServiceWA identity provider
/// </summary>
public class ServiceWAIdXService : IWAIdXService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceWAIdXService> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _authority;
    private readonly string _scope;

    public ServiceWAIdXService(
        HttpClient httpClient,
        ILogger<ServiceWAIdXService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Load configuration
        _clientId = configuration["ServiceWA:ClientId"] ?? throw new InvalidOperationException("ServiceWA:ClientId not configured");
        _clientSecret = configuration["ServiceWA:ClientSecret"] ?? throw new InvalidOperationException("ServiceWA:ClientSecret not configured");
        _authority = configuration["ServiceWA:Authority"] ?? "https://identity.wa.gov.au";
        var apiBaseUrl = configuration["ServiceWA:ApiBaseUrl"] ?? "https://api.servicewa.wa.gov.au";
        _scope = configuration["ServiceWA:Scope"] ?? "openid profile identity.verify";

        _httpClient.BaseAddress = new Uri(apiBaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<IdentityVerificationResult> VerifyIdentityAsync(
        IdentityVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying identity for {FirstName} {LastName} via ServiceWA",
                request.FirstName, request.LastName);

            // Get access token
            var token = await GetAccessTokenAsync(cancellationToken);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Prepare verification request
            var verificationRequest = new
            {
                givenName = request.FirstName,
                familyName = request.LastName,
                dateOfBirth = request.DateOfBirth.ToString("yyyy-MM-dd"),
                driversLicense = request.DriversLicenseNumber,
                medicareNumber = request.MedicareNumber,
                verificationType = "full",
                consentGiven = true
            };

            var json = JsonSerializer.Serialize(verificationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Call ServiceWA verification endpoint
            var response = await _httpClient.PostAsync("/v1/identity/verify", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var verificationResponse = JsonSerializer.Deserialize<ServiceWAVerificationResponse>(responseJson);

                return new IdentityVerificationResult
                {
                    IsVerified = verificationResponse?.Status == "verified",
                    VerificationId = verificationResponse?.VerificationId,
                    VerificationScore = verificationResponse?.Score ?? 0,
                    VerifiedAt = DateTime.UtcNow,
                    VerifiedClaims = verificationResponse?.VerifiedAttributes ?? new Dictionary<string, string>(),
                    ErrorCode = verificationResponse?.Status == "verified" ? null : verificationResponse?.ErrorCode,
                    ErrorMessage = verificationResponse?.Status == "verified" ? null : verificationResponse?.ErrorMessage
                };
            }

            _logger.LogWarning("ServiceWA identity verification failed with status {StatusCode}", response.StatusCode);

            return new IdentityVerificationResult
            {
                IsVerified = false,
                ErrorCode = "SERVICE_ERROR",
                ErrorMessage = $"Verification service returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during identity verification");
            return new IdentityVerificationResult
            {
                IsVerified = false,
                ErrorCode = "SYSTEM_ERROR",
                ErrorMessage = "An error occurred during verification"
            };
        }
    }

    public async Task<DocumentVerificationResult> VerifyDocumentAsync(
        DocumentVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying document type {DocumentType} via ServiceWA", request.DocumentType);

            // Get access token
            var token = await GetAccessTokenAsync(cancellationToken);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Prepare document verification request
            var verificationRequest = new
            {
                documentType = request.DocumentType,
                documentNumber = request.DocumentNumber,
                documentImage = request.DocumentImage != null ? Convert.ToBase64String(request.DocumentImage) : null,
                extractData = true,
                validateAuthenticity = true
            };

            var json = JsonSerializer.Serialize(verificationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Call ServiceWA document verification endpoint
            var response = await _httpClient.PostAsync("/v1/document/verify", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var verificationResponse = JsonSerializer.Deserialize<ServiceWADocumentResponse>(responseJson);

                return new DocumentVerificationResult
                {
                    IsValid = verificationResponse?.IsValid ?? false,
                    DocumentId = verificationResponse?.DocumentId,
                    DocumentType = verificationResponse?.DocumentType,
                    ExpiryDate = verificationResponse?.ExpiryDate,
                    VerifiedAt = DateTime.UtcNow,
                    ExtractedData = verificationResponse?.ExtractedData ?? new Dictionary<string, string>(),
                    ErrorCode = verificationResponse?.IsValid == true ? null : verificationResponse?.ErrorCode,
                    ErrorMessage = verificationResponse?.IsValid == true ? null : verificationResponse?.ErrorMessage
                };
            }

            _logger.LogWarning("ServiceWA document verification failed with status {StatusCode}", response.StatusCode);

            return new DocumentVerificationResult
            {
                IsValid = false,
                ErrorCode = "SERVICE_ERROR",
                ErrorMessage = $"Verification service returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during document verification");
            return new DocumentVerificationResult
            {
                IsValid = false,
                ErrorCode = "SYSTEM_ERROR",
                ErrorMessage = "An error occurred during verification"
            };
        }
    }

    public async Task<BiometricVerificationResult> VerifyBiometricsAsync(
        BiometricVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying biometrics for user {UserId} via ServiceWA", request.UserId);

            // Get access token
            var token = await GetAccessTokenAsync(cancellationToken);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Prepare biometric verification request
            var verificationRequest = new
            {
                userId = request.UserId,
                biometricType = request.BiometricType,
                biometricData = request.BiometricData != null ? Convert.ToBase64String(request.BiometricData) : null,
                performLivenessCheck = true,
                matchThreshold = 0.85
            };

            var json = JsonSerializer.Serialize(verificationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Call ServiceWA biometric verification endpoint
            var response = await _httpClient.PostAsync("/v1/biometric/verify", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var verificationResponse = JsonSerializer.Deserialize<ServiceWABiometricResponse>(responseJson);

                return new BiometricVerificationResult
                {
                    IsMatch = verificationResponse?.IsMatch ?? false,
                    MatchScore = verificationResponse?.MatchScore ?? 0,
                    BiometricType = request.BiometricType,
                    VerifiedAt = DateTime.UtcNow,
                    LivenessDetected = verificationResponse?.LivenessDetected ?? false
                };
            }

            _logger.LogWarning("ServiceWA biometric verification failed with status {StatusCode}", response.StatusCode);

            return new BiometricVerificationResult
            {
                IsMatch = false,
                MatchScore = 0,
                BiometricType = request.BiometricType,
                VerifiedAt = DateTime.UtcNow,
                LivenessDetected = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during biometric verification");
            return new BiometricVerificationResult
            {
                IsMatch = false,
                MatchScore = 0,
                BiometricType = request.BiometricType,
                VerifiedAt = DateTime.UtcNow,
                LivenessDetected = false
            };
        }
    }

    public async Task<string> GenerateVerificationTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating verification token for user {UserId}", userId);

            // Get access token
            var token = await GetAccessTokenAsync(cancellationToken);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Request verification token from ServiceWA
            var tokenRequest = new
            {
                userId = userId,
                tokenType = "verification",
                expiryMinutes = 15,
                scope = "identity.verify"
            };

            var json = JsonSerializer.Serialize(tokenRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/token/generate", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonSerializer.Deserialize<ServiceWATokenResponse>(responseJson);
                return tokenResponse?.Token ?? throw new InvalidOperationException("Failed to generate token");
            }

            throw new InvalidOperationException($"Token generation failed with status {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating verification token");
            // Fallback to local token generation
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }
    }

    public async Task<bool> ValidateVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating verification token");

            // Get access token
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Validate token with ServiceWA
            var validationRequest = new { token = token };
            var json = JsonSerializer.Serialize(validationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/token/validate", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var validationResponse = JsonSerializer.Deserialize<ServiceWATokenValidationResponse>(responseJson);
                return validationResponse?.IsValid ?? false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating verification token");
            return false;
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // OAuth2 client credentials flow
        var tokenEndpoint = $"{_authority}/connect/token";

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["scope"] = _scope
        };

        var content = new FormUrlEncodedContent(tokenRequest);
        var response = await _httpClient.PostAsync(tokenEndpoint, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(responseJson);
            return tokenResponse?.AccessToken ?? throw new InvalidOperationException("Failed to obtain access token");
        }

        throw new InvalidOperationException($"Failed to obtain access token: {response.StatusCode}");
    }

    // Response DTOs for ServiceWA API
    private class ServiceWAVerificationResponse
    {
        public string? Status { get; set; }
        public string? VerificationId { get; set; }
        public int Score { get; set; }
        public Dictionary<string, string>? VerifiedAttributes { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private class ServiceWADocumentResponse
    {
        public bool IsValid { get; set; }
        public string? DocumentId { get; set; }
        public string? DocumentType { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public Dictionary<string, string>? ExtractedData { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private class ServiceWABiometricResponse
    {
        public bool IsMatch { get; set; }
        public double MatchScore { get; set; }
        public bool LivenessDetected { get; set; }
    }

    private class ServiceWATokenResponse
    {
        public string? Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    private class ServiceWATokenValidationResponse
    {
        public bool IsValid { get; set; }
        public string? UserId { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    private class OAuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}