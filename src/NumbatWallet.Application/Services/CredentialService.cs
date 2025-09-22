using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Results;
using System.Text.Json;

namespace NumbatWallet.Application.Services;

public class CredentialService : ICredentialService
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CredentialService(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CredentialDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByIdAsync(id, cancellationToken);
        return credential != null ? MapToDto(credential) : null;
    }

    public async Task<IEnumerable<CredentialDto>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement specification pattern
        var allCredentials = await _credentialRepository.GetAllAsync(cancellationToken);
        var credentials = allCredentials.Where(c => c.WalletId == walletId);
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
        {
            throw new InvalidOperationException($"Wallet with ID {dto.WalletId} not found");
        }

        var dataJson = JsonSerializer.Serialize(dto.Data);

        var credentialResult = Credential.Create(
            dto.WalletId,
            Guid.Parse(dto.IssuerId ?? Guid.NewGuid().ToString()),
            dto.Type, // credentialType as string
            dataJson,
            "default-schema" // TODO: Add schema to DTO
        );

        if (!credentialResult.IsSuccess)
        {
            throw new InvalidOperationException(credentialResult.Error.Message);
        }

        var credential = credentialResult.Value;

        await _credentialRepository.AddAsync(credential, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(credential);
    }

    public async Task<bool> RevokeAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByIdAsync(id, cancellationToken);
        if (credential == null)
        {
            return false;
        }

        credential.Revoke(reason);
        await _credentialRepository.UpdateAsync(credential, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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

        result.Checks.Issuer = credential.IssuerId != Guid.Empty;

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
        {
            throw new InvalidOperationException($"Credential with ID {dto.CredentialId} not found");
        }

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
        var credentialData = JsonDocument.Parse(credential.CredentialData);
        var credentialSubject = new Dictionary<string, object>();

        // Extract credential subject from JSON data
        if (credentialData.RootElement.TryGetProperty("credentialSubject", out var subjectElement))
        {
            foreach (var prop in subjectElement.EnumerateObject())
            {
                credentialSubject[prop.Name] = prop.Value.ToString();
            }
        }
        else
        {
            // Use entire data as credential subject if not structured
            foreach (var prop in credentialData.RootElement.EnumerateObject())
            {
                credentialSubject[prop.Name] = prop.Value.ToString();
            }
        }

        return new CredentialDto
        {
            Id = credential.Id.ToString(),
            HolderId = credential.WalletId.ToString(), // Using WalletId as HolderId
            IssuerId = credential.IssuerId.ToString(),
            Type = credential.CredentialType,
            CredentialSubject = credentialSubject,
            Status = credential.Status.ToString(),
            IssuanceDate = credential.IssuedAt.DateTime,
            ExpirationDate = credential.ExpiresAt?.DateTime,
            IsRevoked = credential.Status == CredentialStatus.Revoked,
            RevocationDate = credential.RevokedAt?.DateTime,
            RevocationReason = credential.RevocationReason
        };
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByIdAsync(id, cancellationToken);
        if (credential == null)
        {
            return false;
        }

        // Parse the status string to CredentialStatus enum
        if (!Enum.TryParse<CredentialStatus>(status, true, out var credentialStatus))
        {
            return false;
        }

        // Update the status based on the parsed value
        Result result;
        switch (credentialStatus)
        {
            case CredentialStatus.Revoked:
                result = credential.Revoke("Status updated via API");
                break;
            case CredentialStatus.Suspended:
                result = credential.Suspend("Status updated via API");
                break;
            case CredentialStatus.Active:
                result = credential.Activate();
                break;
            default:
                return false;
        }

        if (!result.IsSuccess)
        {
            return false;
        }

        await _credentialRepository.UpdateAsync(credential, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UserHasAccessAsync(string userId, Guid credentialId, CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByIdAsync(credentialId, cancellationToken);
        if (credential == null)
        {
            return false;
        }

        // Get the wallet to check ownership
        var wallet = await _walletRepository.GetByIdAsync(credential.WalletId, cancellationToken);
        if (wallet == null)
        {
            return false;
        }

        // Check if the user owns this wallet through person
        var person = await _personRepository.GetByIdAsync(wallet.PersonId, cancellationToken);
        if (person == null)
        {
            return false;
        }

        // Check if the user's external ID matches
        return person.ExternalId == userId;
    }

    public async Task<VerificationResultDto> VerifyCredentialAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var credential = await _credentialRepository.GetByIdAsync(id, cancellationToken);
        if (credential == null)
        {
            return new VerificationResultDto
            {
                IsValid = false,
                ErrorMessage = "Credential not found",
                Checks = new VerificationChecksDto()
            };
        }

        // Basic verification - check status and expiry
        var errors = new List<string>();
        var isValid = true;
        var checks = new VerificationChecksDto
        {
            Signature = true, // Assume valid for now
            Schema = true,    // Assume valid for now
            Issuer = true     // Assume valid for now
        };

        if (credential.Status != CredentialStatus.Active)
        {
            isValid = false;
            errors.Add($"Credential is {credential.Status}");
            checks.Revocation = false;
        }
        else
        {
            checks.Revocation = true;
        }

        if (credential.IsExpired())
        {
            isValid = false;
            errors.Add("Credential is expired");
            checks.Expiry = false;
        }
        else
        {
            checks.Expiry = true;
        }

        return new VerificationResultDto
        {
            IsValid = isValid,
            VerifiedAt = DateTime.UtcNow,
            ErrorMessage = errors.Any() ? string.Join("; ", errors) : null,
            Checks = checks
        };
    }

    public async Task<CredentialDto> IssueCredentialAsync(IssueCredentialDto dto, CancellationToken cancellationToken = default)
    {
        // This is just a wrapper for the existing IssueAsync method
        return await IssueAsync(dto, cancellationToken);
    }

    public async Task<bool> RevokeCredentialAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        // This is just a wrapper for the existing RevokeAsync method
        return await RevokeAsync(id, reason, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAvailableCredentialTypesAsync(CancellationToken cancellationToken = default)
    {
        // Return a list of available credential types
        return await Task.FromResult(new[]
        {
            "DriverLicense",
            "WorkingWithChildren",
            "ProofOfAge",
            "StudentID",
            "HealthCard",
            "ProfessionalLicense",
            "VaccinationCertificate",
            "EducationCredential"
        });
    }

    public async Task<bool> ValidateJwtVcAsync(string jwt, CancellationToken cancellationToken = default)
    {
        // TODO: Implement JWT VC validation
        // For now, return true if the JWT is not empty
        return await Task.FromResult(!string.IsNullOrWhiteSpace(jwt));
    }
}
