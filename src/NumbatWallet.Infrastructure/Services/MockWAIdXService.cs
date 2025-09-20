using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using System.Security.Cryptography;
using System.Text.Json;

namespace NumbatWallet.Infrastructure.Services;

public interface IWAIdXService
{
    Task<IdentityVerificationResult> VerifyIdentityAsync(IdentityVerificationRequest request, CancellationToken cancellationToken = default);
    Task<DocumentVerificationResult> VerifyDocumentAsync(DocumentVerificationRequest request, CancellationToken cancellationToken = default);
    Task<BiometricVerificationResult> VerifyBiometricsAsync(BiometricVerificationRequest request, CancellationToken cancellationToken = default);
    Task<string> GenerateVerificationTokenAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateVerificationTokenAsync(string token, CancellationToken cancellationToken = default);
}

public class MockWAIdXService : IWAIdXService
{
    private readonly ILogger<MockWAIdXService> _logger;
    private readonly Dictionary<string, MockIdentity> _mockIdentities;
    private readonly Dictionary<string, DateTime> _verificationTokens = new();

    public MockWAIdXService(ILogger<MockWAIdXService> logger)
    {
        _logger = logger;
        _mockIdentities = InitializeMockIdentities();
    }

    public async Task<IdentityVerificationResult> VerifyIdentityAsync(IdentityVerificationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock WA IdX: Verifying identity for {FirstName} {LastName}", request.FirstName, request.LastName);

        // Simulate API delay
        await Task.Delay(Random.Shared.Next(500, 1500), cancellationToken);

        // Check if identity exists in mock database
        var key = $"{request.FirstName?.ToLowerInvariant()}_{request.LastName?.ToLowerInvariant()}";
        if (_mockIdentities.TryGetValue(key, out var identity))
        {
            // Check if details match
            if (identity.DateOfBirth == request.DateOfBirth &&
                identity.DriversLicense == request.DriversLicenseNumber)
            {
                return new IdentityVerificationResult
                {
                    IsVerified = true,
                    VerificationId = Guid.NewGuid().ToString(),
                    VerificationScore = Random.Shared.Next(85, 100),
                    VerifiedAt = DateTime.UtcNow,
                    VerifiedClaims = new Dictionary<string, string>
                    {
                        ["given_name"] = identity.FirstName,
                        ["family_name"] = identity.LastName,
                        ["birthdate"] = identity.DateOfBirth.ToString("yyyy-MM-dd"),
                        ["drivers_license"] = identity.DriversLicense ?? "",
                        ["address"] = identity.Address ?? "",
                        ["wa_id"] = identity.WAId
                    }
                };
            }
        }

        // Failed verification
        return new IdentityVerificationResult
        {
            IsVerified = false,
            VerificationScore = Random.Shared.Next(20, 60),
            ErrorCode = "IDENTITY_NOT_FOUND",
            ErrorMessage = "Unable to verify identity with provided details"
        };
    }

