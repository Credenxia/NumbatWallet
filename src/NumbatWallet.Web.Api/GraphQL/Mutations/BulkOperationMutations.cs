using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Subscriptions;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Enums;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Results;

namespace NumbatWallet.Web.Api.GraphQL.Mutations;

/// <summary>
/// GraphQL mutations for bulk operations
/// POA: Issue #187 - Bulk operations support with GraphQL
/// </summary>
[ExtendObjectType("Mutation")]
public class BulkOperationMutations
{
    /// <summary>
    /// Bulk issue credentials with async processing
    /// </summary>
    [Authorize]
    [GraphQLDescription("Bulk issue credentials with async processing and real-time progress tracking")]
    public async Task<BulkIssueResult> BulkIssueCredentials(
        [Service] IDispatcher dispatcher,
        [Service] ITopicEventSender eventSender,
        BulkIssueCredentialsInput input,
        CancellationToken cancellationToken = default)
    {
        // Convert input to command using the record constructor
        var command = new BulkIssueCredentialsCommand(
            input.WalletIds.Select(id => Guid.Parse(id)).ToList(),
            input.CredentialType,
            input.Template ?? new Dictionary<string, object>(),
            input.IssuerId,
            Guid.Parse(input.IssuerOrganizationId),
            input.ValidFrom ?? DateTime.UtcNow,
            input.ValidUntil);

        // Execute command
        var result = await dispatcher.SendAsync(command, cancellationToken);

        // Convert to BulkIssueResult DTO for GraphQL
        var bulkResult = new BulkIssueResult
        {
            OperationId = Guid.NewGuid().ToString(),
            TotalCount = result.TotalRequested,
            SuccessCount = result.SuccessCount,
            FailureCount = result.FailureCount,
            FailedIds = result.Errors.Select(e => e.WalletId.ToString()).ToList(),
            Errors = result.Errors.ToDictionary(e => e.WalletId.ToString(), e => e.Error)
        };

        // Send initial subscription event
        await eventSender.SendAsync(
            $"OnBulkOperationStarted_{bulkResult.OperationId}",
            bulkResult,
            cancellationToken);

        return bulkResult;
    }

    /// <summary>
    /// Bulk revoke credentials
    /// </summary>
    [Authorize]
    [GraphQLDescription("Bulk revoke multiple credentials")]
    public async Task<BulkRevokeResult> BulkRevokeCredentials(
        [Service] IDispatcher dispatcher,
        [Service] ITopicEventSender eventSender,
        BulkRevokeCredentialsInput input,
        CancellationToken cancellationToken = default)
    {
        // Convert string IDs to Guids
        var credentialIds = input.CredentialIds.Select(id => Guid.Parse(id)).ToList();

        // Create command using record constructor
        var command = new BulkRevokeCredentialsCommand(
            credentialIds,
            input.Reason);

        var result = await dispatcher.SendAsync(command, cancellationToken);

        // Convert to BulkRevokeResult DTO for GraphQL
        var bulkResult = new BulkRevokeResult
        {
            OperationId = Guid.NewGuid().ToString(),
            TotalCount = result.TotalRequested,
            SuccessCount = result.SuccessCount,
            FailureCount = result.FailureCount,
            FailedIds = result.Errors.Select(e => e.CredentialId.ToString()).ToList()
        };

        await eventSender.SendAsync(
            $"OnBulkOperationStarted_{bulkResult.OperationId}",
            bulkResult,
            cancellationToken);

        return bulkResult;
    }

    /// <summary>
    /// Bulk verify credentials
    /// </summary>
    [Authorize]
    [GraphQLDescription("Bulk verify multiple credentials")]
    public async Task<BulkVerificationResult> BulkVerifyCredentials(
        [Service] IDispatcher dispatcher,
        BulkVerifyCredentialsInput input,
        CancellationToken cancellationToken = default)
    {
        // Convert string IDs to Guids
        var credentialIds = input.CredentialIds.Select(id => Guid.Parse(id)).ToList();

        // Create command using record constructor
        var command = new BulkVerifyCredentialsCommand(credentialIds);

        var result = await dispatcher.SendAsync(command, cancellationToken);

        // Convert to BulkVerificationResult DTO for GraphQL
        return new BulkVerificationResult
        {
            TotalCount = result.TotalRequested,
            ValidCount = result.SuccessCount,
            InvalidCount = result.FailureCount,
            Results = result.Results.ToDictionary(
                r => r.CredentialId.ToString(),
                r => r.IsValid ? VerificationStatus.Verified : VerificationStatus.Failed)
        };
    }

    /// <summary>
    /// Import credentials from CSV/JSON
    /// </summary>
    [Authorize]
    [GraphQLDescription("Import credentials from CSV or JSON file")]
    public async Task<BulkIssueResult> ImportCredentials(
        [Service] IDispatcher dispatcher,
        [Service] ITopicEventSender eventSender,
        IFile file,
        ImportCredentialsInput input,
        CancellationToken cancellationToken = default)
    {
        // Read file content
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken);

