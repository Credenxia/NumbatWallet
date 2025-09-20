using System.Text.Json;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Exceptions;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Enums;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Commands.Credentials.Handlers;

public class BulkIssueCredentialsCommandHandler : ICommandHandler<BulkIssueCredentialsCommand, BulkIssueResult>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly ILogger<BulkIssueCredentialsCommandHandler> _logger;

    public BulkIssueCredentialsCommandHandler(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        IIssuerRepository issuerRepository,
        IEventDispatcher eventDispatcher,
        ILogger<BulkIssueCredentialsCommandHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _issuerRepository = issuerRepository;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<BulkIssueResult> HandleAsync(BulkIssueCredentialsCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bulk issuing {Type} credentials to {Count} wallets",
            command.CredentialType, command.WalletIds.Count);

        // Get issuer
        var issuer = await _issuerRepository.GetByIdAsync(Guid.Parse(command.IssuerId), cancellationToken)
            ?? throw new NotFoundException($"Issuer with ID {command.IssuerId} not found");

        var issuedCredentialIds = new List<Guid>();
        var errors = new List<BulkIssueError>();

        // Serialize template data once
        var credentialData = JsonSerializer.Serialize(command.Template);

        // Create credential type string
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

        // Process each wallet
        foreach (var walletId in command.WalletIds)
        {
            try
            {
                // Get wallet
                var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);
                if (wallet == null)
                {
                    _logger.LogWarning("Wallet {WalletId} not found during bulk issuance", walletId);
                    errors.Add(new BulkIssueError(walletId, $"Wallet with ID {walletId} not found"));
                    continue;
                }

                // Check if wallet is active
                if (wallet.Status != WalletStatus.Active)
                {
                    _logger.LogWarning("Wallet {WalletId} is not active during bulk issuance", walletId);
                    errors.Add(new BulkIssueError(walletId, "Wallet is not active"));
                    continue;
                }

                // Create credential
                var credentialResult = Credential.Create(
                    walletId,
                    issuer.Id,
                    credentialTypeString,
                    credentialData,
                    $"schema:{credentialTypeString.ToLowerInvariant()}:1.0");

                if (credentialResult.IsFailure)
                {
                    _logger.LogWarning("Failed to create credential for wallet {WalletId}: {Error}",
                        walletId, credentialResult.Error.Message);
                    errors.Add(new BulkIssueError(walletId, credentialResult.Error.Message));
                    continue;
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
                    _logger.LogWarning("Failed to activate credential for wallet {WalletId}: {Error}",
                        walletId, activateResult.Error.Message);
                    errors.Add(new BulkIssueError(walletId, activateResult.Error.Message));
                    continue;
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

                // Add to successful list
                issuedCredentialIds.Add(credential.Id);

                _logger.LogInformation("Successfully issued credential {CredentialId} to wallet {WalletId}",
                    credential.Id, walletId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error issuing credential to wallet {WalletId}", walletId);
                errors.Add(new BulkIssueError(walletId, $"Unexpected error: {ex.Message}"));
            }
        }

        var result = new BulkIssueResult(
            command.WalletIds.Count,
            issuedCredentialIds.Count,
            errors.Count,
            issuedCredentialIds,
            errors);

        _logger.LogInformation("Bulk issuance complete: {Success}/{Total} successful",
            result.SuccessCount, result.TotalRequested);

        return result;
    }
}