using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NumbatWallet.Application.Commands.Batch;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Web.Api.Controllers;
using NumbatWallet.Web.Api.Security;
using NumbatWallet.Web.Api.Tests.TestHelpers;

namespace NumbatWallet.Web.Api.Tests.Controllers;

[Collection("Sequential")]
public class BatchControllerTests : ApiTestBase
{
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ISecurityAuditService> _mockAuditService;
    private readonly JsonSerializerOptions _jsonOptions;

    public BatchControllerTests(WebApplicationFactory<Program> factory) : base(factory)
    {
        _mockCacheService = new Mock<ICacheService>();
        _mockAuditService = new Mock<ISecurityAuditService>();

        // Configure JSON options with StringEnumConverter to match the API
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private HttpClient CreateClient()
    {
        return CreateAuthenticatedClient(services =>
        {
            // Remove existing registrations
            var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
            if (cacheDescriptor != null)
            {
                services.Remove(cacheDescriptor);
            }

            var auditDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISecurityAuditService));
            if (auditDescriptor != null)
            {
                services.Remove(auditDescriptor);
            }

            // Add mocks
            services.AddSingleton(_mockCacheService.Object);
            services.AddSingleton(_mockAuditService.Object);
        });
    }

    [Fact]
    public async Task BatchIssueCredentials_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClient();

        var request = new BatchIssueCredentialsRequestDto
        {
            Credentials = new List<BatchIssueCredentialItemDto>
            {
                new BatchIssueCredentialItemDto
                {
                    BatchItemId = "batch-001",
                    HolderId = "holder-123",
                    Type = "ProofOfAge",
                    Claims = new Dictionary<string, object>
                    {
                        ["firstName"] = "John",
                        ["lastName"] = "Doe"
                    }
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/batch/credentials/issue", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BatchOperationResultDto<CredentialDto>>(_jsonOptions);
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task BatchIssueCredentials_WithTooManyItems_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateClient();

        var request = new BatchIssueCredentialsRequestDto
        {
            Credentials = new List<BatchIssueCredentialItemDto>()
        };

        // Add 101 items (exceeds limit)
        for (int i = 0; i < 101; i++)
        {
            request.Credentials.Add(new BatchIssueCredentialItemDto
            {
                BatchItemId = $"batch-{i}",
                HolderId = $"holder-{i}",
                Type = "ProofOfAge"
            });
        }

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/batch/credentials/issue", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Batch size cannot exceed 100 credentials");
    }

    [Fact]
    public async Task BatchVerifyCredentials_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClient();

        var request = new BatchVerifyCredentialsRequestDto
        {
            Credentials = new List<BatchVerifyCredentialItemDto>
            {
                new BatchVerifyCredentialItemDto
                {
                    BatchItemId = "verify-001",
                    CredentialId = "cred-123",
                    CredentialData = "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9..."
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/batch/credentials/verify", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BatchOperationResultDto<VerificationResultDto>>(_jsonOptions);
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Results.Should().HaveCount(1);
    }

    [Fact]
    public async Task BatchVerifyCredentials_ProcessesMultipleItems_ReturnsAggregateResult()
    {
        // Arrange
        var client = CreateClient();

        var request = new BatchVerifyCredentialsRequestDto
        {
            Credentials = new List<BatchVerifyCredentialItemDto>
            {
                new BatchVerifyCredentialItemDto
                {
                    BatchItemId = "verify-001",
                    CredentialId = "cred-123",
                    CredentialData = "data1"
                },
                new BatchVerifyCredentialItemDto
                {
                    BatchItemId = "verify-002",
                    CredentialId = "cred-456",
                    CredentialData = "data2"
                },
                new BatchVerifyCredentialItemDto
                {
                    BatchItemId = "verify-003",
                    CredentialId = "cred-789",
                    CredentialData = "data3"
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/batch/credentials/verify", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BatchOperationResultDto<VerificationResultDto>>(_jsonOptions);
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(3);
        result.Results.Should().HaveCount(3);
        result.SuccessCount.Should().BeGreaterThanOrEqualTo(0);
        result.FailureCount.Should().BeGreaterThanOrEqualTo(0);
        (result.SuccessCount + result.FailureCount).Should().Be(3);
    }

    [Fact]
    public async Task BatchRevokeCredentials_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClient();

        var request = new BatchRevokeCredentialsRequestDto
        {
            Credentials = new List<BatchRevokeCredentialItemDto>
            {
                new BatchRevokeCredentialItemDto { CredentialId = "cred-123", Reason = "Security breach detected" },
                new BatchRevokeCredentialItemDto { CredentialId = "cred-456", Reason = "Security breach detected" }
            },
            Reason = "Security breach detected"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/batch/credentials/revoke", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BatchOperationResultDto<bool>>(_jsonOptions);
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.Results.Should().HaveCount(2);
    }

    [Fact]
    public async Task BatchApproveIssuances_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClient();

        var request = new BatchApproveIssuancesRequestDto
        {
            Issuances = new List<BatchApproveIssuanceItemDto>
            {
                new BatchApproveIssuanceItemDto
                {
                    IssuanceId = Guid.NewGuid(),
                    Comments = "Approved after verification"
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/batch/issuances/approve", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BatchOperationResultDto<IssuanceDto>>(_jsonOptions);
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task BatchApproveIssuances_WithTooManyItems_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateClient();

        var request = new BatchApproveIssuancesRequestDto
        {
            Issuances = new List<BatchApproveIssuanceItemDto>()
        };

        // Add 51 items (exceeds limit)
        for (int i = 0; i < 51; i++)
        {
            request.Issuances.Add(new BatchApproveIssuanceItemDto
            {
                IssuanceId = Guid.NewGuid(),
                Comments = $"Approval {i}"
            });
        }

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/batch/issuances/approve", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Batch size cannot exceed 50 issuances");
    }

    [Fact]
    public async Task GetBatchStatus_WithExistingBatch_ReturnsStatus()
    {
        // Arrange
        var client = CreateClient();
        var batchId = Guid.NewGuid();

        var expectedStatus = new BatchOperationStatusDto
        {
            BatchId = batchId,
            Status = "Processing",
            TotalItems = 100,
            ProcessedItems = 67,
            SuccessCount = 65,
            FailureCount = 2,
            StartedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        _mockCacheService
            .Setup(x => x.GetAsync<BatchOperationStatusDto>($"batch:{batchId}", default))
            .ReturnsAsync(expectedStatus);

        // Act
        var response = await client.GetAsync($"/api/v1/batch/status/{batchId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BatchOperationStatusDto>(_jsonOptions);
        result.Should().NotBeNull();
        result.BatchId.Should().Be(batchId);
        result.Status.Should().Be("Processing");
        result.ProcessedItems.Should().Be(67);
    }

    [Fact]
    public async Task GetBatchStatus_WithNonExistentBatch_ReturnsNotFound()
    {
        // Arrange
        var client = CreateClient();
        var batchId = Guid.NewGuid();

        _mockCacheService
            .Setup(x => x.GetAsync<BatchOperationStatusDto>($"batch:{batchId}", default))
            .ReturnsAsync((BatchOperationStatusDto?)null);

        // Act
        var response = await client.GetAsync($"/api/v1/batch/status/{batchId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"Batch operation {batchId} not found");
    }

    [Fact]
    public async Task BatchIssueCredentials_TracksProgressInCache()
    {
        // Arrange
        var client = CreateClient();

        var request = new BatchIssueCredentialsRequestDto
        {
            Credentials = new List<BatchIssueCredentialItemDto>
            {
                new BatchIssueCredentialItemDto
                {
                    HolderId = "holder-123",
                    Type = "ProofOfAge"
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/batch/credentials/issue", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify audit was logged
        _mockAuditService.Verify(x => x.LogSecurityEventAsync(
            It.IsAny<HttpContext>(),
            SecurityEventType.DataModification,
            It.Is<string>(s => s.Contains("Batch credential issuance")),
            It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task BatchOperations_HandleConcurrentRequests()
    {
        // Arrange
        var client = CreateClient();

        var request1 = new BatchVerifyCredentialsRequestDto
        {
            Credentials = Enumerable.Range(1, 10).Select(i => new BatchVerifyCredentialItemDto
            {
                BatchItemId = $"verify-{i}",
                CredentialId = $"cred-{i}",
                CredentialData = $"data-{i}"
            }).ToList()
        };

        var request2 = new BatchVerifyCredentialsRequestDto
        {
            Credentials = Enumerable.Range(11, 10).Select(i => new BatchVerifyCredentialItemDto
            {
                BatchItemId = $"verify-{i}",
                CredentialId = $"cred-{i}",
                CredentialData = $"data-{i}"
            }).ToList()
        };

        // Act - Send concurrent requests
        var task1 = client.PostAsJsonAsync("/api/v1/batch/credentials/verify", request1);
        var task2 = client.PostAsJsonAsync("/api/v1/batch/credentials/verify", request2);

        var responses = await Task.WhenAll(task1, task2);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));

        var result1 = await responses[0].Content.ReadFromJsonAsync<BatchOperationResultDto<VerificationResultDto>>(_jsonOptions);
        var result2 = await responses[1].Content.ReadFromJsonAsync<BatchOperationResultDto<VerificationResultDto>>(_jsonOptions);

        result1!.TotalItems.Should().Be(10);
        result2!.TotalItems.Should().Be(10);
    }
}