using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Enums;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Exceptions;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Credentials.Handlers;

/// <summary>
/// Handler for requesting credentials from issuers
/// </summary>
public class RequestCredentialCommandHandler : ICommandHandler<RequestCredentialCommand, CredentialRequestDto>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly IIssuanceRepository _issuanceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RequestCredentialCommandHandler> _logger;

    public RequestCredentialCommandHandler(
        IWalletRepository walletRepository,
        IIssuerRepository issuerRepository,
        IIssuanceRepository issuanceRepository,
        IUnitOfWork unitOfWork,
        ILogger<RequestCredentialCommandHandler> logger)
    {
        _walletRepository = walletRepository;
        _issuerRepository = issuerRepository;
        _issuanceRepository = issuanceRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CredentialRequestDto> HandleAsync(
        RequestCredentialCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing credential request from wallet {WalletId} to issuer {IssuerId}",
            command.WalletId, command.IssuerId);

        // Verify wallet exists and is active
        var wallet = await _walletRepository.GetByIdAsync(command.WalletId, cancellationToken);
        if (wallet == null)
        {
            throw new Application.Common.Exceptions.EntityNotFoundException("Wallet", command.WalletId.ToString());
        }

        if (wallet.Status != SharedKernel.Enums.WalletStatus.Active)
        {
            throw new BusinessRuleException("WALLET_INACTIVE", "Wallet must be active to request credentials");
        }

        // Verify issuer exists and is active
        var issuer = await _issuerRepository.GetByIdAsync(command.IssuerId, cancellationToken);
        if (issuer == null)
        {
            throw new Application.Common.Exceptions.EntityNotFoundException("Issuer", command.IssuerId.ToString());
        }

        if (!issuer.IsActive)
        {
            throw new BusinessRuleException("ISSUER_INACTIVE", "Issuer is not active");
        }

        // Get tenant ID from wallet - parse as GUID
        var tenantId = Guid.Parse(wallet.TenantId);

        // Create an issuance request
        var issuance = new Issuance(
            tenantId: tenantId,
            credentialType: command.CredentialType,
            walletId: command.WalletId,
            requesterId: command.WalletId.ToString(), // Requester is the wallet holder
            claims: command.RequestedClaims,
            expiryDate: DateTime.UtcNow.AddYears(1)); // Default 1 year expiry

        await _issuanceRepository.AddAsync(issuance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new CredentialRequestDto
        {
            RequestId = Guid.NewGuid(), // In production, use the actual issuance ID
            Status = "Pending",
            RequestedAt = DateTime.UtcNow,
            Message = $"Your credential request has been submitted to {issuer.Name} and is pending approval.",
            IssuanceId = issuance.Id
        };

        _logger.LogInformation("Created credential request {RequestId} for issuance {IssuanceId}",
            result.RequestId, issuance.Id);

        return result;
    }
}