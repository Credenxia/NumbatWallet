using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Credentials;

public record VerifyCredentialCommand : ICommand<VerificationResultDto>
{
    public required string CredentialId { get; init; }
    public string? VerifierId { get; init; }
    public Dictionary<string, object>? VerificationOptions { get; init; }
    public string? CredentialData { get; init; }
}

// Handler moved to separate file: Handlers/VerifyCredentialCommandHandler.cs
// Using VerificationResultDto from Application.DTOs namespace
