using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using System.Collections.Concurrent;

namespace NumbatWallet.Application.Commands.Batch.Handlers;

/// <summary>
/// Handler for batch credential verification
/// </summary>
public class BatchVerifyCredentialsCommandHandler : ICommandHandler<BatchVerifyCredentialsCommand, BatchOperationResultDto<VerificationResultDto>>
{
    private readonly ICommandHandler<VerifyCredentialCommand, VerificationResultDto> _verifyCredentialHandler;
    private readonly ILogger<BatchVerifyCredentialsCommandHandler> _logger;

    public BatchVerifyCredentialsCommandHandler(
        ICommandHandler<VerifyCredentialCommand, VerificationResultDto> verifyCredentialHandler,
        ILogger<BatchVerifyCredentialsCommandHandler> logger)
    {
        _verifyCredentialHandler = verifyCredentialHandler;
        _logger = logger;
    }

    public async Task<BatchOperationResultDto<VerificationResultDto>> HandleAsync(
        BatchVerifyCredentialsCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing batch credential verification for {Count} items", command.Credentials.Count);

        var results = new ConcurrentBag<BatchOperationItemResult<VerificationResultDto>>();
        var tasks = new List<Task>();

        // Process each credential verification in parallel
        foreach (var credRequest in command.Credentials)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Create verify credential command
                    var verifyCommand = new VerifyCredentialCommand
                    {
                        CredentialId = credRequest.CredentialId,
                        CredentialData = credRequest.CredentialData,
                        VerificationOptions = new Dictionary<string, object>
                        {
                            ["CheckRevocation"] = true,
                            ["CheckExpiry"] = true,
                            ["CheckSignature"] = true
                        }
                    };

                    // Verify the credential
                    var verificationResult = await _verifyCredentialHandler.HandleAsync(verifyCommand, cancellationToken);

                    results.Add(new BatchOperationItemResult<VerificationResultDto>
                    {
                        Success = true,
                        Data = verificationResult,
                        ItemId = credRequest.BatchItemId
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to verify credential for batch item {ItemId}", credRequest.BatchItemId);
                    results.Add(new BatchOperationItemResult<VerificationResultDto>
                    {
                        Success = false,
                        Error = ex.Message,
                        ItemId = credRequest.BatchItemId
                    });
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        var response = new BatchOperationResultDto<VerificationResultDto>
        {
            TotalItems = command.Credentials.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results.ToList(),
            ProcessedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Batch credential verification completed: {SuccessCount}/{TotalItems} succeeded",
            response.SuccessCount, response.TotalItems);

        return response;
    }
}