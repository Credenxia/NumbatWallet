using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Repositories;
using NumbatWallet.Domain.ValueObjects;
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
    private readonly IIssuerRepository _issuerRepository;
    private readonly ICryptoService _cryptoService;
    private readonly ILogger<IssueCredentialCommandHandler> _logger;
    private readonly ITenantService _tenantService;

    public IssueCredentialCommandHandler(
        IUnitOfWork unitOfWork,
        ICredentialRepository credentialRepository,
        IPersonRepository personRepository,
        IIssuerRepository issuerRepository,
        ICryptoService cryptoService,
        ILogger<IssueCredentialCommandHandler> logger,
        ITenantService tenantService)
    {
        _unitOfWork = unitOfWork;
        _credentialRepository = credentialRepository;
        _personRepository = personRepository;
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
        var holderId = PersonId.From(command.HolderId);
        var holder = await _personRepository.GetByIdAsync(holderId, cancellationToken);
        if (holder == null)
        {
            throw new EntityNotFoundException("Person", command.HolderId);
        }

        // Validate issuer exists and is authorized
        var issuerId = IssuerId.From(command.IssuerId);
        var issuer = await _issuerRepository.GetByIdAsync(issuerId, cancellationToken);
        if (issuer == null || !issuer.IsActive)
        {
            throw new UnauthorizedIssuerException(command.IssuerId);
        }

        // Create credential aggregate
        var credentialResult = Credential.Create(
            holderId: holderId,
            issuerId: issuerId,
            type: command.CredentialType,
            credentialSubject: command.CredentialSubject,
            expirationDate: command.ExpirationDate,
            metadata: command.Metadata);

        if (credentialResult.IsFailure)
        {
            throw new DomainValidationException(credentialResult.Error.Message);
        }

        var credential = credentialResult.Value;

        // Generate cryptographic proof
        var proofData = await GenerateProofAsync(credential, issuer);
        credential.SetProof(proofData);

        // Save credential
        await _credentialRepository.AddAsync(credential, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

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