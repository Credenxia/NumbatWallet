using Swashbuckle.AspNetCore.Filters;
using NumbatWallet.Application.DTOs;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Web.Api.Documentation;

/// <summary>
/// Provides example data for Swagger/OpenAPI documentation
/// </summary>
public static class ApiDocumentation
{
    /// <summary>
    /// Example request for creating a new wallet
    /// </summary>
    public class CreateWalletRequestExample : IExamplesProvider<CreateWalletDto>
    {
        public CreateWalletDto GetExamples()
        {
            return new CreateWalletDto
            {
                PersonId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
                DeviceInfo = new DeviceInfoDto
                {
                    Platform = "iOS",
                    DeviceId = "device-123-456",
                    DeviceName = "John's iPhone",
                    AppVersion = "1.0.0"
                },
                Name = "John's Primary Wallet"
            };
        }
    }

    /// <summary>
    /// Example response for wallet creation
    /// </summary>
    public class WalletDtoExample : IExamplesProvider<WalletDto>
    {
        public WalletDto GetExamples()
        {
            return new WalletDto
            {
                Id = "456e7890-e89b-12d3-a456-426614174000",
                PersonId = "123e4567-e89b-12d3-a456-426614174000",
                PersonName = "John Doe",
                Name = "John's Primary Wallet",
                Status = "Active",
                IsActive = true,
                IsSuspended = false,
                CreatedAt = DateTimeOffset.Now.AddDays(-30),
                UpdatedAt = DateTimeOffset.Now.AddDays(-1),
                CredentialCount = 5,
                Metadata = new Dictionary<string, string>
                {
                    ["type"] = "Holder",
                    ["tags"] = "personal,primary"
                }
            };
        }
    }

    /// <summary>
    /// Example request for issuing a credential
    /// </summary>
    public class IssueCredentialRequestExample : IExamplesProvider<IssueCredentialDto>
    {
        public IssueCredentialDto GetExamples()
        {
            return new IssueCredentialDto
            {
                WalletId = Guid.Parse("456e7890-e89b-12d3-a456-426614174000"),
                Type = "DRIVERS_LICENSE",
                Data = new Dictionary<string, object>
                {
                    ["id"] = "did:numbat:wa:456e7890-e89b-12d3-a456-426614174000",
                    ["name"] = "John Doe",
                    ["dateOfBirth"] = "1990-01-01",
                    ["licenseNumber"] = "DL123456789",
                    ["class"] = "C",
                    ["restrictions"] = new[] { "S" },
                    ["firstName"] = "John",
                    ["lastName"] = "Doe",
                    ["address"] = "123 Main St, Perth, WA 6000"
                },
                ExpiresAt = DateTime.Now.AddYears(5),
                IssuerId = "789e0123-e89b-12d3-a456-426614174000"
            };
        }
    }

    /// <summary>
    /// Example response for credential
    /// </summary>
    public class CredentialDtoExample : IExamplesProvider<CredentialDto>
    {
        public CredentialDto GetExamples()
        {
            return new CredentialDto
            {
                Id = "abc12345-e89b-12d3-a456-426614174000",
                HolderId = "456e7890-e89b-12d3-a456-426614174000",
                IssuerId = "789e0123-e89b-12d3-a456-426614174000",
                Type = "DriversLicense",
                Status = "Active",
                IssuanceDate = DateTime.Now.AddDays(-1),
                ExpirationDate = DateTime.Now.AddYears(5),
                IsRevoked = false,
                CredentialSubject = new Dictionary<string, object>
                {
                    ["id"] = "did:numbat:wa:456e7890-e89b-12d3-a456-426614174000",
                    ["name"] = "John Doe",
                    ["licenseNumber"] = "DL123456789"
                },
                Metadata = new Dictionary<string, string>
                {
                    ["firstName"] = "John",
                    ["lastName"] = "Doe"
                }
            };
        }
    }

    // /// <summary>
    // /// Example request for creating a presentation
    // /// </summary>
    // public class CreatePresentationRequestExample : IExamplesProvider<CreatePresentationDto>
    // {
    //     public CreatePresentationDto GetExamples()
    //     {
    //         // TODO: Fix to match actual DTO structure
    //         return new CreatePresentationDto();
    //     }
    // }

    /// <summary>
    /// Example error response
    /// </summary>
    public class ErrorResponseExample : IExamplesProvider<ProblemDetails>
    {
        public ProblemDetails GetExamples()
        {
            return new ProblemDetails
            {
                Type = "https://numbatwallet.wa.gov.au/errors/validation",
                Title = "Validation Error",
                Status = 400,
                Detail = "One or more validation errors occurred.",
                Instance = "/api/v1/wallets",
                Extensions = new Dictionary<string, object?>
                {
                    ["traceId"] = "00-1234567890abcdef1234567890abcdef-1234567890abcdef-00",
                    ["errors"] = new
                    {
                        PersonId = new[] { "The PersonId field is required." },
                        WalletName = new[] { "Wallet name must be between 3 and 100 characters." }
                    }
                }
            };
        }
    }

    /// <summary>
    /// Example health check response
    /// </summary>
    public class HealthCheckResponseExample : IExamplesProvider<HealthCheckResponse>
    {
        public HealthCheckResponse GetExamples()
        {
            return new HealthCheckResponse
            {
                Status = "Healthy",
                Duration = "00:00:00.0123456",
                Info = new Dictionary<string, object>
                {
                    ["database"] = new { status = "Healthy", duration = "00:00:00.0023456" },
                    ["redis"] = new { status = "Healthy", duration = "00:00:00.0012345" },
                    ["storage"] = new { status = "Healthy", duration = "00:00:00.0034567" }
                }
            };
        }
    }

    /// <summary>
    /// Example bulk operation request
    /// </summary>
    public class BulkOperationRequestExample : IExamplesProvider<BulkOperationRequestDto>
    {
        public BulkOperationRequestDto GetExamples()
        {
            return new BulkOperationRequestDto
            {
                Operation = BulkOperationType.Issue,
                EntityType = "Credential",
                EntityIds = new List<Guid>
                {
                    Guid.Parse("111e1111-e89b-12d3-a456-426614174000"),
                    Guid.Parse("222e2222-e89b-12d3-a456-426614174000"),
                    Guid.Parse("333e3333-e89b-12d3-a456-426614174000")
                },
                Parameters = new Dictionary<string, object>
                {
                    ["issuerId"] = "789e0123-e89b-12d3-a456-426614174000",
                    ["type"] = "EmployeeID",
                    ["validityPeriodDays"] = 365
                },
                ScheduledAt = DateTimeOffset.Now.AddHours(2),
                NotificationEmail = "admin@numbatwallet.wa.gov.au"
            };
        }
    }

    /// <summary>
    /// Example pagination response
    /// </summary>
    public class PagedResponseExample<T> : IExamplesProvider<PagedResponseDto<T>>
    {
        public PagedResponseDto<T> GetExamples()
        {
            return new PagedResponseDto<T>
            {
                Items = new List<T>(),
                TotalCount = 150,
                PageNumber = 1,
                PageSize = 20,
                TotalPages = 8,
                HasPreviousPage = false,
                HasNextPage = true
            };
        }
    }
}

public record HealthCheckResponse
{
    public string Status { get; init; } = "";
    public string Duration { get; init; } = "";
    public Dictionary<string, object> Info { get; init; } = new();
}

public record ProblemDetails
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public int? Status { get; init; }
    public string? Detail { get; init; }
    public string? Instance { get; init; }
    public IDictionary<string, object?> Extensions { get; init; } = new Dictionary<string, object?>();
}