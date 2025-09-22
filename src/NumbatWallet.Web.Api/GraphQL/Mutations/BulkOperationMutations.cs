using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Subscriptions;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.SharedKernel.Models;
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
        // Convert input to command
        var command = new BulkIssueCredentialsCommand
        {
            IssuerId = input.IssuerId,
            Credentials = input.Credentials.Select(c => new BulkCredentialRequest
            {
                SubjectId = c.SubjectId,
                CredentialType = c.CredentialType,
                Claims = c.Claims.ToDictionary(x => x.Key, x => (object)x.Value),
                ExpiresAt = c.ExpiresAt,
                Metadata = c.Metadata ?? new Dictionary<string, string>()
            }).ToList(),
            Options = new BulkProcessingOptions
            {
                ContinueOnError = input.Options?.ContinueOnError ?? true,
                MaxConcurrency = input.Options?.MaxConcurrency ?? 10,
                EnableProgressTracking = input.Options?.EnableProgressTracking ?? true,
                ValidateBeforeProcessing = input.Options?.ValidateBeforeProcessing ?? true,
                Timeout = input.Options?.TimeoutSeconds.HasValue
                    ? TimeSpan.FromSeconds(input.Options.TimeoutSeconds.Value)
                    : TimeSpan.FromMinutes(5)
            }
        };

        // Execute command
        var result = await dispatcher.DispatchAsync<BulkIssueCredentialsCommand, BulkIssueResult>(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            throw new GraphQLException(result.Error.Message);
        }

        // Send initial subscription event
        await eventSender.SendAsync(
            $"OnBulkOperationStarted_{result.Value.OperationId}",
            result.Value,
            cancellationToken);

        return result.Value;
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
        var command = new BulkRevokeCredentialsCommand
        {
            CredentialIds = input.CredentialIds,
            Reason = input.Reason,
            Options = new BulkProcessingOptions
            {
                ContinueOnError = input.Options?.ContinueOnError ?? true,
                MaxConcurrency = input.Options?.MaxConcurrency ?? 10,
                EnableProgressTracking = input.Options?.EnableProgressTracking ?? true
            }
        };

        var result = await dispatcher.DispatchAsync<BulkRevokeCredentialsCommand, BulkRevokeResult>(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            throw new GraphQLException(result.Error.Message);
        }

        await eventSender.SendAsync(
            $"OnBulkOperationStarted_{result.Value.OperationId}",
            result.Value,
            cancellationToken);

        return result.Value;
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
        var command = new BulkVerifyCredentialsCommand
        {
            CredentialIds = input.CredentialIds,
            VerificationOptions = new VerificationOptions
            {
                CheckRevocation = input.Options?.CheckRevocation ?? true,
                CheckExpiry = input.Options?.CheckExpiry ?? true,
                CheckSignature = input.Options?.CheckSignature ?? true
            }
        };

        var result = await dispatcher.DispatchAsync<BulkVerifyCredentialsCommand, BulkVerificationResult>(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            throw new GraphQLException(result.Error.Message);
        }

        return result.Value;
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
        List<BulkCredentialRequest> credentials;
        if (file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            credentials = ParseCsvCredentials(content, input.CsvOptions);
        }
        else if (file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            credentials = ParseJsonCredentials(content);
        }
        else
        {
            throw new GraphQLException("Unsupported file format. Use CSV or JSON.");
        }

        // Create command
        var command = new BulkIssueCredentialsCommand
        {
            IssuerId = input.IssuerId,
            Credentials = credentials,
            Options = new BulkProcessingOptions
            {
                ContinueOnError = input.Options?.ContinueOnError ?? true,
                MaxConcurrency = input.Options?.MaxConcurrency ?? 10,
                EnableProgressTracking = true,
                ValidateBeforeProcessing = true
            }
        };

        var result = await dispatcher.DispatchAsync<BulkIssueCredentialsCommand, BulkIssueResult>(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            throw new GraphQLException(result.Error.Message);
        }

        await eventSender.SendAsync(
            $"OnBulkOperationStarted_{result.Value.OperationId}",
            result.Value,
            cancellationToken);

        return result.Value;
    }

    private List<BulkCredentialRequest> ParseCsvCredentials(string content, CsvImportOptions? options)
    {
        var credentials = new List<BulkCredentialRequest>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            throw new GraphQLException("CSV file must contain headers and at least one data row");
        }

        var headers = lines[0].Split(options?.Delimiter ?? ',');
        var subjectIdIndex = Array.IndexOf(headers, options?.SubjectIdColumn ?? "SubjectId");
        var typeIndex = Array.IndexOf(headers, options?.CredentialTypeColumn ?? "CredentialType");

        if (subjectIdIndex < 0 || typeIndex < 0)
        {
            throw new GraphQLException("CSV must contain SubjectId and CredentialType columns");
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(options?.Delimiter ?? ',');

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
                SubjectId = values[subjectIdIndex],
                CredentialType = values[typeIndex],
                Claims = claims
            });
        }

        return credentials;
    }

    private List<BulkCredentialRequest> ParseJsonCredentials(string content)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Deserialize<List<BulkCredentialRequest>>(content);
            return json ?? new List<BulkCredentialRequest>();
        }
        catch (Exception ex)
        {
            throw new GraphQLException($"Invalid JSON format: {ex.Message}");
        }
    }
}

