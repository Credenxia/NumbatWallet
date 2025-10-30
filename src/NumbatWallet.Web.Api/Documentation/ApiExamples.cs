using Swashbuckle.AspNetCore.Filters;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Commands.Batch;
using NumbatWallet.Web.Api.Controllers;
using NumbatWallet.Web.Api.Webhooks;

namespace NumbatWallet.Web.Api.Documentation;

/// <summary>
/// Batch issue credentials request example
/// </summary>
public class BatchIssueCredentialsRequestExample : IExamplesProvider<BatchIssueCredentialsRequestDto>
{
    public BatchIssueCredentialsRequestDto GetExamples()
    {
        return new BatchIssueCredentialsRequestDto
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
                        ["lastName"] = "Doe",
                        ["dateOfBirth"] = "1990-01-01",
                        ["nationality"] = "Australian",
                        ["isOver18"] = true
                    },
                    ExpiryDate = DateTime.UtcNow.AddYears(5)
                },
                new BatchIssueCredentialItemDto
                {
                    BatchItemId = "batch-002",
                    HolderId = "holder-456",
                    Type = "DriverLicense",
                    Claims = new Dictionary<string, object>
                    {
                        ["licenseNumber"] = "DL-12345678",
                        ["firstName"] = "Jane",
                        ["lastName"] = "Smith",
                        ["class"] = "C",
                        ["restrictions"] = "S"
                    },
                    ExpiryDate = DateTime.UtcNow.AddYears(3)
                }
            }
        };
    }
}

/// <summary>
/// Batch verify credentials request example
/// </summary>
public class BatchVerifyCredentialsRequestExample : IExamplesProvider<BatchVerifyCredentialsRequestDto>
{
    public BatchVerifyCredentialsRequestDto GetExamples()
    {
        return new BatchVerifyCredentialsRequestDto
        {
            Credentials = new List<BatchVerifyCredentialItemDto>
            {
                new BatchVerifyCredentialItemDto
                {
                    BatchItemId = "verify-001",
                    CredentialId = "cred-123-456",
                    CredentialData = "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9...",
                    Options = new VerificationOptionsDto
                    {
                        CheckRevocation = true,
                        CheckExpiry = true,
                        CheckSignature = true,
                        CheckSchema = true,
                        RequireTrustChain = true
                    }
                },
                new BatchVerifyCredentialItemDto
                {
                    BatchItemId = "verify-002",
                    CredentialId = "cred-789-012",
                    CredentialData = "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9..."
                }
            }
        };
    }
}

/// <summary>
/// Batch revoke credentials request example
/// </summary>
public class BatchRevokeCredentialsRequestExample : IExamplesProvider<BatchRevokeCredentialsRequestDto>
{
    public BatchRevokeCredentialsRequestDto GetExamples()
    {
        return new BatchRevokeCredentialsRequestDto
        {
            Credentials = new List<BatchRevokeCredentialItemDto>
            {
                new BatchRevokeCredentialItemDto
                {
                    CredentialId = "cred-123-456",
                    Reason = "Suspected fraudulent activity"
                },
                new BatchRevokeCredentialItemDto
                {
                    CredentialId = "cred-789-012",
                    Reason = "Expired identity verification"
                }
            },
            Reason = "Suspected fraudulent activity detected"
        };
    }
}

/// <summary>
/// Batch operation result example
/// </summary>
public class BatchOperationResultExample : IExamplesProvider<BatchOperationResultDto<CredentialDto>>
{
    public BatchOperationResultDto<CredentialDto> GetExamples()
    {
        return new BatchOperationResultDto<CredentialDto>
        {
            TotalItems = 3,
            SuccessCount = 2,
            FailureCount = 1,
            ProcessedAt = DateTime.UtcNow,
            Results = new List<BatchOperationItemResult<CredentialDto>>
            {
                new BatchOperationItemResult<CredentialDto>
                {
                    Success = true,
                    ItemId = "batch-001",
                    Data = new CredentialDto
                    {
                        Id = "cred-new-123",
                        HolderId = "holder-123",
                        IssuerId = "issuer-gov-001",
                        Type = "ProofOfAge",
                        CredentialSubject = new Dictionary<string, object>
                        {
                            ["firstName"] = "John",
                            ["lastName"] = "Doe",
                            ["isOver18"] = true
                        },
                        IssuanceDate = DateTime.UtcNow,
                        ExpirationDate = DateTime.UtcNow.AddYears(5),
                        Status = "Active",
                        IsRevoked = false
                    }
                },
                new BatchOperationItemResult<CredentialDto>
                {
                    Success = false,
                    ItemId = "batch-002",
                    Error = "Holder not found or inactive",
                    Data = null
                }
            }
        };
    }
}

