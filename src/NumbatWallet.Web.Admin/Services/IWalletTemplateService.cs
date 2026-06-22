using NumbatWallet.Web.Admin.Models;

namespace NumbatWallet.Web.Admin.Services;

/// <summary>
/// Wallet template service interface for Admin Portal
/// </summary>
public interface IWalletTemplateService
{
    Task<List<WalletTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<List<WalletTemplateDto>> GetTemplatesByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<WalletTemplateDto?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<WalletTemplateDto> CreateTemplateAsync(WalletTemplateDto walletTemplate, CancellationToken cancellationToken = default);
    Task<WalletTemplateDto> UpdateTemplateAsync(WalletTemplateDto walletTemplate, CancellationToken cancellationToken = default);
    Task DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
}
