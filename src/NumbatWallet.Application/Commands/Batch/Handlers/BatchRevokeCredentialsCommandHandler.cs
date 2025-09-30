using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.CQRS.Interfaces;
using System.Collections.Concurrent;

namespace NumbatWallet.Application.Commands.Batch.Handlers;

/// <summary>
/// Handler for batch credential revocation
/// </summary>
public class BatchRevokeCredentialsCommandHandler : ICommandHandler<BatchRevokeCredentialsCommand, BatchOperationResultDto<bool>>
{
    private readonly ICommandHandler<RevokeCredentialCommand, bool> _revokeCredentialHandler;
    private readonly ILogger<BatchRevokeCredentialsCommandHandler> _logger;

    public BatchRevokeCredentialsCommandHandler(
        ICommandHandler<RevokeCredentialCommand, bool> revokeCredentialHandler,
        ILogger<BatchRevokeCredentialsCommandHandler> logger)
    {
        _revokeCredentialHandler = revokeCredentialHandler;
        _logger = logger;
    }

    public async Task<BatchOperationResultDto<bool>> HandleAsync(
        BatchRevokeCredentialsCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing batch credential revocation for {Count} items", command.Credentials.Count);

        var results = new ConcurrentBag<BatchOperationItemResult<bool>>();
        var tasks = new List<Task>();

        // Process each credential revocation in parallel
        foreach (var credRequest in command.Credentials)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Parse credential ID
                    if (!Guid.TryParse(credRequest.CredentialId, out var credentialId))
                    {
                        throw new ArgumentException($"Invalid credential ID: {credRequest.CredentialId}");
                    }

                    // Create revoke credential command
                    var revokeCommand = new RevokeCredentialCommand(
                        CredentialId: credentialId,
                        Reason: credRequest.Reason,
                        RevokerId: command.RevokerId);

                    // Revoke the credential
                    var result = await _revokeCredentialHandler.HandleAsync(revokeCommand, cancellationToken);

                    results.Add(new BatchOperationItemResult<bool>
                    {
                        Success = true,
                        Data = result,
                        ItemId = credRequest.BatchItemId
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to revoke credential for batch item {ItemId}", credRequest.BatchItemId);
                    results.Add(new BatchOperationItemResult<bool>
                    {
                        Success = false,
                        Error = ex.Message,
                        ItemId = credRequest.BatchItemId
                    });
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        var response = new BatchOperationResultDto<bool>
        {
            TotalItems = command.Credentials.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results.ToList(),
            ProcessedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Batch credential revocation completed: {SuccessCount}/{TotalItems} succeeded",
            response.SuccessCount, response.TotalItems);

        return response;
    }
}