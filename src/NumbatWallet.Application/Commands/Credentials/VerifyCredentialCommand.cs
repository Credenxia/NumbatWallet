using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Repositories;

namespace NumbatWallet.Application.Commands.Credentials;

public record VerifyCredentialCommand : ICommand<VerificationResultDto>
{
    public required string CredentialId { get; init; }
    public string? VerifierId { get; init; }
    public Dictionary<string, object>? VerificationOptions { get; init; }
}

public record VerificationResultDto
{
    public required bool IsValid { get; init; }
    public required string CredentialId { get; init; }
    public string? VerifierId { get; init; }
    public DateTime VerifiedAt { get; init; }
    public List<string> ValidationErrors { get; init; } = new();
    public Dictionary<string, object> VerificationDetails { get; init; } = new();
}

public class VerifyCredentialCommandHandler : ICommandHandler<VerifyCredentialCommand, VerificationResultDto>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly ILogger<VerifyCredentialCommandHandler> _logger;

    public VerifyCredentialCommandHandler(
        ICredentialRepository credentialRepository,
        IIssuerRepository issuerRepository,
        ILogger<VerifyCredentialCommandHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _issuerRepository = issuerRepository;
        _logger = logger;
    }

    public async Task<VerificationResultDto> HandleAsync(
        VerifyCredentialCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing VerifyCredentialCommand for credential {CredentialId}", command.CredentialId);

        var validationErrors = new List<string>();
        var verificationDetails = new Dictionary<string, object>();

        // Get credential
        var credentialId = Guid.Parse(command.CredentialId);
        var credential = await _credentialRepository.GetByIdAsync(credentialId, cancellationToken);

        if (credential == null)
        {
            throw new EntityNotFoundException("Credential", command.CredentialId);
        }

        // Check revocation status
        if (credential.IsRevoked)
        {
            validationErrors.Add("Credential has been revoked");
            verificationDetails["revocationDate"] = credential.RevocationDate?.ToString("O") ?? "";
            verificationDetails["revocationReason"] = credential.RevocationReason ?? "";
        }

        // Check expiration
        if (credential.IsExpired)
        {
            validationErrors.Add("Credential has expired");
            verificationDetails["expirationDate"] = credential.ExpirationDate?.ToString("O") ?? "";
        }

        // Verify issuer
        var issuer = await _issuerRepository.GetByIdAsync(credential.IssuerId, cancellationToken);
        if (issuer == null)
        {
            validationErrors.Add("Issuer not found");
        }
        else
        {
            verificationDetails["issuerName"] = issuer.Name;
            verificationDetails["issuerDid"] = issuer.Did;

            if (!issuer.IsActive)
            {
                validationErrors.Add("Issuer is not active");
            }
        }

        // Verify proof (simplified - in production would verify actual cryptographic signature)
        if (credential.Proof == null || !credential.Proof.ContainsKey("jws"))
        {
            validationErrors.Add("Credential proof is missing or invalid");
        }
        else
        {
            verificationDetails["proofType"] = credential.Proof.GetValueOrDefault("type", "unknown");
            verificationDetails["proofCreated"] = credential.Proof.GetValueOrDefault("created", "unknown");
        }

        // Check credential status
        verificationDetails["credentialStatus"] = credential.Status.ToString();
        verificationDetails["issuanceDate"] = credential.IssuanceDate.ToString("O");

        var isValid = validationErrors.Count == 0;

        _logger.LogInformation(
            "Credential {CredentialId} verification completed. IsValid: {IsValid}",
            credential.Id,
            isValid);

        return new VerificationResultDto
        {
            IsValid = isValid,
            CredentialId = credential.Id.ToString(),
            VerifierId = command.VerifierId,
            VerifiedAt = DateTime.UtcNow,
            ValidationErrors = validationErrors,
            VerificationDetails = verificationDetails
        };
    }
}