// GraphQL Input Types
public class BulkIssueCredentialsInput
{
    public string IssuerId { get; set; } = string.Empty;
    public List<BulkCredentialInput> Credentials { get; set; } = new();
    public BulkProcessingOptionsInput? Options { get; set; }
}

public class BulkCredentialInput
{
    public string SubjectId { get; set; } = string.Empty;
    public string CredentialType { get; set; } = string.Empty;
    public Dictionary<string, string> Claims { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class BulkProcessingOptionsInput
{
    public bool? ContinueOnError { get; set; }
    public int? MaxConcurrency { get; set; }
    public bool? EnableProgressTracking { get; set; }
    public bool? ValidateBeforeProcessing { get; set; }
    public int? TimeoutSeconds { get; set; }
}

public class BulkRevokeCredentialsInput
{
    public List<string> CredentialIds { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public BulkProcessingOptionsInput? Options { get; set; }
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
    public string IssuerId { get; set; } = string.Empty;
    public BulkProcessingOptionsInput? Options { get; set; }
    public CsvImportOptions? CsvOptions { get; set; }
}

public class CsvImportOptions
{
    public char? Delimiter { get; set; }
    public string? SubjectIdColumn { get; set; }
    public string? CredentialTypeColumn { get; set; }
}

// Additional types and commands
public class BulkCredentialRequest
{
    public Guid SubjectId { get; set; }
    public string CredentialType { get; set; } = string.Empty;
    public Dictionary<string, object> Claims { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class BulkProcessingOptions
{
    public bool ContinueOnError { get; set; } = true;
    public int MaxConcurrency { get; set; } = 10;
    public bool EnableProgressTracking { get; set; } = true;
    public bool ValidateBeforeProcessing { get; set; } = true;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
}

public class BulkRevokeCredentialsCommand : ICommand<BulkRevokeResult>
{
    public List<string> CredentialIds { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public BulkProcessingOptions Options { get; set; } = new();
}

public class BulkVerifyCredentialsCommand : ICommand<BulkVerificationResult>
{
    public List<string> CredentialIds { get; set; } = new();
    public VerificationOptions VerificationOptions { get; set; } = new();
}

public class BulkVerificationResult
{
    public string OperationId { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public List<VerificationResultItem> Results { get; set; } = new();
    public DateTime VerifiedAt { get; set; }
}

public class VerificationResultItem
{
    public string CredentialId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}

public class VerificationOptions
{
    public bool CheckRevocation { get; set; } = true;
    public bool CheckExpiry { get; set; } = true;
    public bool CheckSignature { get; set; } = true;
}