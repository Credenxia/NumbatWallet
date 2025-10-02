using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Application.Services;

/// <summary>
/// Service implementation for managing wallet templates
/// </summary>
public class WalletTemplateService : IWalletTemplateService
{
    private readonly IWalletTemplateRepository _repository;
    private readonly ILogger<WalletTemplateService> _logger;

    public WalletTemplateService(
        IWalletTemplateRepository repository,
        ILogger<WalletTemplateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<WalletTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _repository.GetAllAsync(cancellationToken);
        return templates.ToList();
    }

    public async Task<List<WalletTemplate>> GetTemplatesByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var templates = await _repository.GetByTenantIdAsync(tenantId, cancellationToken);
        return templates.ToList();
    }

    public async Task<WalletTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(templateId, cancellationToken);
    }

    public async Task<WalletTemplate> CreateTemplateAsync(WalletTemplate walletTemplate, CancellationToken cancellationToken = default)
    {
        await _repository.AddAsync(walletTemplate, cancellationToken);
        _logger.LogInformation("Created wallet template {TemplateId} with name {TemplateName}",
            walletTemplate.Id, walletTemplate.Name);
        return walletTemplate;
    }

    public async Task<WalletTemplate> UpdateTemplateAsync(WalletTemplate walletTemplate, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(walletTemplate, cancellationToken);
        _logger.LogInformation("Updated wallet template {TemplateId}", walletTemplate.Id);
        return walletTemplate;
    }

    public async Task DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _repository.GetByIdAsync(templateId, cancellationToken);
        if (template != null)
        {
            await _repository.DeleteAsync(template, cancellationToken);
            _logger.LogInformation("Deleted wallet template {TemplateId}", templateId);
        }
    }

    public async Task<WalletTemplate> CloneTemplateAsync(Guid templateId, string newName, CancellationToken cancellationToken = default)
    {
        var original = await _repository.GetByIdAsync(templateId, cancellationToken);
        if (original == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        var cloned = new WalletTemplate(
            original.TenantId,
            newName,
            original.Description,
            original.Type,
            "System");

        foreach (var field in original.Fields)
        {
            var clonedField = new WalletField(
                field.Name,
                field.Label,
                field.FieldType,
                field.IsRequired,
                field.DisplayOrder);

            if (!string.IsNullOrEmpty(field.MappedCredentialField))
                clonedField.SetMappedCredentialField(field.MappedCredentialField);

            if (!string.IsNullOrEmpty(field.ValidationRule))
                clonedField.SetValidationRule(field.ValidationRule);

            if (!string.IsNullOrEmpty(field.DefaultValue))
                clonedField.SetDefaultValue(field.DefaultValue);

            clonedField.UpdateEditability(field.IsEditable);

            // Copy properties
            foreach (var prop in field.Properties)
            {
                clonedField.AddProperty(prop.Key, prop.Value);
            }

            cloned.AddField(clonedField);
        }

        foreach (var credType in original.SupportedCredentialTypes)
        {
            cloned.AddSupportedCredentialType(credType);
        }

        // Copy metadata
        foreach (var metadata in original.Metadata)
        {
            cloned.UpdateMetadata(metadata.Key, metadata.Value);
        }

        await _repository.AddAsync(cloned, cancellationToken);
        _logger.LogInformation("Cloned template {OriginalId} to {NewId} with name {NewName}",
            templateId, cloned.Id, newName);

        return cloned;
    }

    public async Task<Dictionary<string, object>> MapCredentialToTemplate(
        Guid templateId,
        Dictionary<string, object> credentialData,
        CancellationToken cancellationToken = default)
    {
        var template = await _repository.GetByIdAsync(templateId, cancellationToken);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        var mappedData = new Dictionary<string, object>();

        foreach (var field in template.Fields)
        {
            if (!string.IsNullOrEmpty(field.MappedCredentialField) &&
                credentialData.TryGetValue(field.MappedCredentialField, out var value))
            {
                mappedData[field.Name] = value;
            }
            else if (!string.IsNullOrEmpty(field.DefaultValue))
            {
                mappedData[field.Name] = field.DefaultValue;
            }
        }

        return mappedData;
    }

    public async Task<bool> ValidateCredentialAgainstTemplate(
        Guid templateId,
        Dictionary<string, object> credentialData,
        CancellationToken cancellationToken = default)
    {
        var template = await _repository.GetByIdAsync(templateId, cancellationToken);
        if (template == null)
        {
            return false;
        }

        // Check that all required fields have data
        foreach (var field in template.Fields.Where(f => f.IsRequired))
        {
            if (!string.IsNullOrEmpty(field.MappedCredentialField))
            {
                if (!credentialData.ContainsKey(field.MappedCredentialField))
                {
                    _logger.LogWarning("Required field {FieldName} with mapping {Mapping} not found in credential data",
                        field.Name, field.MappedCredentialField);
                    return false;
                }
            }
        }

        return true;
    }
}
