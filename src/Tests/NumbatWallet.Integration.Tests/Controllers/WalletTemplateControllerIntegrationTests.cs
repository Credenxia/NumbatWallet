using System.Net;
using System.Net.Http.Json;
using NumbatWallet.Integration.Tests.TestHarness;

namespace NumbatWallet.Integration.Tests.Controllers;

/// <summary>
/// Integration tests for Wallet Template Controller
/// Tests CRUD operations for wallet templates (Apple Wallet, Google Pay, Web Wallet)
/// </summary>
[Collection("Integration")]
public class WalletTemplateControllerIntegrationTests : IntegrationTestBase
{
    public WalletTemplateControllerIntegrationTests(IntegrationTestFixture fixture) : base(fixture)
    {
        // Set up authentication for tests
        SetBearerToken(GenerateMockToken("test-admin", new[] { "Admin" }));
    }

    [Fact(Skip = "WalletTemplates table not in database schema - POA milestone pending")]
    public async Task GetAllTemplates_ReturnsTemplateList()
    {
        // Arrange
        var endpoint = "/api/v1/wallet-templates";

        // Act
        var response = await GetAsync<List<WalletTemplateDto>>(endpoint);

        // Assert
        response.Should().NotBeNull();
        response.Should().BeOfType<List<WalletTemplateDto>>();
    }

    [Fact(Skip = "WalletTemplates table not in database schema - POA milestone pending")]
    public async Task GetTemplateById_WithValidId_ReturnsTemplate()
    {
        // Arrange - First create a template
        var createRequest = new CreateWalletTemplateRequestDto
        {
            Name = "Test Template",
            Platform = "Apple",
            Configuration = new { PassTypeId = "pass.com.example.test" }
        };

        var createdTemplate = await PostAsync<CreateWalletTemplateRequestDto, WalletTemplateDto>(
            "/api/v1/wallet-templates", createRequest);

        // Act
        var response = await GetAsync<WalletTemplateDto>($"/api/v1/wallet-templates/{createdTemplate!.Id}");

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be(createdTemplate.Id);
        response.Name.Should().Be("Test Template");
    }

    [Fact(Skip = "WalletTemplates table not in database schema - POA milestone pending")]
    public async Task CreateTemplate_WithValidData_ReturnsCreatedTemplate()
    {
        // Arrange
        var request = new CreateWalletTemplateRequestDto
        {
            Name = "Apple Wallet Driver License",
            Platform = "Apple",
            Configuration = new
            {
                PassTypeId = "pass.com.wa.gov.driverslic",
                TeamId = "TEAM123",
                LogoText = "Western Australia",
                BackgroundColor = "rgb(23, 187, 247)"
            }
        };

        // Act
        var response = await PostAsync<CreateWalletTemplateRequestDto, WalletTemplateDto>(
            "/api/v1/wallet-templates", request);

        // Assert
        response.Should().NotBeNull();
        response.Name.Should().Be("Apple Wallet Driver License");
        response.Platform.Should().Be("Apple");
        response.Id.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "WalletTemplates table not in database schema - POA milestone pending")]
    public async Task UpdateTemplate_WithValidData_ReturnsUpdatedTemplate()
    {
        // Arrange - First create a template
        var createRequest = new CreateWalletTemplateRequestDto
        {
            Name = "Original Template",
            Platform = "Google",
            Configuration = new { }
        };

        var createdTemplate = await PostAsync<CreateWalletTemplateRequestDto, WalletTemplateDto>(
            "/api/v1/wallet-templates", createRequest);

        var updateRequest = new UpdateWalletTemplateRequestDto
        {
            Name = "Updated Template",
            Platform = "Google",
            Configuration = new { IssuerId = "3388000000000000000" }
        };

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/v1/wallet-templates/{createdTemplate!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<WalletTemplateDto>(JsonOptions);
        updated!.Name.Should().Be("Updated Template");
    }

    [Fact(Skip = "WalletTemplates table not in database schema - POA milestone pending")]
    public async Task DeleteTemplate_WithValidId_ReturnsNoContent()
    {
        // Arrange - First create a template
        var createRequest = new CreateWalletTemplateRequestDto
        {
            Name = "Template To Delete",
            Platform = "Web",
            Configuration = new { }
        };

        var createdTemplate = await PostAsync<CreateWalletTemplateRequestDto, WalletTemplateDto>(
            "/api/v1/wallet-templates", createRequest);

        // Act
        var response = await Client.DeleteAsync($"/api/v1/wallet-templates/{createdTemplate!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await Client.GetAsync($"/api/v1/wallet-templates/{createdTemplate.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Skip = "WalletTemplates table not in database schema - POA milestone pending")]
    public async Task GetTemplatesByPlatform_ReturnsFilteredTemplates()
    {
        // Arrange - Create templates for different platforms
        await PostAsync<CreateWalletTemplateRequestDto, WalletTemplateDto>(
            "/api/v1/wallet-templates",
            new CreateWalletTemplateRequestDto
            {
                Name = "Apple Template 1",
                Platform = "Apple",
                Configuration = new { }
            });

        await PostAsync<CreateWalletTemplateRequestDto, WalletTemplateDto>(
            "/api/v1/wallet-templates",
            new CreateWalletTemplateRequestDto
            {
                Name = "Google Template 1",
                Platform = "Google",
                Configuration = new { }
            });

        // Act
        var response = await GetAsync<List<WalletTemplateDto>>("/api/v1/wallet-templates?platform=Apple");

        // Assert
        response.Should().NotBeNull();
        response.Should().OnlyContain(t => t.Platform == "Apple");
    }

    [Fact(Skip = "WalletTemplates table not in database schema - POA milestone pending")]
    public async Task CreateTemplate_WithInvalidPlatform_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateWalletTemplateRequestDto
        {
            Name = "Invalid Template",
            Platform = "InvalidPlatform",
            Configuration = new { }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/wallet-templates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

// DTOs for Wallet Template tests
public record WalletTemplateDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public object Configuration { get; init; } = new { };
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateWalletTemplateRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public object Configuration { get; init; } = new { };
}

public record UpdateWalletTemplateRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public object Configuration { get; init; } = new { };
}
