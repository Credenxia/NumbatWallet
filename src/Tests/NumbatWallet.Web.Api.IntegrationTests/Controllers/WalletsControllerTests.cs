using System.Net;
using System.Net.Http.Json;
using NumbatWallet.Application.DTOs;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.Web.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace NumbatWallet.Web.Api.IntegrationTests.Controllers;

public class WalletsControllerTests : IntegrationTestBase
{
    public WalletsControllerTests(NumbatWalletWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetWallets_ShouldReturnPagedResult()
    {
        // Arrange
        await SeedDataAsync();

        // Act
        var response = await Client.GetAsync("/api/v1/wallets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<WalletDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetWallet_WithValidId_ShouldReturnWallet()
    {
        // Arrange
        await SeedDataAsync();
        var createDto = new CreateWalletDto
        {
            PersonId = Guid.NewGuid(),
            WalletName = "Test Wallet",
            Type = WalletType.Holder,
            Tags = new List<string> { "test" }
        };

        var createResponse = await PostAsync("/api/v1/wallets", createDto);
        createResponse.EnsureSuccessStatusCode();
        var wallet = await createResponse.Content.ReadFromJsonAsync<WalletDto>();

        // Act
        var response = await Client.GetAsync($"/api/v1/wallets/{wallet!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WalletDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(wallet.Id);
        result.WalletName.Should().Be("Test Wallet");
    }

    [Fact]
    public async Task GetWallet_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/v1/wallets/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateWallet_WithValidData_ShouldReturnCreatedWallet()
    {
        // Arrange
        await SeedDataAsync();
        var createDto = new CreateWalletDto
        {
            PersonId = Guid.NewGuid(),
            WalletName = "New Test Wallet",
            Type = WalletType.Issuer,
            Tags = new List<string> { "integration", "test" }
        };

        // Act
        var response = await PostAsync("/api/v1/wallets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<WalletDto>();
        result.Should().NotBeNull();
        result!.WalletName.Should().Be("New Test Wallet");
        result.Type.Should().Be(WalletType.Issuer);
        result.Status.Should().Be(WalletStatus.Active);
        result.Tags.Should().Contain("integration");
        result.Tags.Should().Contain("test");
    }

    [Fact]
    public async Task CreateWallet_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreateWalletDto
        {
            PersonId = Guid.Empty, // Invalid
            WalletName = "", // Invalid
            Type = WalletType.Holder,
            Tags = new List<string>()
        };

        // Act
        var response = await PostAsync("/api/v1/wallets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateWallet_WithValidData_ShouldReturnUpdatedWallet()
    {
        // Arrange
        await SeedDataAsync();
        var createDto = new CreateWalletDto
        {
            PersonId = Guid.NewGuid(),
            WalletName = "Original Wallet",
            Type = WalletType.Holder,
            Tags = new List<string> { "original" }
        };

        var createResponse = await PostAsync("/api/v1/wallets", createDto);
        createResponse.EnsureSuccessStatusCode();
        var wallet = await createResponse.Content.ReadFromJsonAsync<WalletDto>();

        var updateDto = new UpdateWalletDto
        {
            WalletName = "Updated Wallet",
            Tags = new List<string> { "updated" },
            Settings = new Dictionary<string, object>
            {
                ["theme"] = "dark",
                ["notifications"] = true
            }
        };

        // Act
        var response = await PutAsync($"/api/v1/wallets/{wallet!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WalletDto>();
        result.Should().NotBeNull();
        result!.WalletName.Should().Be("Updated Wallet");
        result.Tags.Should().Contain("updated");
    }

    [Fact]
    public async Task UpdateWallet_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateDto = new UpdateWalletDto
        {
            WalletName = "Updated Wallet",
            Tags = new List<string> { "updated" }
        };

        // Act
        var response = await PutAsync($"/api/v1/wallets/{nonExistentId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteWallet_WithExistingWallet_ShouldReturnNoContent()
    {
        // Arrange
        await SeedDataAsync();
        var createDto = new CreateWalletDto
        {
            PersonId = Guid.NewGuid(),
            WalletName = "Wallet to Delete",
            Type = WalletType.Holder,
            Tags = new List<string> { "delete" }
        };

        var createResponse = await PostAsync("/api/v1/wallets", createDto);
        createResponse.EnsureSuccessStatusCode();
        var wallet = await createResponse.Content.ReadFromJsonAsync<WalletDto>();

        // Act
        var response = await DeleteAsync($"/api/v1/wallets/{wallet!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await Client.GetAsync($"/api/v1/wallets/{wallet.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteWallet_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await DeleteAsync($"/api/v1/wallets/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWalletStatistics_ShouldReturnStatistics()
    {
        // Arrange
        await SeedDataAsync();
        var createDto = new CreateWalletDto
        {
            PersonId = Guid.NewGuid(),
            WalletName = "Stats Test Wallet",
            Type = WalletType.Holder,
            Tags = new List<string> { "stats" }
        };

        var createResponse = await PostAsync("/api/v1/wallets", createDto);
        createResponse.EnsureSuccessStatusCode();
        var wallet = await createResponse.Content.ReadFromJsonAsync<WalletDto>();

        // Act
        var response = await Client.GetAsync($"/api/v1/wallets/{wallet!.Id}/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WalletStatisticsDto>();
        result.Should().NotBeNull();
        result!.WalletId.Should().Be(wallet.Id);
        result.TotalCredentials.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task SearchWallets_WithQuery_ShouldReturnFilteredResults()
    {
        // Arrange
        await SeedDataAsync();

        // Create multiple wallets
        for (int i = 0; i < 5; i++)
        {
            var createDto = new CreateWalletDto
            {
                PersonId = Guid.NewGuid(),
                WalletName = $"Search Test {i}",
                Type = i % 2 == 0 ? WalletType.Holder : WalletType.Issuer,
                Tags = new List<string> { "search", $"tag{i}" }
            };
            await PostAsync("/api/v1/wallets", createDto);
        }

        // Act
        var response = await Client.GetAsync("/api/v1/wallets/search?query=Search Test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDto<WalletDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterOrEqualTo(5);
        result.Items.Should().OnlyContain(w => w.WalletName.Contains("Search Test"));
    }

    [Fact]
    public async Task ExportWalletData_ShouldReturnWalletData()
    {
        // Arrange
        await SeedDataAsync();
        var createDto = new CreateWalletDto
        {
            PersonId = Guid.NewGuid(),
            WalletName = "Export Test Wallet",
            Type = WalletType.Holder,
            Tags = new List<string> { "export" }
        };

        var createResponse = await PostAsync("/api/v1/wallets", createDto);
        createResponse.EnsureSuccessStatusCode();
        var wallet = await createResponse.Content.ReadFromJsonAsync<WalletDto>();

        // Act
        var response = await Client.GetAsync($"/api/v1/wallets/{wallet!.Id}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain(wallet.Id.ToString());
    }
}