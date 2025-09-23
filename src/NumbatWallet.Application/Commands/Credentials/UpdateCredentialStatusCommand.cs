using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Credentials;

public record UpdateCredentialStatusCommand(
    Guid TenantId,
    Guid CredentialId,
    string Status,
    string? Reason) : ICommand<CredentialDto>;