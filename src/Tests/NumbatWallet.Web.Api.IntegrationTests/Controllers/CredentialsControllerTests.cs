using System.Net;
using System.Net.Http.Json;
using NumbatWallet.Application.DTOs;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.Web.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace NumbatWallet.Web.Api.IntegrationTests.Controllers;

public class CredentialsControllerTests : IntegrationTestBase
{
    public CredentialsControllerTests(NumbatWalletWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetCredentials_ShouldReturnPagedResult()
    {
        // Arrange
        await SeedDataAsync();

        // Act
        var response = await Client.GetAsync("/api/v1/credentials");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<CredentialDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetCredential_WithValidId_ShouldReturnCredential()
    {
        // Arrange
        await SeedDataAsync();
        var credential = await CreateTestCredentialAsync();

        // Act
        var response = await Client.GetAsync($"/api/v1/credentials/{credential.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CredentialDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(credential.Id);
        result.Type.Should().Be(credential.Type);
    }

    [Fact]
    public async Task GetCredential_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/v1/credentials/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task IssueCredential_WithValidData_ShouldReturnIssuedCredential()
    {
        // Arrange
        await SeedDataAsync();
        var wallet = await CreateTestWalletAsync();

        var issueDto = new IssueCredentialDto
        {
            WalletId = wallet.Id,
            IssuerId = Guid.NewGuid(),
            Type = "TestCredential",
            Subject = new Dictionary<string, object>
            {
                ["id"] = $"did:numbat:wa:{wallet.Id}",
                ["name"] = "Test Subject"
            },
            Claims = new Dictionary<string, string>
            {
                ["claim1"] = "value1",
                ["claim2"] = "value2"
            },
            ValidFrom = DateTimeOffset.Now,
            ValidUntil = DateTimeOffset.Now.AddYears(1),
            IsRevocable = true
        };

        // Act
        var response = await PostAsync("/api/v1/credentials/issue", issueDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CredentialDto>();
        result.Should().NotBeNull();
        result!.WalletId.Should().Be(wallet.Id);
        result.Type.Should().Be("TestCredential");
        result.Status.Should().Be(CredentialStatus.Active);
        result.IsRevocable.Should().BeTrue();
        result.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task IssueCredential_WithInvalidWalletId_ShouldReturnBadRequest()
    {
        // Arrange
        var issueDto = new IssueCredentialDto
        {
            WalletId = Guid.Empty, // Invalid
            IssuerId = Guid.NewGuid(),
            Type = "TestCredential",
            Subject = new Dictionary<string, object>(),
            Claims = new Dictionary<string, string>(),
            ValidFrom = DateTimeOffset.Now,
            ValidUntil = DateTimeOffset.Now.AddYears(1),
            IsRevocable = true
        };

        // Act
        var response = await PostAsync("/api/v1/credentials/issue", issueDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyCredential_WithValidCredential_ShouldReturnVerificationResult()
    {
        // Arrange
        await SeedDataAsync();
        var credential = await CreateTestCredentialAsync();

        var verifyDto = new VerifyCredentialDto
        {
            CredentialId = credential.Id,
            VerifierDid = "did:numbat:wa:verifier123",
            Purpose = "Test Verification",
            RequiredClaims = new List<string> { "claim1" }
        };

        // Act
        var response = await PostAsync("/api/v1/credentials/verify", verifyDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VerificationResultDto>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.CredentialId.Should().Be(credential.Id);
    }

    [Fact]
    public async Task RevokeCredential_WithRevocableCredential_ShouldReturnSuccess()
    {
        // Arrange
        await SeedDataAsync();
        var credential = await CreateTestCredentialAsync(isRevocable: true);

        var revokeDto = new RevokeCredentialDto
        {
            CredentialId = credential.Id,
            Reason = "Test revocation",
            RevokedBy = "Test User"
        };

        // Act
        var response = await PostAsync("/api/v1/credentials/revoke", revokeDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CredentialDto>();
        result.Should().NotBeNull();
        result!.IsRevoked.Should().BeTrue();
        result.Status.Should().Be(CredentialStatus.Revoked);
    }

    [Fact]
    public async Task RevokeCredential_WithNonRevocableCredential_ShouldReturnBadRequest()
    {
        // Arrange
        await SeedDataAsync();
        var credential = await CreateTestCredentialAsync(isRevocable: false);

        var revokeDto = new RevokeCredentialDto
        {
            CredentialId = credential.Id,
            Reason = "Test revocation",
            RevokedBy = "Test User"
        };

        // Act
        var response = await PostAsync("/api/v1/credentials/revoke", revokeDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCredentialsByWallet_ShouldReturnWalletCredentials()
    {
        // Arrange
        await SeedDataAsync();
        var wallet = await CreateTestWalletAsync();

        // Create multiple credentials for the wallet
        for (int i = 0; i < 3; i++)
        {
            await CreateTestCredentialAsync(walletId: wallet.Id);
        }

        // Act
        var response = await Client.GetAsync($"/api/v1/credentials/wallet/{wallet.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CredentialDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCountGreaterOrEqualTo(3);
        result.Should().OnlyContain(c => c.WalletId == wallet.Id);
    }

    [Fact]
    public async Task GetCredentialsByIssuer_ShouldReturnIssuerCredentials()
    {
        // Arrange
        await SeedDataAsync();
        var issuerId = Guid.NewGuid();

        // Create multiple credentials from the same issuer
        for (int i = 0; i < 3; i++)
        {
            await CreateTestCredentialAsync(issuerId: issuerId);
        }

        // Act
        var response = await Client.GetAsync($"/api/v1/credentials/issuer/{issuerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CredentialDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCountGreaterOrEqualTo(3);
        result.Should().OnlyContain(c => c.IssuerId == issuerId);
    }

    [Fact]
    public async Task GetExpiredCredentials_ShouldReturnOnlyExpiredCredentials()
    {
        // Arrange
        await SeedDataAsync();

        // Create an expired credential
        var wallet = await CreateTestWalletAsync();
        var issueDto = new IssueCredentialDto
        {
            WalletId = wallet.Id,
            IssuerId = Guid.NewGuid(),
            Type = "ExpiredCredential",
            Subject = new Dictionary<string, object>
            {
                ["id"] = $"did:numbat:wa:{wallet.Id}"
            },
            Claims = new Dictionary<string, string>(),
            ValidFrom = DateTimeOffset.Now.AddYears(-2),
            ValidUntil = DateTimeOffset.Now.AddYears(-1), // Already expired
            IsRevocable = true
        };

        await PostAsync("/api/v1/credentials/issue", issueDto);

        // Act
        var response = await Client.GetAsync("/api/v1/credentials/expired");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CredentialDto>>();
        result.Should().NotBeNull();
        // The expired credential should be in the results
        result!.Should().Contain(c => c.Type == "ExpiredCredential");
    }

    [Fact]
    public async Task RefreshCredential_WithValidCredential_ShouldReturnRefreshedCredential()
    {
        // Arrange
        await SeedDataAsync();
        var credential = await CreateTestCredentialAsync();

        var refreshDto = new RefreshCredentialDto
        {
            CredentialId = credential.Id,
            ExtendValidityDays = 365
        };

        // Act
        var response = await PostAsync("/api/v1/credentials/refresh", refreshDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CredentialDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(credential.Id);
        result.ValidUntil.Should().BeAfter(credential.ValidUntil);
    }

    [Fact]
    public async Task BatchIssueCredentials_ShouldIssueMultipleCredentials()
    {
        // Arrange
        await SeedDataAsync();
        var wallet1 = await CreateTestWalletAsync();
        var wallet2 = await CreateTestWalletAsync();

        var batchDto = new BatchIssueCredentialsDto
        {
            IssuerId = Guid.NewGuid(),
            Type = "BatchCredential",
            Recipients = new List<BatchRecipientDto>
            {
                new BatchRecipientDto
                {
                    WalletId = wallet1.Id,
                    Claims = new Dictionary<string, string> { ["name"] = "User 1" }
                },
                new BatchRecipientDto
                {
                    WalletId = wallet2.Id,
                    Claims = new Dictionary<string, string> { ["name"] = "User 2" }
                }
            },
            ValidFrom = DateTimeOffset.Now,
            ValidUntil = DateTimeOffset.Now.AddYears(1),
            IsRevocable = true
        };

        // Act
        var response = await PostAsync("/api/v1/credentials/batch-issue", batchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BatchIssueResultDto>();
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(0);
        result.IssuedCredentialIds.Should().HaveCount(2);
    }

    // Helper methods
    private async Task<WalletDto> CreateTestWalletAsync()
    {
        var createDto = new CreateWalletDto
        {
            PersonId = Guid.NewGuid(),
            WalletName = $"Test Wallet {Guid.NewGuid()}",
            Type = WalletType.Holder,
            Tags = new List<string> { "test" }
        };

        var response = await PostAsync("/api/v1/wallets", createDto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WalletDto>())!;
    }

    private async Task<CredentialDto> CreateTestCredentialAsync(
        Guid? walletId = null,
        Guid? issuerId = null,
        bool isRevocable = true)
    {
        var wallet = walletId.HasValue ?
            new WalletDto { Id = walletId.Value } :
            await CreateTestWalletAsync();

        var issueDto = new IssueCredentialDto
        {
            WalletId = wallet.Id,
            IssuerId = issuerId ?? Guid.NewGuid(),
            Type = "TestCredential",
            Subject = new Dictionary<string, object>
            {
                ["id"] = $"did:numbat:wa:{wallet.Id}",
                ["name"] = "Test Subject"
            },
            Claims = new Dictionary<string, string>
            {
                ["claim1"] = "value1",
                ["claim2"] = "value2"
            },
            ValidFrom = DateTimeOffset.Now,
            ValidUntil = DateTimeOffset.Now.AddYears(1),
            IsRevocable = isRevocable
        };

        var response = await PostAsync("/api/v1/credentials/issue", issueDto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CredentialDto>())!;
    }
}