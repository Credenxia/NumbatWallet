using System.Net;
using System.Net.Http.Json;
using NumbatWallet.Integration.Tests.TestHarness;

namespace NumbatWallet.Integration.Tests.Controllers;

/// <summary>
/// Integration tests for Wallet Template Controller (POA-200 template builder).
/// Exercises the REAL API contract: CreateTemplateRequest(Name, Description, Type,
/// SupportedCredentialTypes, Fields) where Type is the WalletTemplateType enum
/// (DriverLicense, Passport, ..., Custom) — not the legacy Name/Platform/Configuration shape
/// these tests were originally written (and skipped) against.
/// </summary>
[Collection("Integration")]
public class WalletTemplateControllerIntegrationTests : IntegrationTestBase
{
    public WalletTemplateControllerIntegrationTests(IntegrationTestFixture fixture) : base(fixture)
    {
        // Endpoints require the AdminOnly policy
        SetBearerToken(GenerateMockToken("test-admin", new[] { "Admin" }));
    }

    private static object BuildCreateRequest(string name, string type = "Custom") => new
    {
        Name = name,
        Description = $"Integration test template: {name}",
        Type = type,
        SupportedCredentialTypes = new List<string> { "DriverLicense" },
        Fields = new List<object>
        {
            new
            {
                Name = "fullName",
                Label = "Full Name",
                FieldType = "text",
                IsRequired = true,
                IsEditable = false,
                MappedCredentialField = (string?)null,
                ValidationRule = (string?)null,
                DefaultValue = (string?)null,
                DisplayOrder = 1
            }
        }
    };

    [Fact]
    public async Task GetAllTemplates_ReturnsTemplateList()
    {
        // Arrange
        var endpoint = "/api/v1/wallet-templates";

        // Act
        var response = await GetAsync<List<WalletTemplateResponseDto>>(endpoint);

        // Assert
        response.Should().NotBeNull();
        response.Should().BeOfType<List<WalletTemplateResponseDto>>();
    }

    [Fact]
    public async Task GetTemplateById_WithValidId_ReturnsTemplate()
    {
        // Arrange - First create a template
        var createdTemplate = await PostAsync<object, WalletTemplateResponseDto>(
            "/api/v1/wallet-templates", BuildCreateRequest("Test Template"));

        // Act
        var response = await GetAsync<WalletTemplateResponseDto>(
            $"/api/v1/wallet-templates/{createdTemplate!.Id}");

        // Assert
        response.Should().NotBeNull();
        response!.Id.Should().Be(createdTemplate.Id);
        response.Name.Should().Be("Test Template");
    }

    [Fact]
    public async Task CreateTemplate_WithValidData_ReturnsCreatedTemplate()
    {
        // Arrange
        var request = BuildCreateRequest("Apple Wallet Driver License", type: "DriverLicense");

        // Act
        var response = await PostAsync<object, WalletTemplateResponseDto>(
            "/api/v1/wallet-templates", request);

        // Assert
        response.Should().NotBeNull();
        response!.Name.Should().Be("Apple Wallet Driver License");
        response.Type.Should().Be("DriverLicense");
        response.Id.Should().NotBeEmpty();
        response.SupportedCredentialTypes.Should().Contain("DriverLicense");
    }

    [Fact]
    public async Task UpdateTemplate_WithValidData_ReturnsUpdatedTemplate()
    {
        // Arrange - First create a template
        var createdTemplate = await PostAsync<object, WalletTemplateResponseDto>(
            "/api/v1/wallet-templates", BuildCreateRequest("Original Template"));

        // NOTE: UpdateTemplate only mutates Fields and SupportedCredentialTypes
        // (core properties of WalletTemplate are immutable).
        var updateRequest = new
        {
            Name = "Original Template",
            Description = "updated",
            SupportedCredentialTypes = new List<string> { "ProofOfAge" },
            Fields = new List<object>
            {
                new
                {
                    Name = "dateOfBirth",
                    Label = "Date of Birth",
                    FieldType = "date",
                    IsRequired = true,
                    IsEditable = false,
                    MappedCredentialField = (string?)null,
                    ValidationRule = (string?)null,
                    DefaultValue = (string?)null,
                    DisplayOrder = 1
                }
            }
        };

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/v1/wallet-templates/{createdTemplate!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<WalletTemplateResponseDto>(JsonOptions);
        updated!.SupportedCredentialTypes.Should().ContainSingle().Which.Should().Be("ProofOfAge");
    }

    [Fact]
    public async Task DeleteTemplate_WithValidId_ReturnsNoContent()
    {
        // Arrange - First create a template
        var createdTemplate = await PostAsync<object, WalletTemplateResponseDto>(
            "/api/v1/wallet-templates", BuildCreateRequest("Template To Delete"));

        // Act
        var response = await Client.DeleteAsync($"/api/v1/wallet-templates/{createdTemplate!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await Client.GetAsync($"/api/v1/wallet-templates/{createdTemplate.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTemplatesByTenant_ReturnsTenantTemplates()
    {
        // Arrange - Create templates of different types for the test tenant
        // (the API filters by tenant, not by platform — platform filtering does not exist)
        var apple = await PostAsync<object, WalletTemplateResponseDto>(
            "/api/v1/wallet-templates", BuildCreateRequest("DL Template", type: "DriverLicense"));
        var custom = await PostAsync<object, WalletTemplateResponseDto>(
            "/api/v1/wallet-templates", BuildCreateRequest("Custom Template", type: "Custom"));

        // Act
        var response = await GetAsync<List<WalletTemplateResponseDto>>(
            $"/api/v1/wallet-templates/tenant/{Fixture.TestTenantId}");

        // Assert
        response.Should().NotBeNull();
        response!.Select(t => t.Id).Should().Contain(new[] { apple!.Id, custom!.Id });
    }

    [Fact]
    public async Task CreateTemplate_WithInvalidType_ReturnsBadRequest()
    {
        // Arrange - Type is not a valid WalletTemplateType enum value
        var request = BuildCreateRequest("Invalid Template", type: "InvalidPlatform");

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/wallet-templates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

// Response DTO matching the serialized WalletTemplate entity (enums as strings)
public record WalletTemplateResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public List<string> SupportedCredentialTypes { get; init; } = new();
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
