using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Web.Api.Extensions;
using System.ComponentModel.DataAnnotations;

namespace NumbatWallet.Web.Api.Controllers;

/// <summary>
/// API endpoints for managing wallet templates
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/wallet-templates")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")]
[Tags("Wallet Templates")]
public class WalletTemplateController : ControllerBase
{
    private readonly IWalletTemplateService _walletTemplateService;
    private readonly ILogger<WalletTemplateController> _logger;

    public WalletTemplateController(
        IWalletTemplateService walletTemplateService,
        ILogger<WalletTemplateController> logger)
    {
        _walletTemplateService = walletTemplateService;
        _logger = logger;
    }

    /// <summary>
    /// Get all wallet templates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WalletTemplate>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTemplates(CancellationToken cancellationToken)
    {
        var templates = await _walletTemplateService.GetTemplatesAsync(cancellationToken);
        return Ok(templates);
    }

    /// <summary>
    /// Get wallet templates for a specific tenant
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    [ProducesResponseType(typeof(List<WalletTemplate>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTemplatesByTenant(
        [Required] Guid tenantId,
        CancellationToken cancellationToken)
    {
        var templates = await _walletTemplateService.GetTemplatesByTenantAsync(tenantId, cancellationToken);
        return Ok(templates);
    }

    /// <summary>
    /// Get a specific wallet template by ID
    /// </summary>
    [HttpGet("{templateId}")]
    [ProducesResponseType(typeof(WalletTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTemplateById(
        [Required] Guid templateId,
        CancellationToken cancellationToken)
    {
        var template = await _walletTemplateService.GetTemplateByIdAsync(templateId, cancellationToken);
        if (template == null)
        {
            return NotFound(new { message = "Template not found" });
        }
        return Ok(template);
    }

    /// <summary>
    /// Create a new wallet template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WalletTemplate), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody][Required] CreateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = HttpContext.GetTenantId();
            var userId = HttpContext.GetUserId();

            var template = new WalletTemplate(
                tenantId,
                request.Name,
                request.Description,
                request.Type,
                userId);

            foreach (var credType in request.SupportedCredentialTypes)
            {
                template.AddSupportedCredentialType(credType);
            }

            foreach (var field in request.Fields)
            {
                var walletField = new WalletField(
                    field.Name,
                    field.Label,
                    field.FieldType,
                    field.IsRequired,
                    field.DisplayOrder);

                if (!string.IsNullOrEmpty(field.MappedCredentialField))
                {
                    walletField.SetMappedCredentialField(field.MappedCredentialField);
                }

                if (!string.IsNullOrEmpty(field.ValidationRule))
                {
                    walletField.SetValidationRule(field.ValidationRule);
                }

                if (!string.IsNullOrEmpty(field.DefaultValue))
                {
                    walletField.SetDefaultValue(field.DefaultValue);
                }

                walletField.UpdateEditability(field.IsEditable);
                template.AddField(walletField);
            }

            var created = await _walletTemplateService.CreateTemplateAsync(template, cancellationToken);

            return CreatedAtAction(
                nameof(GetTemplateById),
                new { templateId = created.Id, version = "1.0" },
                created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating wallet template");
            return BadRequest(new { message = "Error creating template", error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing wallet template
    /// </summary>
    [HttpPut("{templateId}")]
    [ProducesResponseType(typeof(WalletTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateTemplate(
        [Required] Guid templateId,
        [FromBody][Required] UpdateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var template = await _walletTemplateService.GetTemplateByIdAsync(templateId, cancellationToken);
            if (template == null)
            {
                return NotFound(new { message = "Template not found" });
            }

            // WalletTemplate is immutable for core properties, need to recreate
            // Since we're updating fields, we'll just update them directly

            // Update fields if provided
            if (request.Fields != null)
            {
                // Clear existing fields and add new ones
                foreach (var field in template.Fields.ToList())
                {
                    template.RemoveField(field.Name);
                }

                foreach (var field in request.Fields)
                {
                    var walletField = new WalletField(
                        field.Name,
                        field.Label,
                        field.FieldType,
                        field.IsRequired,
                        field.DisplayOrder);

                    if (!string.IsNullOrEmpty(field.MappedCredentialField))
                    {
                        walletField.SetMappedCredentialField(field.MappedCredentialField);
                    }

                    if (!string.IsNullOrEmpty(field.ValidationRule))
                    {
                        walletField.SetValidationRule(field.ValidationRule);
                    }

                    if (!string.IsNullOrEmpty(field.DefaultValue))
                    {
                        walletField.SetDefaultValue(field.DefaultValue);
                    }

                    walletField.UpdateEditability(field.IsEditable);
                    template.AddField(walletField);
                }
            }

            // Update supported credential types if provided
            if (request.SupportedCredentialTypes != null)
            {
                foreach (var type in template.SupportedCredentialTypes.ToList())
                {
                    template.RemoveSupportedCredentialType(type);
                }

                foreach (var type in request.SupportedCredentialTypes)
                {
                    template.AddSupportedCredentialType(type);
                }
            }

            var updated = await _walletTemplateService.UpdateTemplateAsync(template, cancellationToken);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating wallet template {TemplateId}", templateId);
            return BadRequest(new { message = "Error updating template", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a wallet template
    /// </summary>
    [HttpDelete("{templateId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteTemplate(
        [Required] Guid templateId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _walletTemplateService.DeleteTemplateAsync(templateId, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting wallet template {TemplateId}", templateId);
            return NotFound(new { message = "Template not found or could not be deleted" });
        }
    }

    /// <summary>
    /// Clone an existing wallet template
    /// </summary>
    [HttpPost("{templateId}/clone")]
    [ProducesResponseType(typeof(WalletTemplate), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CloneTemplate(
        [Required] Guid templateId,
        [FromBody][Required] CloneTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cloned = await _walletTemplateService.CloneTemplateAsync(templateId, request.NewName, cancellationToken);

            return CreatedAtAction(
                nameof(GetTemplateById),
                new { templateId = cloned.Id, version = "1.0" },
                cloned);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning wallet template {TemplateId}", templateId);
            return BadRequest(new { message = "Error cloning template", error = ex.Message });
        }
    }

    /// <summary>
    /// Map credential data to template fields
    /// </summary>
    [HttpPost("{templateId}/map")]
    [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MapCredentialToTemplate(
        [Required] Guid templateId,
        [FromBody][Required] MapCredentialRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var mapped = await _walletTemplateService.MapCredentialToTemplate(
                templateId,
                request.CredentialData,
                cancellationToken);

            return Ok(mapped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping credential to template {TemplateId}", templateId);
            return BadRequest(new { message = "Error mapping credential", error = ex.Message });
        }
    }

    /// <summary>
    /// Validate credential data against template requirements
    /// </summary>
    [HttpPost("{templateId}/validate")]
    [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateCredentialAgainstTemplate(
        [Required] Guid templateId,
        [FromBody][Required] ValidateCredentialRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var isValid = await _walletTemplateService.ValidateCredentialAgainstTemplate(
                templateId,
                request.CredentialData,
                cancellationToken);

            return Ok(new ValidationResponse(isValid, isValid ? Array.Empty<string>() : new[] { "Validation failed" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credential against template {TemplateId}", templateId);
            return BadRequest(new { message = "Error validating credential", error = ex.Message });
        }
    }
}

// Request/Response DTOs
public record CreateTemplateRequest(
    [Required] string Name,
    string Description,
    [Required] WalletTemplateType Type,
    List<string> SupportedCredentialTypes,
    List<FieldRequest> Fields);

public record UpdateTemplateRequest(
    [Required] string Name,
    string Description,
    List<string>? SupportedCredentialTypes,
    List<FieldRequest>? Fields);

public record FieldRequest(
    [Required] string Name,
    [Required] string Label,
    [Required] string FieldType,
    bool IsRequired,
    bool IsEditable,
    string? MappedCredentialField,
    string? ValidationRule,
    string? DefaultValue,
    int DisplayOrder);

public record CloneTemplateRequest([Required] string NewName);

public record MapCredentialRequest([Required] Dictionary<string, object> CredentialData);

public record ValidateCredentialRequest([Required] Dictionary<string, object> CredentialData);

public record ValidationResponse(bool IsValid, string[] Errors);