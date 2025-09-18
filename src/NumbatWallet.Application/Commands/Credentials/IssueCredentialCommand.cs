using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Repositories;
using NumbatWallet.Domain.Specifications;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Credentials;

public record IssueCredentialCommand : ICommand<CredentialDto>
{
    public required string HolderId { get; init; }
    public required string IssuerId { get; init; }
    public required string CredentialType { get; init; }
    public required Dictionary<string, object> CredentialSubject { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

public class IssueCredentialCommandHandler : ICommandHandler<IssueCredentialCommand, CredentialDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly ICryptoService _cryptoService;
    private readonly ILogger<IssueCredentialCommandHandler> _logger;
    private readonly ITenantService _tenantService;

    public IssueCredentialCommandHandler(
        IUnitOfWork unitOfWork,
        ICredentialRepository credentialRepository,
        IPersonRepository personRepository,
        IWalletRepository walletRepository,
        IIssuerRepository issuerRepository,
        ICryptoService cryptoService,
        ILogger<IssueCredentialCommandHandler> logger,
        ITenantService tenantService)
    {
        _unitOfWork = unitOfWork;
        _credentialRepository = credentialRepository;
        _personRepository = personRepository;
        _walletRepository = walletRepository;
        _issuerRepository = issuerRepository;
        _cryptoService = cryptoService;
        _logger = logger;
        _tenantService = tenantService;
    }

    public async Task<CredentialDto> HandleAsync(
        IssueCredentialCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing IssueCredentialCommand for holder {HolderId}", command.HolderId);

        // Validate holder exists
        var holderId = Guid.Parse(command.HolderId);
        var holder = await _personRepository.GetByIdAsync(holderId, cancellationToken);
        if (holder == null)
        {
            throw new EntityNotFoundException("Person", command.HolderId);
        }

        // Validate issuer exists and is authorized
        var issuerId = Guid.Parse(command.IssuerId);
        var issuer = await _issuerRepository.GetByIdAsync(issuerId, cancellationToken);
        if (issuer == null || !issuer.IsActive)
        {
            throw new UnauthorizedIssuerException(command.IssuerId);
        }

        // Get wallet for the holder
        var wallets = await _walletRepository.FindAsync(new WalletByPersonSpecification(holderId), cancellationToken);
        var wallet = wallets.FirstOrDefault();
        if (wallet == null)
        {
            throw new EntityNotFoundException("Wallet", $"for person {command.HolderId}");
        }

        // Serialize credential subject to JSON
        var credentialData = System.Text.Json.JsonSerializer.Serialize(command.CredentialSubject);

        // Create credential aggregate
        var credentialResult = Credential.Create(
            walletId: wallet.Id,
            issuerId: issuerId,
            credentialType: command.CredentialType,
            credentialData: credentialData,
            schemaId: command.Metadata?.GetValueOrDefault("schemaId") ?? "default-schema");

        if (credentialResult.IsFailure)
        {
            throw new DomainValidationException(credentialResult.Error.Message);
        }

        var credential = credentialResult.Value;

        // Set expiration if provided
        if (command.ExpirationDate.HasValue)
        {
            // Note: The domain model doesn't have a SetExpiration method,
            // so this would need to be added or handled differently
        }

        // Save credential
        await _credentialRepository.AddAsync(credential, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Credential {CredentialId} issued successfully", credential.Id);

        // Map to DTO
        return MapToDto(credential);
    }

    private async Task<Dictionary<string, object>> GenerateProofAsync(Credential credential, Issuer issuer)
    {
        // Generate digital signature using issuer's private key
        var proofData = new Dictionary<string, object>
        {
            ["type"] = "RsaSignature2018",
            ["created"] = DateTime.UtcNow.ToString("O"),
            ["verificationMethod"] = $"{issuer.Did}#key-1",
            ["proofPurpose"] = "assertionMethod"
        };

        // In production, this would create actual cryptographic signature
        // For now, we'll use a placeholder
        var dataToSign = System.Text.Json.JsonSerializer.Serialize(credential.CredentialSubject);
        var encryptedSignature = await _cryptoService.EncryptAsync(dataToSign, DataClassification.Protected);
        proofData["jws"] = encryptedSignature;

        return proofData;
    }

    private CredentialDto MapToDto(Credential credential)
    {
        return new CredentialDto
        {
            Id = credential.Id.ToString(),
            HolderId = credential.HolderId.Value,
            IssuerId = credential.IssuerId.Value,
            Type = credential.Type,
            CredentialSubject = credential.CredentialSubject,
            IssuanceDate = credential.IssuanceDate,
            ExpirationDate = credential.ExpirationDate,
            Status = credential.Status.ToString(),
            Proof = credential.Proof,
            Metadata = credential.Metadata
        };
    }
}