using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Exceptions;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Credentials.Handlers;

public class PresentCredentialCommandHandler : ICommandHandler<PresentCredentialCommand, PresentationResult>
{
    /// <summary>Default presentation token lifetime when Presentation:TokenLifetimeMinutes is not configured.</summary>
    public const int DefaultTokenLifetimeMinutes = 15;

    private readonly ICredentialRepository _credentialRepository;
    private readonly IPresentationRepository _presentationRepository;
    private readonly IVerificationService _verificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Interfaces.IEventDispatcher _eventDispatcher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PresentCredentialCommandHandler> _logger;

    public PresentCredentialCommandHandler(
        ICredentialRepository credentialRepository,
        IPresentationRepository presentationRepository,
        IVerificationService verificationService,
        IUnitOfWork unitOfWork,
        Interfaces.IEventDispatcher eventDispatcher,
        IConfiguration configuration,
        ILogger<PresentCredentialCommandHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _presentationRepository = presentationRepository;
        _verificationService = verificationService;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
        _configuration = configuration;
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

        // Create and persist the presentation record. Its Id doubles as the token's jti so
        // the verify-by-token flow can load this exact presentation later.
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(GetTokenLifetimeMinutes());
        var presentationResult = Presentation.Create(
            credential.Id,
            credential.WalletId,
            command.VerifierId,
            command.Purpose,
            JsonSerializer.Serialize(disclosedClaims),
            expiresAt);

        if (presentationResult.IsFailure)
        {
            throw new BusinessRuleException(presentationResult.Error.Message);
        }

        var presentation = presentationResult.Value;

        // Match the credential's tenant; when empty (e.g. unit tests) the TenantInterceptor
        // fills it from the ambient tenant on save.
        if (!string.IsNullOrWhiteSpace(credential.TenantId))
        {
            presentation.SetTenantId(credential.TenantId);
        }

        // Create presentation token (jti = presentation.Id)
        var presentationToken = await _verificationService.CreatePresentationTokenAsync(
            presentation.Id,
            credential.Id,
            command.VerifierId,
            command.Purpose,
            disclosedClaims,
            expiresAt,
            cancellationToken);

        // Generate verification URL
        var verificationUrl = await _verificationService.CreateVerificationUrlAsync(
            presentationToken,
            cancellationToken);

        await _presentationRepository.AddAsync(presentation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch domain event
        var credentialPresentedEvent = new CredentialPresentedEvent(
            credential.Id,
            credential.WalletId,
            command.VerifierId,
            command.Purpose,
            presentation.PresentedAt);

        await _eventDispatcher.DispatchAsync(credentialPresentedEvent, cancellationToken);

        _logger.LogInformation("Successfully presented credential {CredentialId} as presentation {PresentationId}",
            command.CredentialId, presentation.Id);

        return new PresentationResult(
            presentationToken,
            verificationUrl,
            presentation.PresentedAt.UtcDateTime,
            disclosedClaims);
    }

    private int GetTokenLifetimeMinutes()
    {
        var configured = _configuration["Presentation:TokenLifetimeMinutes"];
        return int.TryParse(configured, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) && minutes > 0
            ? minutes
            : DefaultTokenLifetimeMinutes;
    }
}
