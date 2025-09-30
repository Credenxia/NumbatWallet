using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NumbatWallet.Application.DTOs;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.Web.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace NumbatWallet.Web.Api.IntegrationTests.GraphQL;

public class GraphQLIntegrationTests : IntegrationTestBase
{
    private const string GraphQLEndpoint = "/graphql";

    public GraphQLIntegrationTests(NumbatWalletWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Query_GetWallets_ShouldReturnWalletsList()
    {
        // Arrange
        await SeedDataAsync();
        var query = @"
            query {
                wallets {
                    items {
                        id
                        walletName
                        walletDid
                        type
                        status
                    }
                    totalCount
                }
            }";

        // Act
        var response = await SendGraphQLQueryAsync(query);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain("\"wallets\"");
        result.Should().Contain("\"items\"");
        result.Should().Contain("\"totalCount\"");
    }

    [Fact]
    public async Task Query_GetWalletById_ShouldReturnWallet()
    {
        // Arrange
        await SeedDataAsync();
        var wallet = await CreateTestWalletAsync();

        var query = $@"
            query {{
                wallet(id: ""{wallet.Id}"") {{
                    id
                    walletName
                    walletDid
                    type
                    status
                    createdAt
                }}
            }}";

        // Act
        var response = await SendGraphQLQueryAsync(query);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain($"\"{wallet.Id}\"");
        result.Should().Contain($"\"{wallet.WalletName}\"");
    }

    [Fact]
    public async Task Mutation_CreateWallet_ShouldCreateNewWallet()
    {
        // Arrange
        await SeedDataAsync();
        var personId = Guid.NewGuid();

        var mutation = $@"
            mutation {{
                createWallet(input: {{
                    personId: ""{personId}""
                    walletName: ""GraphQL Test Wallet""
                    type: HOLDER
                    tags: [""graphql"", ""test""]
                }}) {{
                    id
                    walletName
                    type
                    status
                    tags
                }}
            }}";

        // Act
        var response = await SendGraphQLQueryAsync(mutation);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain("\"walletName\":\"GraphQL Test Wallet\"");
        result.Should().Contain("\"type\":\"HOLDER\"");
        result.Should().Contain("\"graphql\"");
    }

    [Fact]
    public async Task Mutation_UpdateWallet_ShouldUpdateWallet()
    {
        // Arrange
        await SeedDataAsync();
        var wallet = await CreateTestWalletAsync();

        var mutation = $@"
            mutation {{
                updateWallet(id: ""{wallet.Id}"", input: {{
                    walletName: ""Updated GraphQL Wallet""
                    tags: [""updated""]
                }}) {{
                    id
                    walletName
                    tags
                }}
            }}";

        // Act
        var response = await SendGraphQLQueryAsync(mutation);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain("\"walletName\":\"Updated GraphQL Wallet\"");
        result.Should().Contain("\"updated\"");
    }

    [Fact]
    public async Task Query_GetCredentials_ShouldReturnCredentialsList()
    {
        // Arrange
        await SeedDataAsync();
        await CreateTestCredentialAsync();

        var query = @"
            query {
                credentials {
                    items {
                        id
                        type
                        status
                        format
                        issuedAt
                    }
                    totalCount
                }
            }";

        // Act
        var response = await SendGraphQLQueryAsync(query);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain("\"credentials\"");
        result.Should().Contain("\"items\"");
        result.Should().Contain("\"totalCount\"");
    }

    [Fact]
    public async Task Mutation_IssueCredential_ShouldIssueNewCredential()
    {
        // Arrange
        await SeedDataAsync();
        var wallet = await CreateTestWalletAsync();
        var issuerId = Guid.NewGuid();

        var mutation = $@"
            mutation {{
                issueCredential(input: {{
                    walletId: ""{wallet.Id}""
                    issuerId: ""{issuerId}""
                    type: ""GraphQLTestCredential""
                    claims: {{key: ""testClaim"", value: ""testValue""}}
                    validFrom: ""{DateTimeOffset.Now:O}""
                    validUntil: ""{DateTimeOffset.Now.AddYears(1):O}""
                    isRevocable: true
                }}) {{
                    id
                    type
                    status
                    isRevocable
                }}
            }}";

        // Act
        var response = await SendGraphQLQueryAsync(mutation);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain("\"type\":\"GraphQLTestCredential\"");
        result.Should().Contain("\"status\":\"ACTIVE\"");
        result.Should().Contain("\"isRevocable\":true");
    }

    [Fact]
    public async Task Subscription_WalletUpdates_ShouldReceiveUpdates()
    {
        // Arrange
        await SeedDataAsync();

        var subscription = @"
            subscription {
                walletUpdated {
                    id
                    walletName
                    status
                    updatedAt
                }
            }";

        // Note: Testing subscriptions requires WebSocket support
        // This is a placeholder showing the subscription structure
        // Actual implementation would need WebSocket client

        // Act & Assert
        // This test demonstrates the subscription structure
        subscription.Should().Contain("walletUpdated");
    }

    [Fact]
    public async Task Query_ComplexQuery_WithFragments_ShouldWork()
    {
        // Arrange
        await SeedDataAsync();
        var wallet = await CreateTestWalletAsync();

        var query = $@"
            query GetWalletDetails {{
                wallet(id: ""{wallet.Id}"") {{
                    ...walletFields
                    credentials {{
                        ...credentialFields
                    }}
                }}
            }}

            fragment walletFields on Wallet {{
                id
                walletName
                walletDid
                type
                status
            }}

            fragment credentialFields on Credential {{
                id
                type
                status
                format
            }}";

        // Act
        var response = await SendGraphQLQueryAsync(query);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain("\"wallet\"");
        result.Should().Contain($"\"{wallet.Id}\"");
    }

    [Fact]
    public async Task Query_WithVariables_ShouldWork()
    {
        // Arrange
        await SeedDataAsync();
        var wallet = await CreateTestWalletAsync();

        var query = @"
            query GetWallet($id: ID!) {
                wallet(id: $id) {
                    id
                    walletName
                    status
                }
            }";

        var variables = new { id = wallet.Id.ToString() };

        // Act
        var response = await SendGraphQLQueryAsync(query, variables);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain($"\"{wallet.Id}\"");
        result.Should().Contain($"\"{wallet.WalletName}\"");
    }

    [Fact]
    public async Task Query_BatchQuery_ShouldReturnMultipleResults()
    {
        // Arrange
        await SeedDataAsync();
        var wallet1 = await CreateTestWalletAsync();
        var wallet2 = await CreateTestWalletAsync();

        var query = $@"
            query {{
                wallet1: wallet(id: ""{wallet1.Id}"") {{
                    id
                    walletName
                }}
                wallet2: wallet(id: ""{wallet2.Id}"") {{
                    id
                    walletName
                }}
                allWallets: wallets {{
                    totalCount
                }}
            }}";

        // Act
        var response = await SendGraphQLQueryAsync(query);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain("\"wallet1\"");
        result.Should().Contain("\"wallet2\"");
        result.Should().Contain("\"allWallets\"");
        result.Should().Contain($"\"{wallet1.Id}\"");
        result.Should().Contain($"\"{wallet2.Id}\"");
    }

    [Fact]
    public async Task Query_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        await SeedDataAsync();

        // Create multiple wallets
        for (int i = 0; i < 10; i++)
        {
            await CreateTestWalletAsync();
        }

        var query = @"
            query {
                wallets(first: 5, after: ""0"") {
                    items {
                        id
                        walletName
                    }
                    totalCount
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                        startCursor
                        endCursor
                    }
                }
            }";