        // Parse based on file type
        var (walletIds, template) = file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            ? ParseCsvCredentials(content, input.CsvOptions)
            : ParseJsonCredentials(content);

        // Create command
        var command = new BulkIssueCredentialsCommand(
            walletIds,
            input.CredentialType,
            template,
            input.IssuerId,
            Guid.Parse(input.IssuerOrganizationId),
            input.ValidFrom ?? DateTime.UtcNow,
            input.ValidUntil);

        var result = await dispatcher.SendAsync(command, cancellationToken);

        // Convert to BulkIssueResult DTO for GraphQL
        var bulkResult = new BulkIssueResult
        {
            OperationId = Guid.NewGuid().ToString(),
            TotalCount = result.TotalRequested,
            SuccessCount = result.SuccessCount,
            FailureCount = result.FailureCount,
            FailedIds = result.Errors.Select(e => e.WalletId.ToString()).ToList(),
            Errors = result.Errors.ToDictionary(e => e.WalletId.ToString(), e => e.Error)
        };

        await eventSender.SendAsync(
            $"OnBulkOperationStarted_{bulkResult.OperationId}",
            bulkResult,
            cancellationToken);

        return bulkResult;
    }

    private (List<Guid>, Dictionary<string, object>) ParseCsvCredentials(string content, CsvImportOptions? options)
    {
        var walletIds = new List<Guid>();
        var template = new Dictionary<string, object>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            throw new GraphQLException("CSV file must contain headers and at least one data row");
        }

        var headers = lines[0].Split(options?.Delimiter ?? ",");
        var walletIdIndex = Array.IndexOf(headers, options?.WalletIdColumn ?? "WalletId");

        if (walletIdIndex < 0)
        {
            throw new GraphQLException("CSV must contain WalletId column");
        }

        // First row of data becomes the template
        if (lines.Length > 1)
        {
            var firstDataRow = lines[1].Split(options?.Delimiter ?? ',');
            for (int j = 0; j < headers.Length; j++)
            {
                if (j != walletIdIndex)
                {
                    template[headers[j]] = firstDataRow[j];
                }
            }
        }

        // Collect all wallet IDs
        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(options?.Delimiter ?? ",");
            if (values.Length > walletIdIndex && Guid.TryParse(values[walletIdIndex], out var walletId))
            {
                walletIds.Add(walletId);
            }
        }

        return (walletIds, template);
    }

    private (List<Guid>, Dictionary<string, object>) ParseJsonCredentials(string content)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Deserialize<ImportData>(content);
            if (json == null)
            {
                throw new GraphQLException("Invalid JSON format");
            }

            return (json.WalletIds.Select(id => Guid.Parse(id)).ToList(), json.Template);
        }
        catch (Exception ex)
        {
            throw new GraphQLException($"Invalid JSON format: {ex.Message}");
        }
    }

    private class ImportData
    {
        public List<string> WalletIds { get; set; } = new();
        public Dictionary<string, object> Template { get; set; } = new();
    }
}

// GraphQL Input Types
public class BulkIssueCredentialsInput
{
    public List<string> WalletIds { get; set; } = new();
    public CredentialType CredentialType { get; set; }
    public Dictionary<string, object> Template { get; set; } = new();
    public string IssuerId { get; set; } = string.Empty;
    public string IssuerOrganizationId { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public class BulkRevokeCredentialsInput
{
    public List<string> CredentialIds { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
}

public class BulkVerifyCredentialsInput
{
    public List<string> CredentialIds { get; set; } = new();
    public VerificationOptionsInput? Options { get; set; }
}

public class VerificationOptionsInput
{
    public bool? CheckRevocation { get; set; }
    public bool? CheckExpiry { get; set; }
    public bool? CheckSignature { get; set; }
}

public class ImportCredentialsInput
{
    public CredentialType CredentialType { get; set; }
    public string IssuerId { get; set; } = string.Empty;
    public string IssuerOrganizationId { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public CsvImportOptions? CsvOptions { get; set; }
}

public class CsvImportOptions
{
    public string? Delimiter { get; set; }
    public string? WalletIdColumn { get; set; }
    public bool HasHeaders { get; set; } = true;
}

// Result types
public class BulkIssueResult
{
    public string OperationId { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> FailedIds { get; set; } = new();
    public Dictionary<string, string> Errors { get; set; } = new();
}

public class BulkRevokeResult
{
    public string OperationId { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> FailedIds { get; set; } = new();
}

public class BulkVerificationResult
{
    public int TotalCount { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public Dictionary<string, VerificationStatus> Results { get; set; } = new();
}