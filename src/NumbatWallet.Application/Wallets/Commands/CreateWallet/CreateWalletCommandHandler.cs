using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Extensions;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Application.DomainServices;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Wallets.Commands.CreateWallet;

public sealed class CreateWalletCommandHandler : ICommandHandler<CreateWalletCommand, WalletDto>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateWalletCommandHandler> _logger;
    private readonly ITenantService _tenantService;
    private readonly IWalletDomainService _walletDomainService;
    private readonly IHsmService _hsmService;

    public CreateWalletCommandHandler(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateWalletCommandHandler> logger,
        ITenantService tenantService,
        IWalletDomainService walletDomainService,
        IHsmService hsmService)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _tenantService = tenantService;
        _walletDomainService = walletDomainService;
        _hsmService = hsmService;
    }

    public async Task<WalletDto> HandleAsync(
        CreateWalletCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating {Type} wallet for person {PersonId}", command.Type, command.PersonId);

        // Determine tenant ID
        var tenantId = command.TenantId ?? _tenantService.TenantId.ToString();

        // Verify person exists
        var person = await _personRepository.GetByIdAsync(command.PersonId, cancellationToken);
        if (person == null)
        {
            throw new NotFoundException(nameof(Person), command.PersonId);
        }

        // Check if person already has a wallet in this tenant
        var walletExists = await _walletRepository.WalletExistsForPersonAsync(
            command.PersonId,
            Guid.Parse(tenantId),
            cancellationToken);

        if (walletExists)
        {
            throw new ConflictException($"Person already has a wallet in this tenant.");
        }

        // Create wallet with type
        var walletResult = Wallet.Create(
            command.PersonId,
            command.Name,
            command.Type);

        if (!walletResult.IsSuccess)
        {
            throw new ConflictException($"Failed to create wallet: {walletResult.Error.Message}");
        }

        var wallet = walletResult.Value;

        // Set the tenant ID
        wallet.SetTenantId(tenantId);

        // Generate DID using did:key method
        var didKeyName = $"wallet-{wallet.Id}";
        var didKeyId = await _hsmService.GenerateKeyPairAsync(
            didKeyName,
            KeyAlgorithm.ECC_P256,
            cancellationToken);

        // Create did:key format DID
        var walletDid = $"did:key:{didKeyId}";
        // The DID is already set in the Wallet aggregate during creation

        _logger.LogInformation("Generated DID {WalletDid} for wallet {WalletId}", walletDid, wallet.Id);

        // Save to repository
        await _walletRepository.AddAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet {WalletId} created successfully with DID {WalletDid}", wallet.Id, walletDid);

        // Convert to DTO and return
        return wallet.ToDto();
    }
}
