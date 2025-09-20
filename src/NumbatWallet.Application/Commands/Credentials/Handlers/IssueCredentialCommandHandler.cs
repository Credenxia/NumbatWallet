using System.Text.Json;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Exceptions;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Enums;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Commands.Credentials.Handlers;

public class IssueCredentialCommandHandler : ICommandHandler<IssueCredentialCommand, CredentialDto>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly ILogger<IssueCredentialCommandHandler> _logger;

    public IssueCredentialCommandHandler(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        IIssuerRepository issuerRepository,
        IEventDispatcher eventDispatcher,
        ILogger<IssueCredentialCommandHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _issuerRepository = issuerRepository;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<CredentialDto> HandleAsync(IssueCredentialCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Issuing credential of type {Type} for wallet {WalletId}",
            command.CredentialType, command.WalletId);

        // Get wallet
        var wallet = await _walletRepository.GetByIdAsync(command.WalletId, cancellationToken)
            ?? throw new NotFoundException($"Wallet with ID {command.WalletId} not found");

        // Check if wallet is active
        if (wallet.Status != WalletStatus.Active)
        {
            throw new BusinessRuleException("Cannot issue credential to inactive wallet");
        }

        // Get issuer
        var issuer = await _issuerRepository.GetByIdAsync(Guid.Parse(command.IssuerId), cancellationToken)
            ?? throw new NotFoundException($"Issuer with ID {command.IssuerId} not found");

        // Serialize claims data
        var credentialData = JsonSerializer.Serialize(command.Claims);

        // Create credential using mapped credential type string
        var credentialTypeString = command.CredentialType switch
        {
            CredentialType.DriversLicense => "DriversLicense",
            CredentialType.Passport => "Passport",
            CredentialType.StudentId => "StudentId",
            CredentialType.EmployeeId => "EmployeeId",
            CredentialType.HealthCard => "HealthCard",
            CredentialType.MembershipCard => "MembershipCard",
            CredentialType.ProofOfAddress => "ProofOfAddress",
            CredentialType.VaccinationCertificate => "VaccinationCertificate",
            CredentialType.ProfessionalLicense => "ProfessionalLicense",
            CredentialType.EducationalCertificate => "EducationalCertificate",
            CredentialType.ProofOfAge => "ProofOfAge",
            CredentialType.VerifiableCredential => "VerifiableCredential",
            CredentialType.Custom => "Custom",
            _ => "Custom"
        };

        // Create credential
        var credentialResult = Credential.Create(
            command.WalletId,
            issuer.Id,
            credentialTypeString,
            credentialData,
            $"schema:{credentialTypeString.ToLowerInvariant()}:1.0");

        if (credentialResult.IsFailure)
        {
            throw new BusinessRuleException(credentialResult.Error.Message);
        }

        var credential = credentialResult.Value;

        // Set tenant ID from wallet
        credential.SetTenantId(wallet.TenantId);

        // Set expiration if provided
        if (command.ValidUntil.HasValue)
        {
            credential.SetExpiry(new DateTimeOffset(command.ValidUntil.Value, TimeSpan.Zero));
        }

        // Activate the credential
        var activateResult = credential.Activate();
        if (activateResult.IsFailure)
        {
            throw new BusinessRuleException(activateResult.Error.Message);
        }

        // Add credential to repository
        await _credentialRepository.AddAsync(credential, cancellationToken);

        // Dispatch domain event
        var credentialIssuedEvent = new CredentialIssuedEvent(
            credential.Id,
            credential.WalletId,
            credential.IssuerId,
            credential.CredentialType,
            credential.CredentialId,
            credential.IssuedAt);

        await _eventDispatcher.DispatchAsync(credentialIssuedEvent, cancellationToken);

        _logger.LogInformation("Successfully issued credential {CredentialId} to wallet {WalletId}",
            credential.Id, command.WalletId);

        // Map to DTO
        return new CredentialDto
        {
            Id = credential.CredentialId,
            HolderId = credential.WalletId.ToString(),
            IssuerId = credential.IssuerId.ToString(),
            Type = credential.CredentialType,
            CredentialSubject = command.Claims,
            IssuanceDate = credential.IssuedAt.DateTime,
            ExpirationDate = credential.ExpiresAt?.DateTime,
            Status = credential.Status.ToString(),
            IsRevoked = credential.Status == CredentialStatus.Revoked,
            RevocationDate = credential.RevokedAt?.DateTime,
            RevocationReason = credential.RevocationReason
        };
    }
}