/// <summary>
/// Webhook subscription request example
/// </summary>
public class WebhookSubscriptionRequestExample : IExamplesProvider<WebhookSubscriptionRequestDto>
{
    public WebhookSubscriptionRequestDto GetExamples()
    {
        return new WebhookSubscriptionRequestDto
        {
            Url = "https://api.example.com/webhooks/numbat",
            Events = new List<WebhookEventType>
            {
                WebhookEventType.CredentialIssued,
                WebhookEventType.CredentialRevoked,
                WebhookEventType.CredentialExpired,
                WebhookEventType.IssuanceCompleted
            },
            Headers = new Dictionary<string, string>
            {
                ["X-API-Key"] = "your-api-key-here",
                ["X-Custom-Header"] = "custom-value"
            },
            MaxRetries = 3,
            TimeoutSeconds = 30
        };
    }
}

/// <summary>
/// Webhook subscription response example
/// </summary>
public class WebhookSubscriptionResponseExample : IExamplesProvider<WebhookSubscriptionResponseDto>
{
    public WebhookSubscriptionResponseDto GetExamples()
    {
        return new WebhookSubscriptionResponseDto
        {
            Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            Url = "https://api.example.com/webhooks/numbat",
            Secret = "whsec_1234567890abcdef",
            Events = new List<WebhookEventType>
            {
                WebhookEventType.CredentialIssued,
                WebhookEventType.CredentialRevoked
            },
            IsActive = true,
            Headers = new Dictionary<string, string>
            {
                ["X-API-Key"] = "your-api-key-here"
            },
            MaxRetries = 3,
            TimeoutSeconds = 30,
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            LastDeliveryAt = DateTime.UtcNow.AddHours(-2),
            ConsecutiveFailures = 0
        };
    }
}

/// <summary>
/// Webhook payload example
/// </summary>
public class WebhookPayloadExample : IExamplesProvider<WebhookPayload>
{
    public WebhookPayload GetExamples()
    {
        return new WebhookPayload
        {
            Id = Guid.NewGuid(),
            EventType = WebhookEventType.CredentialIssued,
            Timestamp = DateTime.UtcNow,
            Source = "NumbatWallet",
            Data = new
            {
                credentialId = "cred-123-456",
                holderId = "holder-789",
                issuerId = "issuer-001",
                type = "ProofOfAge",
                issuanceDate = DateTime.UtcNow,
                expirationDate = DateTime.UtcNow.AddYears(5)
            },
            Metadata = new Dictionary<string, string>
            {
                ["environment"] = "production",
                ["version"] = "1.0.0",
                ["tenantId"] = "tenant-001"
            }
        };
    }
}

/// <summary>
/// Webhook test response example
/// </summary>
public class WebhookTestResponseExample : IExamplesProvider<WebhookTestResponseDto>
{
    public WebhookTestResponseDto GetExamples()
    {
        return new WebhookTestResponseDto
        {
            Success = true,
            StatusCode = 200,
            Duration = TimeSpan.FromMilliseconds(245),
            ErrorMessage = null,
            DeliveredAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Verification result example
/// </summary>
public class VerificationResultExample : IExamplesProvider<VerificationResultDto>
{
    public VerificationResultDto GetExamples()
    {
        return new VerificationResultDto
        {
            IsValid = true,
            VerifiedAt = DateTime.UtcNow,
            Checks = new VerificationChecksDto
            {
                Signature = true,
                Expiry = true,
                Revocation = true,
                Schema = true,
                Issuer = true
            },
            ErrorMessage = null,
            Claims = new Dictionary<string, object>
            {
                ["credentialId"] = "cred-123-456",
                ["holderId"] = "holder-789",
                ["issuerId"] = "issuer-001",
                ["credentialType"] = "ProofOfAge",
                ["trustScore"] = 95.5
            }
        };
    }
}

/// <summary>
/// Batch operation status example
/// </summary>
public class BatchOperationStatusExample : IExamplesProvider<BatchOperationStatusDto>
{
    public BatchOperationStatusDto GetExamples()
    {
        return new BatchOperationStatusDto
        {
            BatchId = Guid.Parse("batch-8400-e29b-41d4-a716-446655440000"),
            Status = "Processing",
            TotalItems = 100,
            ProcessedItems = 67,
            SuccessCount = 65,
            FailureCount = 2,
            StartedAt = DateTime.UtcNow.AddMinutes(-2),
            CompletedAt = null,
            Duration = null
        };
    }
}

/// <summary>
/// API error response example
/// </summary>
public class ApiErrorResponseExample : IExamplesProvider<ApiErrorResponse>
{
    public ApiErrorResponse GetExamples()
    {
        return new ApiErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = "The batch size cannot exceed 100 items",
            Instance = "/api/v1/batch/credentials/issue",
            TraceId = "00-0af7651916cd43dd8448eb211c80319c-00",
            Errors = new Dictionary<string, string[]>
            {
                ["Credentials"] = new[] { "Batch size cannot exceed 100 credentials" }
            },
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Standard API error response
/// </summary>
public class ApiErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
    public DateTime Timestamp { get; set; }
}