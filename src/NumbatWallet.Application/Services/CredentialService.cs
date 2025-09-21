using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Enums;
using NumbatWallet.Domain.Interfaces;
using System.Text.Json;

namespace NumbatWallet.Application.Services;

public class CredentialService : ICredentialService
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CredentialService(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CredentialDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByIdAsync(id, cancellationToken);
        return credential != null ? MapToDto(credential) : null;
    }

    public async Task<IEnumerable<CredentialDto>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialRepository.FindAsync(c => c.WalletId == walletId, cancellationToken);
        return credentials.Select(MapToDto);
    }

    public async Task<IEnumerable<CredentialDto>> GetActiveCredentialsAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialRepository.FindAsync(
            c => c.WalletId == walletId &&
                 c.Status == CredentialStatus.Active &&
                 (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow),
            cancellationToken);
        return credentials.Select(MapToDto);
    }

    public async Task<IEnumerable<CredentialDto>> GetExpiredCredentialsAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialRepository.FindAsync(
            c => c.WalletId == walletId &&
                 (c.Status == CredentialStatus.Expired ||
                  (c.ExpiresAt != null && c.ExpiresAt <= DateTime.UtcNow)),
            cancellationToken);
        return credentials.Select(MapToDto);
    }

    public async Task<IEnumerable<CredentialDto>> GetRevokedCredentialsAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialRepository.FindAsync(
            c => c.WalletId == walletId && c.Status == CredentialStatus.Revoked,
            cancellationToken);
        return credentials.Select(MapToDto);
    }

    public async Task<CredentialDto> IssueAsync(IssueCredentialDto dto, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(dto.WalletId, cancellationToken);
        if (wallet == null)
            throw new InvalidOperationException($"Wallet with ID {dto.WalletId} not found");

        var credentialType = Enum.Parse<CredentialType>(dto.Type);
        var dataJson = JsonSerializer.Serialize(dto.Data);

        var credential = Credential.Create(
            dto.WalletId,
            credentialType,
            dataJson,
            dto.IssuerId ?? "system",
            dto.ExpiresAt
        );

        await _credentialRepository.AddAsync(credential, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return MapToDto(credential);
    }

    public async Task<bool> RevokeAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByIdAsync(id, cancellationToken);
        if (credential == null)
            return false;

        credential.Revoke(reason);
        await _credentialRepository.UpdateAsync(credential, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<VerificationResultDto> VerifyAsync(Guid id, VerificationOptionsDto options, CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByIdAsync(id, cancellationToken);
        if (credential == null)
        {
            return new VerificationResultDto
            {
                IsValid = false,
                ErrorMessage = "Credential not found"
            };
        }

        var result = new VerificationResultDto
        {
            Checks = new VerificationChecksDto()
        };

        // Check expiry
        if (options.CheckExpiry)
        {
            result.Checks.Expiry = credential.ExpiresAt == null || credential.ExpiresAt > DateTime.UtcNow;
        }

        // Check revocation
        if (options.CheckRevocation)
        {
            result.Checks.Revocation = credential.Status != CredentialStatus.Revoked;
        }

        // Mock signature and schema checks (would be implemented with actual crypto)
        if (options.CheckSignature)
        {
            result.Checks.Signature = true; // Mock: always valid
        }

        if (options.CheckSchema)
        {
            result.Checks.Schema = true; // Mock: always valid
        }

        result.Checks.Issuer = !string.IsNullOrEmpty(credential.IssuerId);

        result.IsValid = result.Checks.Expiry &&
                        result.Checks.Revocation &&
                        result.Checks.Signature &&
                        result.Checks.Schema &&
                        result.Checks.Issuer;

        return result;
    }

    public async Task<PresentationDto> CreatePresentationAsync(CreatePresentationDto dto, CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByIdAsync(dto.CredentialId, cancellationToken);
        if (credential == null)
            throw new InvalidOperationException($"Credential with ID {dto.CredentialId} not found");

        // Create presentation (simplified - would involve actual ZK proofs in production)
        var presentation = new PresentationDto
        {
            PresentationId = Guid.NewGuid(),
            CredentialId = dto.CredentialId,
            VerifierDid = dto.VerifierDid,
            DisclosedAttributes = dto.DisclosedAttributes,
            ProofToken = GenerateProofToken(credential, dto.DisclosedAttributes, dto.Challenge),
            PresentedAt = DateTime.UtcNow
        };

        // In a real implementation, we would store the presentation
        return presentation;
    }

    private static string GenerateProofToken(Credential credential, List<string> disclosedAttributes, string? challenge)
    {
        // Mock implementation - would use actual cryptographic proofs
        var token = new
        {
            credentialId = credential.Id,
            disclosed = disclosedAttributes,
            challenge = challenge ?? "default",
            timestamp = DateTime.UtcNow
        };

        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(token)));
    }

    private static CredentialDto MapToDto(Credential credential)
    {
        return new CredentialDto
        {
            Id = credential.Id,
            WalletId = credential.WalletId,
            Type = credential.Type.ToString(),
            Data = credential.Data,
            Status = credential.Status.ToString(),
            IssuerId = credential.IssuerId,
            IssuedAt = credential.IssuedAt,
            ExpiresAt = credential.ExpiresAt,
            RevokedAt = credential.RevokedAt,
            RevokedReason = credential.RevokedReason,
            CreatedAt = credential.CreatedAt,
            UpdatedAt = credential.UpdatedAt
        };
    }
}