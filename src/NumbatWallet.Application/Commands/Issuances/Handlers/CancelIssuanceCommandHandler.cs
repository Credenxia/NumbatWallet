using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Commands.Issuances.Handlers;

public class CancelIssuanceCommandHandler : ICommandHandler<CancelIssuanceCommand, bool>
{
    private readonly IIssuanceRepository _issuanceRepository;
    private readonly ILogger<CancelIssuanceCommandHandler> _logger;

    public CancelIssuanceCommandHandler(
        IIssuanceRepository issuanceRepository,
        ILogger<CancelIssuanceCommandHandler> logger)
    {
        _issuanceRepository = issuanceRepository;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(CancelIssuanceCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling issuance request {IssuanceId} with reason: {Reason}",
            command.IssuanceId, command.Reason);

        // Get the issuance
        var issuance = await _issuanceRepository.GetByIdAsync(command.IssuanceId, cancellationToken);
        if (issuance == null)
        {
            throw new InvalidOperationException($"Issuance {command.IssuanceId} not found");
        }

        // Cancel the issuance
        issuance.Cancel(command.CancelledBy, command.Reason);

        // Update in repository
        await _issuanceRepository.UpdateAsync(issuance, cancellationToken);

        _logger.LogInformation("Cancelled issuance request {IssuanceId} with status {Status}",
            issuance.Id, issuance.Status);

        return true;
    }
}