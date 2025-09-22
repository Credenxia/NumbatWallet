using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for managing wallet templates and field mappings
/// </summary>
public interface IWalletTemplateService
{
    Task<List<WalletTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<List<WalletTemplate>> GetTemplatesByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<WalletTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<WalletTemplate> CreateTemplateAsync(WalletTemplate walletTemplate, CancellationToken cancellationToken = default);
    Task<WalletTemplate> UpdateTemplateAsync(WalletTemplate walletTemplate, CancellationToken cancellationToken = default);
    Task DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<WalletTemplate> CloneTemplateAsync(Guid templateId, string newName, CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> MapCredentialToTemplate(Guid templateId, Dictionary<string, object> credentialData, CancellationToken cancellationToken = default);
    Task<bool> ValidateCredentialAgainstTemplate(Guid templateId, Dictionary<string, object> credentialData, CancellationToken cancellationToken = default);
}