    public async Task<DocumentVerificationResult> VerifyDocumentAsync(DocumentVerificationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock WA IdX: Verifying document type {DocumentType}", request.DocumentType);

        await Task.Delay(Random.Shared.Next(1000, 2000), cancellationToken);

        // Simulate document verification (always pass for valid test documents)
        if (request.DocumentNumber?.StartsWith("TEST", StringComparison.OrdinalIgnoreCase) == true ||
            request.DocumentNumber?.StartsWith("WA", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new DocumentVerificationResult
            {
                IsValid = true,
                DocumentId = Guid.NewGuid().ToString(),
                DocumentType = request.DocumentType,
                ExpiryDate = DateTime.UtcNow.AddYears(5),
                VerifiedAt = DateTime.UtcNow,
                ExtractedData = new Dictionary<string, string>
                {
                    ["document_number"] = request.DocumentNumber,
                    ["document_type"] = request.DocumentType,
                    ["issuing_authority"] = "Government of Western Australia",
                    ["issue_date"] = DateTime.UtcNow.AddYears(-2).ToString("yyyy-MM-dd")
                }
            };
        }

        return new DocumentVerificationResult
        {
            IsValid = false,
            ErrorCode = "INVALID_DOCUMENT",
            ErrorMessage = "Document verification failed"
        };
    }

    public async Task<BiometricVerificationResult> VerifyBiometricsAsync(BiometricVerificationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock WA IdX: Verifying biometrics for user {UserId}", request.UserId);

        await Task.Delay(Random.Shared.Next(800, 1800), cancellationToken);

        // Simulate biometric verification (80% success rate for testing)
        var isSuccess = Random.Shared.Next(1, 11) <= 8;

        return new BiometricVerificationResult
        {
            IsMatch = isSuccess,
            MatchScore = isSuccess ? Random.Shared.Next(85, 99) : Random.Shared.Next(30, 60),
            BiometricType = request.BiometricType,
            VerifiedAt = DateTime.UtcNow,
            LivenessDetected = isSuccess
        };
    }

    public async Task<string> GenerateVerificationTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _verificationTokens[token] = DateTime.UtcNow.AddMinutes(15); // 15 minute expiry

        _logger.LogInformation("Mock WA IdX: Generated verification token for user {UserId}", userId);
        return token;
    }

    public async Task<bool> ValidateVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);

        if (_verificationTokens.TryGetValue(token, out var expiry))
        {
            if (expiry > DateTime.UtcNow)
            {
                _verificationTokens.Remove(token); // One-time use
                return true;
            }
            _verificationTokens.Remove(token); // Clean up expired token
        }

        return false;
    }

    private Dictionary<string, MockIdentity> InitializeMockIdentities()
    {
        return new Dictionary<string, MockIdentity>
        {
            ["john_smith"] = new MockIdentity
            {
                FirstName = "John",
                LastName = "Smith",
                DateOfBirth = new DateTime(1990, 5, 15),
                DriversLicense = "WA123456",
                Address = "123 Main St, Perth WA 6000",
                WAId = "WA-ID-001"
            },
            ["jane_doe"] = new MockIdentity
            {
                FirstName = "Jane",
                LastName = "Doe",
                DateOfBirth = new DateTime(1985, 8, 22),
                DriversLicense = "WA789012",
                Address = "456 George St, Perth WA 6001",
                WAId = "WA-ID-002"
            },
            ["test_user"] = new MockIdentity
            {
                FirstName = "Test",
                LastName = "User",
                DateOfBirth = new DateTime(2000, 1, 1),
                DriversLicense = "TEST12345",
                Address = "789 Test Ave, Perth WA 6002",
                WAId = "WA-ID-TEST"
            }
        };
    }

    private class MockIdentity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? DriversLicense { get; set; }
        public string? Address { get; set; }
        public string WAId { get; set; } = string.Empty;
    }
}

// Request/Response DTOs
public class IdentityVerificationRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? DriversLicenseNumber { get; set; }
    public string? MedicareNumber { get; set; }
}

public class IdentityVerificationResult
{
    public bool IsVerified { get; set; }
    public string? VerificationId { get; set; }
    public int VerificationScore { get; set; }
    public DateTime VerifiedAt { get; set; }
    public Dictionary<string, string> VerifiedClaims { get; set; } = new();
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DocumentVerificationRequest
{
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public byte[]? DocumentImage { get; set; }
}

public class DocumentVerificationResult
{
    public bool IsValid { get; set; }
    public string? DocumentId { get; set; }
    public string? DocumentType { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime VerifiedAt { get; set; }
    public Dictionary<string, string> ExtractedData { get; set; } = new();
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BiometricVerificationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string BiometricType { get; set; } = "facial";
    public byte[]? BiometricData { get; set; }
}

public class BiometricVerificationResult
{
    public bool IsMatch { get; set; }
    public double MatchScore { get; set; }
    public string BiometricType { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }
    public bool LivenessDetected { get; set; }
}