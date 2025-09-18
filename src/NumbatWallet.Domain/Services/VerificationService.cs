using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Domain.Services;

public interface IVerificationDomainService
{
    Task<VerificationResult> VerifyCredentialAsync(
        Credential credential,
        Guid verifierId,
        CancellationToken cancellationToken = default);

    Task<VerificationResult> VerifyPresentationAsync(
        IEnumerable<Credential> credentials,
        string[] requestedAttributes,
        Guid verifierId,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateCredentialSchemaAsync(
        Credential credential,
        CancellationToken cancellationToken = default);

    Task<bool> CheckRevocationStatusAsync(
        Credential credential,
        CancellationToken cancellationToken = default);
}

public class VerificationDomainService : IVerificationDomainService
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IIssuerRepository _issuerRepository;

    public VerificationDomainService(
        ICredentialRepository credentialRepository,
        IIssuerRepository issuerRepository)
    {
        _credentialRepository = credentialRepository;
        _issuerRepository = issuerRepository;
    }

    public async Task<VerificationResult> VerifyCredentialAsync(
        Credential credential,
        Guid verifierId,
        CancellationToken cancellationToken = default)
    {
        var result = new VerificationResult
        {
            CredentialId = credential.Id,
            VerifierId = verifierId,
            VerifiedAt = DateTimeOffset.UtcNow
        };

        // Check credential status
        if (credential.Status != CredentialStatus.Active)
        {
            result.IsValid = false;
            result.FailureReason = $"Credential status is {credential.Status}";
            return result;
        }

        // Check expiration
        if (credential.ExpiresAt.HasValue && credential.ExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            result.IsValid = false;
            result.FailureReason = "Credential has expired";
            return result;
        }

        // Verify issuer is trusted
        var issuer = await _issuerRepository.GetByIdAsync(credential.IssuerId, cancellationToken);
        if (issuer == null || !issuer.IsTrusted)
        {
            result.IsValid = false;
            result.FailureReason = "Issuer is not trusted";
            return result;
        }

        // Check revocation status
        var isRevoked = await CheckRevocationStatusAsync(credential, cancellationToken);
        if (isRevoked)
        {
            result.IsValid = false;
            result.FailureReason = "Credential has been revoked";
            return result;
        }

        // Validate schema
        var schemaValid = await ValidateCredentialSchemaAsync(credential, cancellationToken);
        if (!schemaValid)
        {
            result.IsValid = false;
            result.FailureReason = "Credential schema validation failed";
            return result;
        }

        // Add verification event
        credential.AddDomainEvent(new CredentialVerifiedEvent(
            credential.Id,
            verifierId,
            true,
            "Standard verification",
            DateTimeOffset.UtcNow));

        result.IsValid = true;
        return result;
    }

    public async Task<VerificationResult> VerifyPresentationAsync(
        IEnumerable<Credential> credentials,
        string[] requestedAttributes,
        Guid verifierId,
        CancellationToken cancellationToken = default)
    {
        var result = new VerificationResult
        {
            VerifierId = verifierId,
            VerifiedAt = DateTimeOffset.UtcNow
        };

        // Verify each credential
        foreach (var credential in credentials)
        {
            var credentialResult = await VerifyCredentialAsync(credential, verifierId, cancellationToken);
            if (!credentialResult.IsValid)
            {
                result.IsValid = false;
                result.FailureReason = $"Credential {credential.Id} verification failed: {credentialResult.FailureReason}";
                return result;
            }
        }

        // Check if all requested attributes are present
        var allAttributes = credentials
            .SelectMany(c => c.Claims.Keys)
            .Distinct()
            .ToArray();

        var missingAttributes = requestedAttributes
            .Except(allAttributes)
            .ToArray();

        if (missingAttributes.Any())
        {
            result.IsValid = false;
            result.FailureReason = $"Missing required attributes: {string.Join(", ", missingAttributes)}";
            return result;
        }

        result.IsValid = true;
        return result;
    }

    public async Task<bool> ValidateCredentialSchemaAsync(
        Credential credential,
        CancellationToken cancellationToken = default)
    {
        // Get issuer's schema for this credential type
        var issuer = await _issuerRepository.GetByIdAsync(credential.IssuerId, cancellationToken);
        if (issuer == null)
            return false;

        var schemaId = issuer.GetSchemaForCredentialType(credential.CredentialType);
        if (string.IsNullOrEmpty(schemaId))
            return false;

        // Validate credential matches schema
        // This is simplified - real implementation would validate against JSON Schema
        // In a real implementation, we would fetch the schema definition and validate
        // For now, just check that the credential has the expected schema ID
        return credential.SchemaId == schemaId;
    }

    public async Task<bool> CheckRevocationStatusAsync(
        Credential credential,
        CancellationToken cancellationToken = default)
    {
        // Check if credential is in revocation registry
        // This would integrate with actual revocation registry (blockchain, database, etc.)

        // For now, just check the credential status
        return await Task.FromResult(credential.Status == CredentialStatus.Revoked);
    }
}

public class VerificationResult
{
    public Guid? CredentialId { get; set; }
    public Guid VerifierId { get; set; }
    public bool IsValid { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}