using Carter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Enums;
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
            .WithSummary("Import credentials from CSV or JSON file")
            .WithDescription("Import multiple credentials from a file with validation")
            .Produces<BulkIssueResult>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .DisableAntiforgery();

        // Get bulk operation status
        group.MapGet("/operations/{operationId}/status", GetOperationStatus)
            .WithName("GetBulkOperationStatus")
            .WithSummary("Get the status of a bulk operation")
            .WithDescription("Check the progress and status of a running bulk operation")
            .Produces<BulkOperationStatus>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // Cancel bulk operation
        group.MapDelete("/operations/{operationId}", CancelOperation)
            .WithName("CancelBulkOperation")
            .WithSummary("Cancel a running bulk operation")
            .WithDescription("Attempt to cancel an in-progress bulk operation")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // Get bulk operation history
        group.MapGet("/operations", GetOperationHistory)
            .WithName("GetBulkOperationHistory")
            .WithSummary("Get bulk operation history")
            .WithDescription("Retrieve the history of bulk operations with filtering")
            .Produces<List<BulkOperationSummary>>(StatusCodes.Status200OK);

        // Export bulk operation results
        group.MapGet("/operations/{operationId}/export", ExportOperationResults)
            .WithName("ExportBulkOperationResults")
            .WithSummary("Export bulk operation results")
            .WithDescription("Export the results of a completed bulk operation")
            .Produces<FileContentResult>(StatusCodes.Status200OK, "text/csv")
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> BulkIssueCredentials(
        [FromBody] BulkCredentialIssuanceRequest request,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        // Convert request to command using record constructor
        var command = new BulkIssueCredentialsCommand(
            request.WalletIds.Select(id => Guid.Parse(id)).ToList(),
            request.CredentialType,
            request.Template ?? new Dictionary<string, object>(),
            request.IssuerId,
            Guid.Parse(request.IssuerOrganizationId),
            request.ValidFrom ?? DateTime.UtcNow,
            request.ValidUntil);

        var result = await dispatcher.SendAsync(command, cancellationToken);

        return Results.Accepted($"/api/v1/bulk/operations/{Guid.NewGuid()}/status", result);
    }

    private static async Task<IResult> BulkRevokeCredentials(
        [FromBody] BulkRevokeCredentialsRequest request,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        // Convert request to command using record constructor
        var command = new BulkRevokeCredentialsCommand(
            request.CredentialIds.Select(id => Guid.Parse(id)).ToList(),
            request.Reason);

        var result = await dispatcher.SendAsync(command, cancellationToken);

        return Results.Accepted($"/api/v1/bulk/operations/{Guid.NewGuid()}/status", result);
    }

    private static async Task<IResult> BulkVerifyCredentials(
        [FromBody] BulkVerifyCredentialsRequest request,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        // Convert request to command using record constructor
        var command = new BulkVerifyCredentialsCommand(
            request.CredentialIds.Select(id => Guid.Parse(id)).ToList());

        var result = await dispatcher.SendAsync(command, cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> ImportCredentials(
        IFormFile file,
        [FromForm] string issuerId,
        [FromForm] string issuerOrganizationId,
        [FromForm] string credentialType,
        [FromServices] IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid File",
                Detail = "No file was uploaded or the file is empty",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Read file content
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken);

        // Parse based on file type
        List<Guid> walletIds;
        Dictionary<string, object> template;

        if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            (walletIds, template) = ParseCsvFile(content);
        }
        else if (file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            (walletIds, template) = ParseJsonFile(content);
        }
        else
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Unsupported File Type",
                Detail = "Only CSV and JSON files are supported",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Create command
        var command = new BulkIssueCredentialsCommand(
            walletIds,
            Enum.Parse<CredentialType>(credentialType),
            template,
            issuerId,
            Guid.Parse(issuerOrganizationId),
            DateTime.UtcNow,
            null);

        var result = await dispatcher.SendAsync(command, cancellationToken);

        return Results.Accepted($"/api/v1/bulk/operations/{Guid.NewGuid()}/status", result);
    }

    private static async Task<IResult> GetOperationStatus(
        string operationId,
        [FromServices] IBulkOperationService operationService,
        CancellationToken cancellationToken)
    {
        var status = await operationService.GetOperationStatusAsync(operationId, cancellationToken);

        if (status == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(status);
    }

    private static async Task<IResult> CancelOperation(
        string operationId,
        [FromServices] IBulkOperationService operationService,
        CancellationToken cancellationToken)
    {
        var cancelled = await operationService.CancelOperationAsync(operationId, cancellationToken);

        if (!cancelled)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }

    private static async Task<IResult> GetOperationHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? operationType,
        [FromServices] IBulkOperationService operationService,
        CancellationToken cancellationToken)
    {
        var history = await operationService.GetOperationHistoryAsync(
            from ?? DateTime.UtcNow.AddDays(-7),
            to ?? DateTime.UtcNow,
            operationType,
            cancellationToken);

        return Results.Ok(history);
    }

    private static async Task<IResult> ExportOperationResults(
        string operationId,
        [FromQuery] string format,
        [FromServices] IBulkOperationService operationService,
        CancellationToken cancellationToken)
    {
        var data = await operationService.ExportOperationResultsAsync(
            operationId,
            format ?? "csv",
            cancellationToken);

        if (data == null)
        {
            return Results.NotFound();
        }

        var contentType = format?.ToLowerInvariant() == "json"
            ? "application/json"
            : "text/csv";

        return Results.File(data, contentType, $"bulk-operation-{operationId}.{format ?? "csv"}");
    }

    private static (List<Guid>, Dictionary<string, object>) ParseCsvFile(string content)
    {
        var walletIds = new List<Guid>();
        var template = new Dictionary<string, object>();

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            throw new InvalidOperationException("CSV file must contain headers and at least one data row");
        }

        var headers = lines[0].Split(',');
        var walletIdIndex = Array.IndexOf(headers, "WalletId");

        if (walletIdIndex < 0)
        {
            throw new InvalidOperationException("CSV must contain WalletId column");
        }

        // Use first row as template
        if (lines.Length > 1)
        {
            var firstRow = lines[1].Split(',');
            for (int i = 0; i < headers.Length; i++)
            {
                if (i != walletIdIndex)
                {
                    template[headers[i]] = firstRow[i];
                }
            }
        }

        // Collect all wallet IDs
        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',');
            if (values.Length > walletIdIndex && Guid.TryParse(values[walletIdIndex], out var walletId))
            {
                walletIds.Add(walletId);
            }
        }

        return (walletIds, template);
    }

    private static (List<Guid>, Dictionary<string, object>) ParseJsonFile(string content)
    {
        var json = JsonSerializer.Deserialize<BulkImportData>(content);
        if (json == null)
        {
            throw new InvalidOperationException("Invalid JSON format");
        }

        return (json.WalletIds.Select(id => Guid.Parse(id)).ToList(), json.Template);
    }
}

// Request DTOs
public class BulkCredentialIssuanceRequest
{
    public List<string> WalletIds { get; set; } = new();
    public CredentialType CredentialType { get; set; }
    public Dictionary<string, object>? Template { get; set; }
    public string IssuerId { get; set; } = string.Empty;
    public string IssuerOrganizationId { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public class BulkRevokeCredentialsRequest
{
    public List<string> CredentialIds { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
}

public class BulkVerifyCredentialsRequest
{
    public List<string> CredentialIds { get; set; } = new();
}

public class BulkImportData
{
    public List<string> WalletIds { get; set; } = new();
    public Dictionary<string, object> Template { get; set; } = new();
}

public class BulkOperationStatus
{
    public string OperationId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class BulkOperationSummary
{
    public string OperationId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}