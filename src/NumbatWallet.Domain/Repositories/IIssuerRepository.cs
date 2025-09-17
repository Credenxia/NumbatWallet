using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Repositories;

public interface IIssuerRepository : IRepository<Issuer, Guid>
{
    Task<Issuer?> GetByDidAsync(string issuerDid, CancellationToken cancellationToken = default);
    Task<IEnumerable<Issuer>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Issuer>> GetActiveIssuersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Issuer>> GetIssuersBySupportedCredentialTypeAsync(string credentialType, CancellationToken cancellationToken = default);
    Task<bool> IssuerExistsAsync(string issuerDid, CancellationToken cancellationToken = default);
}