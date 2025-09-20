using System.Text.Json;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Exceptions;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Commands.Credentials.Handlers;

public class PresentCredentialCommandHandler : ICommandHandler<PresentCredentialCommand, PresentationResult>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IVerificationService _verificationService;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly ILogger<PresentCredentialCommandHandler> _logger;

    public PresentCredentialCommandHandler(
        ICredentialRepository credentialRepository,
        IVerificationService verificationService,
        IEventDispatcher eventDispatcher,
        ILogger<PresentCredentialCommandHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _verificationService = verificationService;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<PresentationResult> HandleAsync(PresentCredentialCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Presenting credential {CredentialId} to verifier {VerifierId}",
            command.CredentialId, command.VerifierId);

        // Get credential
        var credential = await _credentialRepository.GetByIdAsync(command.CredentialId, cancellationToken)
            ?? throw new NotFoundException($"Credential with ID {command.CredentialId} not found");

        // Check if credential can be presented
        if (credential.IsExpired())
        {
            throw new BusinessRuleException("Cannot present expired credential");
        }

        if (credential.Status != CredentialStatus.Active)
        {
            throw new BusinessRuleException("Cannot present inactive credential");
        }

        // Parse credential data to get claims
        var allClaims = JsonSerializer.Deserialize<Dictionary<string, object>>(credential.CredentialData)
            ?? new Dictionary<string, object>();

        // Apply selective disclosure if requested
        var disclosedClaims = command.SelectiveDisclosure != null && command.SelectiveDisclosure.Any()
            ? allClaims.Where(kvp => command.SelectiveDisclosure.Contains(kvp.Key))
                      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            : allClaims;

        // Create presentation token
        var presentationToken = await _verificationService.CreatePresentationTokenAsync(
            credential.Id,
            command.VerifierId,
            command.Purpose,
            disclosedClaims,
            cancellationToken);

        // Generate verification URL
        var verificationUrl = await _verificationService.CreateVerificationUrlAsync(
            presentationToken,
            cancellationToken);

        // Dispatch domain event
        var presentedAt = DateTimeOffset.UtcNow;
        var credentialPresentedEvent = new CredentialPresentedEvent(
            credential.Id,
            credential.WalletId,
            command.VerifierId,
            command.Purpose,
            presentedAt);

        await _eventDispatcher.DispatchAsync(credentialPresentedEvent, cancellationToken);

        _logger.LogInformation("Successfully presented credential {CredentialId}", command.CredentialId);

        return new PresentationResult(
            presentationToken,
            verificationUrl,
            presentedAt.DateTime,
            disclosedClaims);
    }
}

// Interface for verification service
public interface IVerificationService
{
    Task<string> CreatePresentationTokenAsync(
        Guid credentialId,
        string verifierId,
        string purpose,
        Dictionary<string, object> disclosedClaims,
        CancellationToken cancellationToken);

    Task<string> CreateVerificationUrlAsync(
        string presentationToken,
        CancellationToken cancellationToken);
}