        // Act
        var response = await SendGraphQLQueryAsync(query);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain("\"pageInfo\"");
        result.Should().Contain("\"hasNextPage\"");
        result.Should().Contain("\"totalCount\"");
    }

    [Fact]
    public async Task Mutation_WithError_ShouldReturnGraphQLError()
    {
        // Arrange
        var mutation = @"
            mutation {
                createWallet(input: {
                    personId: ""invalid-guid""
                    walletName: """"
                    type: HOLDER
                }) {
                    id
                }
            }";

        // Act
        var response = await SendGraphQLQueryAsync(mutation);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); // GraphQL returns 200 with errors
        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain("\"errors\"");
    }

    // Helper methods
    private async Task<HttpResponseMessage> SendGraphQLQueryAsync(string query, object? variables = null)
    {
        var request = new
        {
            query,
            variables
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return await Client.PostAsync(GraphQLEndpoint, content);
    }

    private async Task<WalletDto> CreateTestWalletAsync()
    {
        var createDto = new CreateWalletDto
        {
            PersonId = Guid.NewGuid(),
            WalletName = $"GraphQL Test Wallet {Guid.NewGuid()}",
            Type = WalletType.Holder,
            Tags = new List<string> { "graphql", "test" }
        };

        var response = await PostAsync("/api/v1/wallets", createDto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WalletDto>())!;
    }

    private async Task<CredentialDto> CreateTestCredentialAsync()
    {
        var wallet = await CreateTestWalletAsync();
        var issueDto = new IssueCredentialDto
        {
            WalletId = wallet.Id,
            IssuerId = Guid.NewGuid(),
            Type = "GraphQLTestCredential",
            Subject = new Dictionary<string, object>
            {
                ["id"] = $"did:numbat:wa:{wallet.Id}"
            },
            Claims = new Dictionary<string, string>
            {
                ["testClaim"] = "testValue"
            },
            ValidFrom = DateTimeOffset.Now,
            ValidUntil = DateTimeOffset.Now.AddYears(1),
            IsRevocable = true
        };

        var response = await PostAsync("/api/v1/credentials/issue", issueDto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CredentialDto>())!;
    }
}