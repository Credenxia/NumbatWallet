using System.Net;
using System.Net.Http.Json;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Integration.Tests.TestHarness;

namespace NumbatWallet.Integration.Tests.Controllers;

/// <summary>
/// Integration tests for Credential Controller
/// </summary>
[Collection("Integration")]
public class CredentialControllerIntegrationTests : IntegrationTestBase
{
    public CredentialControllerIntegrationTests(IntegrationTestFixture fixture) : base(fixture)
    {
        // Set up authentication for tests
        SetBearerToken(GenerateMockToken("test-issuer", new[] { "Issuer", "Admin" }));
    }

    [Fact]
    public async Task IssueCredential_WithValidRequest_ReturnsCreatedCredential()
    {
        // Arrange - Use seeded test data
        var walletId = await TestData.GetFirstWalletIdAsync();
        var issuerId = await TestData.GetFirstIssuerIdAsync();

        var request = new IssueCredentialRequestDto
        {
            WalletId = walletId,
            IssuerId = issuerId,
            CredentialType = "DriversLicense",
            Subject = "Test Credential Subject",
            Claims = new Dictionary<string, object>
            {
                ["firstName"] = "John",
                ["lastName"] = "Doe",
                ["licenseNumber"] = "DL123456",
                ["dateOfBirth"] = "1990-01-01"
            },
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var response = await PostAsync<IssueCredentialRequestDto, CredentialDto>("/api/v1/credential/issue", request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().NotBeNullOrEmpty();
        response.HolderId.Should().Be(walletId.ToString());
        response.Type.Should().Be("DriversLicense"); // Fixed: enum name is DriversLicense
        response.Status.Should().Be("Active");
        response.CredentialSubject.Should().ContainKey("firstName");
    }

    [Fact]
    public async Task GetCredentialById_WithExistingId_ReturnsCredential()
    {
        // Arrange - First issue a credential using seeded data
        var walletId = await TestData.GetFirstWalletIdAsync();
        var issuerId = await TestData.GetFirstIssuerIdAsync();

        var issueRequest = new IssueCredentialRequestDto
        {
            WalletId = walletId,
            IssuerId = issuerId,
            CredentialType = "Passport",
            Subject = "Test Passport",
            Claims = new Dictionary<string, object>
            {
                ["passportNumber"] = "P123456789",
                ["country"] = "Australia"
            }
        };

        var issuedCredential = await PostAsync<IssueCredentialRequestDto, CredentialDto>("/api/v1/credential/issue", issueRequest);
        var credentialId = Guid.Parse(issuedCredential!.Id);

        // Act
        var response = await GetAsync<CredentialDto>($"/api/v1/credential/{credentialId}");

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be(credentialId.ToString());
        response.Type.Should().Be("Passport");
    }

    [Fact]
    public async Task VerifyCredential_WithValidCredential_ReturnsVerificationResult()
    {
        // Arrange
        var request = new VerifyCredentialRequestDto
        {
            CredentialData = "mock-jwt-vc-token",
            VerificationOptions = new VerificationOptionsDto
            {
                CheckExpiry = true,
                CheckRevocation = true
            }
        };

        // Act
        var response = await PostAsync<VerifyCredentialRequestDto, VerificationResultDto>("/api/v1/credential/verify", request);

        // Assert
        response.Should().NotBeNull();
        response.IsValid.Should().BeTrue();
        response.Checks.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCredentialsByWallet_ReturnsWalletCredentials()
    {
        // Arrange - Use seeded test data
        var walletId = await TestData.GetFirstWalletIdAsync();
        var issuerId = await TestData.GetFirstIssuerIdAsync();

        // Issue multiple credentials for the wallet
        var credentialTypes = new[] { "DriversLicense", "Passport", "StudentId" };
        for (int i = 0; i < 3; i++)
        {
            await PostAsync<IssueCredentialRequestDto, CredentialDto>("/api/v1/credential/issue", new IssueCredentialRequestDto
            {
                WalletId = walletId,
                IssuerId = issuerId,
                CredentialType = credentialTypes[i],
                Subject = $"Test Subject {i}",
                Claims = new Dictionary<string, object> { ["index"] = i }
            });
        }

        // Act
        var response = await GetAsync<List<CredentialDto>>($"/api/v1/credential/wallet/{walletId}");

        // Assert
        response.Should().NotBeNull();
        response.Should().HaveCountGreaterThanOrEqualTo(3);
        response.Should().OnlyContain(c => c.HolderId == walletId.ToString());
    }

    [Fact]
    public async Task RevokeCredential_WithValidId_ReturnsSuccess()
    {
        // Arrange - First issue a credential using seeded data
        var walletId = await TestData.GetFirstWalletIdAsync();
        var issuerId = await TestData.GetFirstIssuerIdAsync();

        var issuedCredential = await PostAsync<IssueCredentialRequestDto, CredentialDto>("/api/v1/credential/issue", new IssueCredentialRequestDto
        {
            WalletId = walletId,
            IssuerId = issuerId,
            CredentialType = "HealthCard",
            Subject = "To Be Revoked",
            Claims = new Dictionary<string, object>()
        });

        var credentialId = Guid.Parse(issuedCredential!.Id);

        // Act
        var revokeRequest = new { Reason = "Test revocation" };
        var response = await Client.PostAsJsonAsync($"/api/v1/credential/{credentialId}/revoke", revokeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the credential is revoked
        var revokedCredential = await GetAsync<CredentialDto>($"/api/v1/credential/{credentialId}");
        revokedCredential!.Status.Should().Be("Revoked");
    }

    [Fact]
    public async Task IssueCredential_WithInvalidType_ReturnsBadRequest()
    {
        // Arrange - Use seeded wallet but invalid credential type
        var walletId = await TestData.GetFirstWalletIdAsync();
        var issuerId = await TestData.GetFirstIssuerIdAsync();

        var request = new IssueCredentialRequestDto
        {
            WalletId = walletId,
            IssuerId = issuerId,
            CredentialType = "InvalidType",
            Subject = "Test",
            Claims = new Dictionary<string, object>()
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/credential/issue", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCredentialById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/v1/credential/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShareCredential_CreatesShareableLink()
    {
        // Arrange - First issue a credential using seeded data
        var walletId = await TestData.GetFirstWalletIdAsync();
        var issuerId = await TestData.GetFirstIssuerIdAsync();

        var issuedCredential = await PostAsync<IssueCredentialRequestDto, CredentialDto>("/api/v1/credential/issue", new IssueCredentialRequestDto
        {
            WalletId = walletId,
            IssuerId = issuerId,
            CredentialType = "StudentId",
            Subject = "Student ID Card",
            Claims = new Dictionary<string, object>
            {
                ["studentId"] = "S123456",
                ["university"] = "University of WA"
            }
        });

        var credentialId = Guid.Parse(issuedCredential!.Id);

        var request = new ShareCredentialRequestDto
        {
            CredentialId = credentialId,
            RecipientId = "recipient@example.com",
            RecipientEmail = "recipient@example.com",
            Purpose = SharePurpose.Verification,
            ExpiryHours = 24,
            RequireAuthentication = true
        };

        // Act - Use correct endpoint format {id}/share
        var response = await PostAsync<ShareCredentialRequestDto, ShareCredentialResultDto>($"/api/v1/credential/{credentialId}/share", request);

        // Assert
        response.Should().NotBeNull();
        response.ShareUrl.Should().NotBeNullOrEmpty();
        response.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }
}