namespace NumbatWallet.Application.Commands.Tenants;

/// <summary>
/// Command to delete a tenant
/// </summary>
public record DeleteTenantCommand
{
    public Guid Id { get; init; }
}