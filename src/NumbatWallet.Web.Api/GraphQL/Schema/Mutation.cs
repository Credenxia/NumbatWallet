using HotChocolate;
using HotChocolate.Authorization;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.Commands.Organizations;
using NumbatWallet.Application.Commands.Persons;
using NumbatWallet.Application.Commands.Wallets;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Enums;

namespace NumbatWallet.Web.Api.GraphQL.Schema;

public class Mutation
{
    // Person Mutations
    [Authorize(Roles = new[] { "Admin", "Officer" })]
    public async Task<PersonDto> CreatePerson(
        CreatePersonInput input,
        [Service] ICommandHandler<CreatePersonCommand, PersonDto> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreatePersonCommand(
            input.FirstName,
            input.LastName,
            input.Email,
            input.PhoneNumber,
            input.DateOfBirth);

        return await handler.HandleAsync(command, cancellationToken);
    }

    [Authorize]
    public async Task<PersonDto> UpdatePerson(
        UpdatePersonInput input,
        [Service] ICommandHandler<UpdatePersonCommand, PersonDto> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePersonCommand(
            input.Id,
            input.FirstName,
            input.LastName,
            input.Email,
            input.PhoneNumber);

        return await handler.HandleAsync(command, cancellationToken);
    }

    [Authorize(Roles = new[] { "Admin" })]
    public async Task<bool> DeletePerson(
        Guid id,
        [Service] ICommandHandler<DeletePersonCommand, bool> handler,
        CancellationToken cancellationToken)
    {
        var command = new DeletePersonCommand(id);
        return await handler.HandleAsync(command, cancellationToken);
    }

    // Organization Mutations
    [Authorize(Roles = new[] { "Admin" })]
    public async Task<OrganizationDto> CreateOrganization(
        CreateOrganizationInput input,
        [Service] ICommandHandler<CreateOrganizationCommand, OrganizationDto> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrganizationCommand(
            input.Name,
            input.Identifier,
            input.Type,
            input.ContactEmail,
            input.ContactPhone,
            input.Address,
            input.Website);

        return await handler.HandleAsync(command, cancellationToken);
    }

    [Authorize(Roles = new[] { "Admin", "Officer" })]
    public async Task<OrganizationDto> UpdateOrganization(
        UpdateOrganizationInput input,
        [Service] ICommandHandler<UpdateOrganizationCommand, OrganizationDto> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateOrganizationCommand(
            input.Id,
            input.Name,
            input.ContactEmail,
            input.ContactPhone,
            input.Address,
            input.Website,
            input.IsActive);

        return await handler.HandleAsync(command, cancellationToken);
    }

    // Wallet Mutations
    [Authorize]
    public async Task<WalletDto> CreateWallet(
        CreateWalletInput input,
        [Service] ICommandHandler<CreateWalletCommand, WalletDto> handler,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var command = new CreateWalletCommand(
            input.PersonId,
            input.Name ?? "My Wallet",
            userId);

        return await handler.HandleAsync(command, cancellationToken);
    }

    [Authorize]
    public async Task<WalletDto> UpdateWalletSettings(
        UpdateWalletSettingsInput input,
        [Service] ICommandHandler<UpdateWalletSettingsCommand, WalletDto> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWalletSettingsCommand(
            input.WalletId,
            input.Name,
            input.EnableBiometric,
            input.RequirePinForAccess,
            input.AutoLockTimeoutMinutes);

        return await handler.HandleAsync(command, cancellationToken);
    }

    [Authorize]
    public async Task<bool> BackupWallet(
        Guid walletId,
        [Service] ICommandHandler<BackupWalletCommand, bool> handler,
        CancellationToken cancellationToken)
    {
        var command = new BackupWalletCommand(walletId);
        return await handler.HandleAsync(command, cancellationToken);
    }

    [Authorize]
    public async Task<WalletDto> RestoreWallet(
        RestoreWalletInput input,
        [Service] ICommandHandler<RestoreWalletCommand, WalletDto> handler,
        CancellationToken cancellationToken)
    {
        var command = new RestoreWalletCommand(
            input.BackupData,
            input.RecoveryPhrase,
            input.Pin);

        return await handler.HandleAsync(command, cancellationToken);
    }

