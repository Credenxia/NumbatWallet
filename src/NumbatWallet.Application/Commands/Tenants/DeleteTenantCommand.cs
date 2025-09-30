using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Commands.Tenants;

/// <summary>
/// Command to delete a tenant
/// </summary>
public record DeleteTenantCommand : ICommand
{
    public Guid Id { get; init; }
}