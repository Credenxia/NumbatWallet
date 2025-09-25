using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Commands.Issuances;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using System.Collections.Concurrent;

namespace NumbatWallet.Application.Commands.Batch.Handlers;

/// <summary>
/// Handler for batch issuance approval
/// </summary>
public class BatchApproveIssuancesCommandHandler : ICommandHandler<BatchApproveIssuancesCommand, BatchOperationResultDto<IssuanceDto>>
{
    private readonly ICommandHandler<ApproveIssuanceCommand, IssuanceDto> _approveIssuanceHandler;
    private readonly ILogger<BatchApproveIssuancesCommandHandler> _logger;

    public BatchApproveIssuancesCommandHandler(
        ICommandHandler<ApproveIssuanceCommand, IssuanceDto> approveIssuanceHandler,
        ILogger<BatchApproveIssuancesCommandHandler> logger)
    {
        _approveIssuanceHandler = approveIssuanceHandler;
        _logger = logger;
    }

    public async Task<BatchOperationResultDto<IssuanceDto>> HandleAsync(
        BatchApproveIssuancesCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing batch issuance approval for {Count} items", command.IssuanceIds.Count);

        var results = new ConcurrentBag<BatchOperationItemResult<IssuanceDto>>();
        var tasks = new List<Task>();

        // Process each issuance approval in parallel
        foreach (var issuanceId in command.IssuanceIds)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Create approve issuance command
                    var approveCommand = new ApproveIssuanceCommand(
                        IssuanceId: issuanceId,
                        ApprovedBy: command.ApproverId,
                        Comments: "Approved via batch operation");

                    // Approve the issuance
                    var issuance = await _approveIssuanceHandler.HandleAsync(approveCommand, cancellationToken);

                    results.Add(new BatchOperationItemResult<IssuanceDto>
                    {
                        Success = true,
                        Data = issuance,
                        ItemId = issuanceId.ToString()
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to approve issuance {IssuanceId}", issuanceId);
                    results.Add(new BatchOperationItemResult<IssuanceDto>
                    {
                        Success = false,
                        Error = ex.Message,
                        ItemId = issuanceId.ToString()
                    });
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        var response = new BatchOperationResultDto<IssuanceDto>
        {
            TotalItems = command.IssuanceIds.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results.ToList(),
            ProcessedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Batch issuance approval completed: {SuccessCount}/{TotalItems} succeeded",
            response.SuccessCount, response.TotalItems);

        return response;
    }
}