    // Credential Mutations
    [Authorize(Roles = new[] { "Admin", "Officer", "Issuer" })]
    public async Task<CredentialDto> IssueCredential(
        IssueCredentialInput input,
        [Service] ICommandHandler<IssueCredentialCommand, CredentialDto> handler,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var issuerId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("Issuer not authenticated");

        var command = new IssueCredentialCommand(
            input.WalletId,
            input.CredentialType,
            input.Subject,
            input.Claims,
            input.ValidFrom ?? DateTime.UtcNow,
            input.ValidUntil,
            issuerId,
            input.IssuerOrganizationId);

        return await handler.HandleAsync(command, cancellationToken);
    }

    [Authorize(Roles = new[] { "Admin", "Officer", "Issuer" })]
    public async Task<bool> RevokeCredential(
        RevokeCredentialInput input,
        [Service] ICommandHandler<RevokeCredentialCommand, bool> handler,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var revokerId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var command = new RevokeCredentialCommand(
            input.CredentialId,
            input.Reason,
            revokerId);

        return await handler.HandleAsync(command, cancellationToken);
    }

    [Authorize]
    public async Task<VerificationResult> VerifyCredential(
        Guid credentialId,
        [Service] ICredentialService credentialService,
        CancellationToken cancellationToken)
    {
        return await credentialService.VerifyCredentialAsync(credentialId, cancellationToken);
    }

    [Authorize]
    public async Task<CredentialDto> ShareCredential(
        ShareCredentialInput input,
        [Service] ICommandHandler<ShareCredentialCommand, CredentialDto> handler,
        CancellationToken cancellationToken)
    {
        var command = new ShareCredentialCommand(
            input.CredentialId,
            input.RecipientEmail,
            input.ExpiresInMinutes ?? 60,
            input.RequirePin,
            input.Pin);

        return await handler.HandleAsync(command, cancellationToken);
    }

    // Bulk Operations
    [Authorize(Roles = new[] { "Admin", "Officer" })]
    public async Task<BulkIssueResult> BulkIssueCredentials(
        BulkIssueCredentialsInput input,
        [Service] ICommandHandler<BulkIssueCredentialsCommand, BulkIssueResult> handler,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        var issuerId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("Issuer not authenticated");

        var command = new BulkIssueCredentialsCommand(
            input.WalletIds,
            input.CredentialType,
            input.Template,
            issuerId,
            input.IssuerOrganizationId,
            input.ValidFrom ?? DateTime.UtcNow,
            input.ValidUntil);

        return await handler.HandleAsync(command, cancellationToken);
    }
}

// Input Types
public class CreatePersonInput
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

public class UpdatePersonInput
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class CreateOrganizationInput
{
    public required string Name { get; set; }
    public required string Identifier { get; set; }
    public required OrganizationType Type { get; set; }
    public required string ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
}

public class UpdateOrganizationInput
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateWalletInput
{
    public Guid PersonId { get; set; }
    public string? Name { get; set; }
}

public class UpdateWalletSettingsInput
{
    public Guid WalletId { get; set; }
    public string? Name { get; set; }
    public bool? EnableBiometric { get; set; }
    public bool? RequirePinForAccess { get; set; }
    public int? AutoLockTimeoutMinutes { get; set; }
}

public class RestoreWalletInput
{
    public required string BackupData { get; set; }
    public required string RecoveryPhrase { get; set; }
    public string? Pin { get; set; }
}

public class IssueCredentialInput
{
    public Guid WalletId { get; set; }
    public required CredentialType CredentialType { get; set; }
    public required string Subject { get; set; }
    public required Dictionary<string, object> Claims { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public Guid IssuerOrganizationId { get; set; }
}

public class RevokeCredentialInput
{
    public Guid CredentialId { get; set; }
    public required string Reason { get; set; }
}

public class ShareCredentialInput
{
    public Guid CredentialId { get; set; }
    public required string RecipientEmail { get; set; }
    public int? ExpiresInMinutes { get; set; }
    public bool RequirePin { get; set; }
    public string? Pin { get; set; }
}

public class BulkIssueCredentialsInput
{
    public required List<Guid> WalletIds { get; set; }
    public required CredentialType CredentialType { get; set; }
    public required Dictionary<string, object> Template { get; set; }
    public Guid IssuerOrganizationId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}

// Result Types
public class VerificationResult
{
    public bool IsValid { get; set; }
    public string? Issuer { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> Claims { get; set; } = new();
}

public class BulkIssueResult
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<Guid> IssuedCredentialIds { get; set; } = new();
    public List<BulkIssueError> Errors { get; set; } = new();
}

public class BulkIssueError
{
    public Guid WalletId { get; set; }
    public string Error { get; set; } = string.Empty;
}