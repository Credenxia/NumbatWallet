using AutoMapper;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Wallets.DTOs;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Wallets.Commands.CreateWallet;

public sealed class CreateWalletCommandHandler : ICommandHandler<CreateWalletCommand, WalletDto>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateWalletCommandHandler> _logger;
    private readonly ITenantService _tenantService;

    public CreateWalletCommandHandler(
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateWalletCommandHandler> logger,
        ITenantService tenantService)
    {
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _tenantService = tenantService;
    }

    public async Task<WalletDto> HandleAsync(
        CreateWalletCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating wallet for person {PersonId}", command.PersonId);

        // Verify person exists
        var person = await _personRepository.GetByIdAsync(command.PersonId, cancellationToken);
        if (person == null)
        {
            throw new NotFoundException(nameof(Person), command.PersonId);
        }

        // Check if person already has a wallet
        var walletExists = await _walletRepository.WalletExistsForPersonAsync(
            command.PersonId,
            _tenantService.TenantId,
            cancellationToken);

        if (walletExists)
        {
            throw new ConflictException($"Wallet with name '{command.Name}' already exists for this person.");
        }

        // Create wallet
        var walletResult = Wallet.Create(
            command.PersonId,
            command.Name);

        if (!walletResult.IsSuccess)
        {
            throw new ConflictException($"Failed to create wallet: {walletResult.Error.Message}");
        }

        var wallet = walletResult.Value;

        // Set the tenant ID
        wallet.SetTenantId(_tenantService.TenantId.ToString());

        // Save to repository
        await _walletRepository.AddAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet {WalletId} created successfully", wallet.Id);

        // Map to DTO and return
        return _mapper.Map<WalletDto>(wallet);
    }
}