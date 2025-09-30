using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Enums;
using System.Collections.Concurrent;

namespace NumbatWallet.Application.Commands.Batch.Handlers;

/// <summary>
/// Handler for batch credential issuance
/// </summary>
public class BatchIssueCredentialsCommandHandler : ICommandHandler<BatchIssueCredentialsCommand, BatchOperationResultDto<CredentialDto>>
{
    private readonly ICommandHandler<IssueCredentialCommand, CredentialDto> _issueCredentialHandler;
    private readonly ILogger<BatchIssueCredentialsCommandHandler> _logger;

    public BatchIssueCredentialsCommandHandler(
        ICommandHandler<IssueCredentialCommand, CredentialDto> issueCredentialHandler,
        ILogger<BatchIssueCredentialsCommandHandler> logger)
    {
        _issueCredentialHandler = issueCredentialHandler;
        _logger = logger;
    }

    public async Task<BatchOperationResultDto<CredentialDto>> HandleAsync(
        BatchIssueCredentialsCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing batch credential issuance for {Count} items", command.Credentials.Count);

        var results = new ConcurrentBag<BatchOperationItemResult<CredentialDto>>();
        var tasks = new List<Task>();

        // Process each credential in parallel
        foreach (var credRequest in command.Credentials)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Parse credential type
                    if (!Enum.TryParse<CredentialType>(credRequest.Type, true, out var credentialType))
                    {
                        throw new ArgumentException($"Invalid credential type: {credRequest.Type}");
                    }

                    // Parse wallet ID
                    if (!Guid.TryParse(credRequest.HolderId, out var walletId))
                    {
                        throw new ArgumentException($"Invalid wallet ID: {credRequest.HolderId}");
                    }

                    // Create issue credential command
                    var issueCommand = new IssueCredentialCommand(
                        WalletId: walletId,
                        CredentialType: credentialType,
                        Subject: $"Credential for {credRequest.HolderId}",
                        Claims: credRequest.Claims,
                        ValidFrom: DateTime.UtcNow,
                        ValidUntil: credRequest.ExpiryDate,
                        IssuerId: command.IssuerId,
                        IssuerOrganizationId: Guid.Empty); // Will be set from tenant context

                    // Issue the credential
                    var credential = await _issueCredentialHandler.HandleAsync(issueCommand, cancellationToken);

                    results.Add(new BatchOperationItemResult<CredentialDto>
                    {
                        Success = true,
                        Data = credential,
                        ItemId = credRequest.BatchItemId
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to issue credential for batch item {ItemId}", credRequest.BatchItemId);
                    results.Add(new BatchOperationItemResult<CredentialDto>
                    {
                        Success = false,
                        Error = ex.Message,
                        ItemId = credRequest.BatchItemId
                    });
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        var response = new BatchOperationResultDto<CredentialDto>
        {
            TotalItems = command.Credentials.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results.ToList(),
            ProcessedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Batch credential issuance completed: {SuccessCount}/{TotalItems} succeeded",
            response.SuccessCount, response.TotalItems);

        return response;
    }
}