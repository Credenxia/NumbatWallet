using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Exceptions;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Commands.Presentations.Handlers;

/// <summary>
/// EXPERIMENTAL (minimal OID4VP subset): persists a one-shot PresentationRequest and returns
/// the DIF Presentation Exchange v2 presentation_definition describing what the verifier
/// requires, plus the nonce the holder's VP-JWT must carry.
/// The definition constrains the VC-JWT payload shape produced by Phase 1:
/// <c>$.vc.type</c> must contain the requested credential type, and each requested claim must
/// be present at <c>$.vc.credentialSubject.&lt;claim&gt;</c>.
/// </summary>
public class CreatePresentationRequestCommandHandler
    : ICommandHandler<CreatePresentationRequestCommand, PresentationRequestResult>
{
    /// <summary>Default request lifetime when Presentation:RequestLifetimeMinutes is not configured.</summary>
    public const int DefaultRequestLifetimeMinutes = 30;

    private const string DefaultRequestBaseUrl = "https://wallet.numbatwallet.com/presentations/requests";

    private readonly IPresentationRequestRepository _presentationRequestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CreatePresentationRequestCommandHandler> _logger;

    public CreatePresentationRequestCommandHandler(
        IPresentationRequestRepository presentationRequestRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<CreatePresentationRequestCommandHandler> logger)
    {
        _presentationRequestRepository = presentationRequestRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PresentationRequestResult> HandleAsync(
        CreatePresentationRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.RequestedClaims is null || command.RequestedClaims.Count == 0)
        {
            throw new BusinessRuleException("A presentation request must name at least one requested claim.");
        }

        if (command.RequestedClaims.Any(string.IsNullOrWhiteSpace))
        {
            throw new BusinessRuleException("Requested claim names must not be empty.");
        }

        var nonce = GenerateNonce();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(GetRequestLifetimeMinutes());
        var definitionJson = BuildPresentationDefinition(command);

        var requestResult = PresentationRequest.Create(
            command.VerifierId,
            command.Purpose,
            command.RequestedCredentialType,
            JsonSerializer.Serialize(command.RequestedClaims),
            definitionJson,
            nonce,
            expiresAt);

        if (requestResult.IsFailure)
        {
            throw new BusinessRuleException(requestResult.Error.Message);
        }

        var request = requestResult.Value;

        await _presentationRequestRepository.AddAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created presentation request {RequestId} for verifier {VerifierId} (type {CredentialType}, expires {ExpiresAt})",
            request.Id, command.VerifierId, command.RequestedCredentialType, expiresAt);

        return new PresentationRequestResult(
            request.Id,
            definitionJson,
            BuildRequestUri(request.Id),
            nonce,
            expiresAt);
    }

    /// <summary>
    /// Builds a minimal-but-valid DIF Presentation Exchange v2 presentation_definition:
    /// one input descriptor whose field constraints pin the credential type and require each
    /// requested claim in the embedded VC's credentialSubject.
    /// </summary>
    private static string BuildPresentationDefinition(CreatePresentationRequestCommand command)
    {
        var fields = new List<Dictionary<string, object>>
        {
            new()
            {
                ["path"] = new[] { "$.vc.type" },
                ["filter"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["contains"] = new Dictionary<string, object> { ["const"] = command.RequestedCredentialType }
                }
            }
        };

        fields.AddRange(command.RequestedClaims.Select(claim => new Dictionary<string, object>
        {
            ["path"] = new[] { $"$.vc.credentialSubject.{claim}" }
        }));

        var definition = new Dictionary<string, object>
        {
            ["id"] = Guid.NewGuid().ToString(),
            ["name"] = command.Purpose,
            ["purpose"] = command.Purpose,
            ["format"] = new Dictionary<string, object>
            {
                ["jwt_vp"] = new Dictionary<string, object> { ["alg"] = new[] { "HS256", "RS256" } },
                ["jwt_vc"] = new Dictionary<string, object> { ["alg"] = new[] { "HS256", "RS256" } }
            },
            ["input_descriptors"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["id"] = command.RequestedCredentialType,
                    ["name"] = command.RequestedCredentialType,
                    ["purpose"] = command.Purpose,
                    ["constraints"] = new Dictionary<string, object> { ["fields"] = fields }
                }
            }
        };

        return JsonSerializer.Serialize(definition);
    }

    private string BuildRequestUri(Guid requestId)
    {
        var baseUrl = _configuration["Presentation:RequestBaseUrl"] ?? DefaultRequestBaseUrl;
        return $"{baseUrl.TrimEnd('/')}/{requestId}";
    }

    private int GetRequestLifetimeMinutes()
    {
        var configured = _configuration["Presentation:RequestLifetimeMinutes"];
        return int.TryParse(configured, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) && minutes > 0
            ? minutes
            : DefaultRequestLifetimeMinutes;
    }

    private static string GenerateNonce()
    {
        // 128-bit random nonce, base64url (no padding) so it is JWT/URL-safe.
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
