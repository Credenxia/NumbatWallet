using Carter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Services;
using NumbatWallet.SharedKernel.Results;
using System.Text.Json;

namespace NumbatWallet.Web.Api.Endpoints;

/// <summary>
/// REST API endpoints for bulk operations
/// POA: Issue #187 - Bulk operations support with REST API
/// </summary>
public class BulkOperationEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/bulk")
            .RequireAuthorization()
            .WithTags("Bulk Operations")
            .WithOpenApi();

        // Bulk issue credentials
        group.MapPost("/credentials/issue", BulkIssueCredentials)
            .WithName("BulkIssueCredentials")
            .WithSummary("Bulk issue multiple credentials")
            .WithDescription("Issue multiple credentials in parallel with real-time progress tracking")
            .Produces<BulkIssueResult>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Bulk revoke credentials
        group.MapPost("/credentials/revoke", BulkRevokeCredentials)
            .WithName("BulkRevokeCredentials")
            .WithSummary("Bulk revoke multiple credentials")
            .WithDescription("Revoke multiple credentials with configurable error handling")
            .Produces<BulkRevokeResult>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Bulk verify credentials
        group.MapPost("/credentials/verify", BulkVerifyCredentials)
            .WithName("BulkVerifyCredentials")
            .WithSummary("Bulk verify multiple credentials")
            .WithDescription("Verify multiple credentials and return validation results")
            .Produces<BulkVerificationResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Import credentials from file
        group.MapPost("/credentials/import", ImportCredentials)
            .WithName("ImportCredentials")
            .WithSummary("Import credentials from CSV or JSON")
            .WithDescription("Import credentials from uploaded CSV or JSON file")
            .DisableAntiforgery()
            .Produces<BulkIssueResult>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Get operation status
        group.MapGet("/operations/{operationId}/status", GetOperationStatus)
            .WithName("GetOperationStatus")
            .WithSummary("Get bulk operation status")
            .WithDescription("Get the current status of a bulk operation")
            .Produces<OperationStatusDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Cancel operation
        group.MapPost("/operations/{operationId}/cancel", CancelOperation)
            .WithName("CancelOperation")
            .WithSummary("Cancel a running bulk operation")
            .WithDescription("Request cancellation of a running bulk operation")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get operation results
        group.MapGet("/operations/{operationId}/results", GetOperationResults)
            .WithName("GetOperationResults")
            .WithSummary("Get bulk operation results")
            .WithDescription("Get detailed results of a completed bulk operation")
            .Produces<OperationResultsDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Export operation results
        group.MapGet("/operations/{operationId}/export", ExportOperationResults)
            .WithName("ExportOperationResults")
            .WithSummary("Export operation results")
            .WithDescription("Export operation results as CSV or JSON")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> BulkIssueCredentials(
        [FromBody] BulkCredentialIssuanceRequest request,
        [FromServices] IDispatcher dispatcher,
        [FromServices] IProgressNotificationService progressService,
        CancellationToken cancellationToken)
    {
        var command = new BulkIssueCredentialsCommand
        {
            IssuerId = request.IssuerId,
            Credentials = request.Credentials.Select(c => new BulkCredentialRequest
            {
                SubjectId = c.SubjectId,
                CredentialType = c.CredentialType,
                Claims = c.Claims.ToDictionary(x => x.Key, x => (object)x.Value),
                ExpiresAt = c.ExpiresAt,
                Metadata = c.Metadata ?? new Dictionary<string, string>()
            }).ToList(),
            Options = MapProcessingOptions(request.Options)
        };

        var result = await dispatcher.DispatchAsync<BulkIssueCredentialsCommand, BulkIssueResult>(
            command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Bulk Issue Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Start progress tracking
        _ = Task.Run(async () =>
        {
            await progressService.NotifyProgressAsync(
                result.Value.OperationId,
                new ProgressUpdate
                {
                    OperationId = result.Value.OperationId,
                    Status = "Started",
                    ProcessedCount = 0,
                    TotalCount = request.Credentials.Count,
                    PercentComplete = 0,
                    UpdatedAt = DateTime.UtcNow
                });
        }, cancellationToken);

        return Results.Accepted($"/api/v1/bulk/operations/{result.Value.OperationId}/status", result.Value);
    }

    private static async Task<IResult> BulkRevokeCredentials(
        [FromBody] BulkRevokeCredentialsRequest request,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var command = new BulkRevokeCredentialsCommand
        {
            CredentialIds = request.CredentialIds,
            Reason = request.Reason,
            Options = MapProcessingOptions(request.Options)
        };

        var result = await dispatcher.DispatchAsync<BulkRevokeCredentialsCommand, BulkOperationResult>(
            command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Bulk Revoke Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Results.Accepted($"/api/v1/bulk/operations/{result.Value.OperationId}/status", result.Value);
    }

    private static async Task<IResult> BulkVerifyCredentials(
        [FromBody] BulkVerifyCredentialsRequest request,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var command = new BulkVerifyCredentialsCommand
        {
            CredentialIds = request.CredentialIds,
            VerificationOptions = new VerificationOptions
            {
                CheckRevocation = request.Options?.CheckRevocation ?? true,
                CheckExpiry = request.Options?.CheckExpiry ?? true,
                CheckSignature = request.Options?.CheckSignature ?? true
            }
        };

        var result = await dispatcher.DispatchAsync<BulkVerifyCredentialsCommand, BulkVerificationResult>(
            command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Bulk Verify Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ImportCredentials(
        [FromForm] ImportCredentialsFormRequest request,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid File",
                Detail = "No file was uploaded",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var allowedExtensions = new[] { ".csv", ".json" };
        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid File Type",
                Detail = "Only CSV and JSON files are supported",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            using var stream = request.File.OpenReadStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync(cancellationToken);

            List<BulkCredentialRequest> credentials;
            if (extension == ".csv")
            {
                credentials = ParseCsvCredentials(content, request.CsvOptions);
            }
            else
            {
                credentials = JsonSerializer.Deserialize<List<BulkCredentialRequest>>(content)
                    ?? new List<BulkCredentialRequest>();
            }

            var command = new BulkIssueCredentialsCommand
            {
                IssuerId = request.IssuerId,
                Credentials = credentials,
                Options = MapProcessingOptions(request.ProcessingOptions)
            };

            var result = await dispatcher.DispatchAsync<BulkIssueCredentialsCommand, BulkIssueResult>(
                command, cancellationToken);

            if (result.IsFailure)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Import Failed",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            return Results.Accepted($"/api/v1/bulk/operations/{result.Value.OperationId}/status", result.Value);
        }
        catch (JsonException ex)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid JSON",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Import Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static async Task<IResult> GetOperationStatus(
        string operationId,
        [FromServices] IBulkOperationStatusService statusService,
        CancellationToken cancellationToken)
    {
        var status = await statusService.GetOperationStatusAsync(operationId, cancellationToken);

        if (status == null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Operation Not Found",
                Detail = $"Operation {operationId} not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Results.Ok(status);
    }

    private static async Task<IResult> CancelOperation(
        string operationId,
        [FromServices] IBulkOperationStatusService statusService,
        CancellationToken cancellationToken)
    {
        var cancelled = await statusService.CancelOperationAsync(operationId, cancellationToken);

        if (!cancelled)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Operation Not Found",
                Detail = $"Operation {operationId} not found or already completed",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Results.NoContent();
    }

    private static async Task<IResult> GetOperationResults(
        string operationId,
        [FromServices] IBulkOperationStatusService statusService,
        CancellationToken cancellationToken)
    {
        var results = await statusService.GetOperationResultsAsync(operationId, cancellationToken);

        if (results == null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Results Not Found",
                Detail = $"Results for operation {operationId} not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Results.Ok(results);
    }

    private static async Task<IResult> ExportOperationResults(
        string operationId,
        [FromQuery] string format,
        [FromServices] IBulkOperationStatusService statusService,
        CancellationToken cancellationToken)
    {
        if (format != "csv" && format != "json")
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid Format",
                Detail = "Format must be 'csv' or 'json'",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var results = await statusService.GetOperationResultsAsync(operationId, cancellationToken);

        if (results == null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Results Not Found",
                Detail = $"Results for operation {operationId} not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        byte[] content;
        string contentType;
        string fileName;

        if (format == "csv")
        {
            content = ExportToCsv(results);
            contentType = "text/csv";
            fileName = $"operation-{operationId}-results.csv";
        }
        else
        {
            content = ExportToJson(results);
            contentType = "application/json";
            fileName = $"operation-{operationId}-results.json";
        }

        return Results.File(content, contentType, fileName);
    }

    private static BulkProcessingOptions MapProcessingOptions(ProcessingOptionsDto? options)
    {
        return new BulkProcessingOptions
        {
            ContinueOnError = options?.ContinueOnError ?? true,
            MaxConcurrency = options?.MaxConcurrency ?? 10,
            EnableProgressTracking = options?.EnableProgressTracking ?? true,
            ValidateBeforeProcessing = options?.ValidateBeforeProcessing ?? true,
            Timeout = options?.TimeoutSeconds.HasValue
                ? TimeSpan.FromSeconds(options.TimeoutSeconds.Value)
                : TimeSpan.FromMinutes(5)
        };
    }

    private static List<BulkCredentialRequest> ParseCsvCredentials(string content, CsvOptionsDto? options)
    {
        var credentials = new List<BulkCredentialRequest>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            throw new InvalidOperationException("CSV file must contain headers and at least one data row");
        }

        var delimiter = options?.Delimiter ?? ',';
        var headers = lines[0].Split(delimiter);
        var subjectIdColumn = options?.SubjectIdColumn ?? "SubjectId";
        var typeColumn = options?.CredentialTypeColumn ?? "CredentialType";

        var subjectIdIndex = Array.IndexOf(headers, subjectIdColumn);
        var typeIndex = Array.IndexOf(headers, typeColumn);

        if (subjectIdIndex < 0 || typeIndex < 0)
        {
            throw new InvalidOperationException($"CSV must contain '{subjectIdColumn}' and '{typeColumn}' columns");
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(delimiter);

            if (values.Length < headers.Length)
            {
                continue; // Skip malformed rows
            }

            var claims = new Dictionary<string, object>();
            for (int j = 0; j < headers.Length; j++)
            {
                if (j != subjectIdIndex && j != typeIndex)
                {
                    claims[headers[j]] = values[j];
                }
            }

            credentials.Add(new BulkCredentialRequest
            {
                SubjectId = values[subjectIdIndex].Trim(),
                CredentialType = values[typeIndex].Trim(),
                Claims = claims
            });
        }

        return credentials;
    }

    private static byte[] ExportToCsv(OperationResultsDto results)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("CredentialId,Status,ErrorMessage,ProcessedAt");

        foreach (var item in results.Items)
        {
            csv.AppendLine($"{item.CredentialId},{item.Status},\"{item.ErrorMessage}\",{item.ProcessedAt:yyyy-MM-dd HH:mm:ss}");
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static byte[] ExportToJson(OperationResultsDto results)
    {
        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return System.Text.Encoding.UTF8.GetBytes(json);
    }
}

// Request DTOs
public class BulkCredentialIssuanceRequest
{
    public string IssuerId { get; set; } = string.Empty;
    public List<CredentialRequestDto> Credentials { get; set; } = new();
    public ProcessingOptionsDto? Options { get; set; }
}

public class CredentialRequestDto
{
    public string SubjectId { get; set; } = string.Empty;
    public string CredentialType { get; set; } = string.Empty;
    public Dictionary<string, string> Claims { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class ProcessingOptionsDto
{
    public bool? ContinueOnError { get; set; }
    public int? MaxConcurrency { get; set; }
    public bool? EnableProgressTracking { get; set; }
    public bool? ValidateBeforeProcessing { get; set; }
    public int? TimeoutSeconds { get; set; }
}

public class BulkRevokeCredentialsRequest
{
    public List<string> CredentialIds { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public ProcessingOptionsDto? Options { get; set; }
}

public class BulkVerifyCredentialsRequest
{
    public List<string> CredentialIds { get; set; } = new();
    public VerificationOptionsDto? Options { get; set; }
}

public class VerificationOptionsDto
{
    public bool? CheckRevocation { get; set; }
    public bool? CheckExpiry { get; set; }
    public bool? CheckSignature { get; set; }
}

public class ImportCredentialsFormRequest
{
    public IFormFile? File { get; set; }
    public string IssuerId { get; set; } = string.Empty;
    public ProcessingOptionsDto? ProcessingOptions { get; set; }
    public CsvOptionsDto? CsvOptions { get; set; }
}

public class CsvOptionsDto
{
    public char? Delimiter { get; set; }
    public string? SubjectIdColumn { get; set; }
    public string? CredentialTypeColumn { get; set; }
}

// DTOs are defined in NumbatWallet.Application.DTOs