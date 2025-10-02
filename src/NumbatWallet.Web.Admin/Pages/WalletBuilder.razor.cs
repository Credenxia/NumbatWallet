using Microsoft.AspNetCore.Components;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Application.Interfaces;
using System.Text.Json;

namespace NumbatWallet.Web.Admin.Pages;

public partial class WalletBuilder
{
    [Inject] private IWalletTemplateService WalletTemplateService { get; set; } = default!;
    [Inject] private IAppleWalletBuilder AppleWalletBuilder { get; set; } = default!;
    [Inject] private IGoogleWalletBuilder GoogleWalletBuilder { get; set; } = default!;
    [Inject] private IWebWalletBuilder WebWalletBuilder { get; set; } = default!;
    [Inject] private ILogger<WalletBuilder> Logger { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;

    private List<WalletTemplate> templates = new();
    private WalletTemplate? selectedTemplate;
    private bool showCreateForm = false;
    private bool showPreview = false;
    private bool isEditingField = false;
    private string previewPlatform = "generic"; // generic, apple, google, web
    private string activePlatformTab = "ios"; // ios, android, web

    private CreateTemplateModel newTemplate = new();
    private FieldModel currentField = new();
    private string newCredentialType = string.Empty;

    // Platform-specific configurations
    private PlatformConfigModel iosConfig = new();
    private PlatformConfigModel androidConfig = new();
    private PlatformConfigModel webConfig = new();

    // Cache JsonSerializerOptions to avoid CA1869
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadTemplates();
    }

    private async Task LoadTemplates()
    {
        try
        {
            templates = await WalletTemplateService.GetTemplatesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading wallet templates");
        }
    }

    private void ShowCreateTemplate()
    {
        newTemplate = new CreateTemplateModel();
        showCreateForm = true;
        selectedTemplate = null;
    }

    private void CancelCreate()
    {
        showCreateForm = false;
        newTemplate = new CreateTemplateModel();
    }

    private async Task CreateTemplate()
    {
        try
        {
            var template = new WalletTemplate(
                Guid.NewGuid(), // TenantId should come from current context
                newTemplate.Name,
                newTemplate.Description,
                newTemplate.Type,
                "Admin User"); // Should come from current user

            foreach (var credType in newTemplate.SupportedCredentialTypes)
            {
                template.AddSupportedCredentialType(credType);
            }

            await WalletTemplateService.CreateTemplateAsync(template);
            await LoadTemplates();
            showCreateForm = false;
            SelectTemplate(template);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating wallet template");
        }
    }

    private void SelectTemplate(WalletTemplate template)
    {
        selectedTemplate = template;
        showCreateForm = false;
        ResetFieldForm();
        LoadPlatformConfigs();
    }

    private void LoadPlatformConfigs()
    {
        if (selectedTemplate == null)
        {
            return;
        }

        // Load platform configs from template metadata
        if (selectedTemplate.Metadata.TryGetValue("ios_config", out var iosData))
        {
            iosConfig = JsonSerializer.Deserialize<PlatformConfigModel>(iosData.ToString() ?? "{}") ?? new PlatformConfigModel();
        }
        else
        {
            iosConfig = new PlatformConfigModel();
        }

        if (selectedTemplate.Metadata.TryGetValue("android_config", out var androidData))
        {
            androidConfig = JsonSerializer.Deserialize<PlatformConfigModel>(androidData.ToString() ?? "{}") ?? new PlatformConfigModel();
        }
        else
        {
            androidConfig = new PlatformConfigModel();
        }

        if (selectedTemplate.Metadata.TryGetValue("web_config", out var webData))
        {
            webConfig = JsonSerializer.Deserialize<PlatformConfigModel>(webData.ToString() ?? "{}") ?? new PlatformConfigModel();
        }
        else
        {
            webConfig = new PlatformConfigModel();
        }
    }

    private void SavePlatformConfigs()
    {
        if (selectedTemplate == null)
        {
            return;
        }

        // Save platform configs to template metadata
        selectedTemplate.UpdateMetadata("ios_config", JsonSerializer.Serialize(iosConfig, s_jsonOptions));
        selectedTemplate.UpdateMetadata("android_config", JsonSerializer.Serialize(androidConfig, s_jsonOptions));
        selectedTemplate.UpdateMetadata("web_config", JsonSerializer.Serialize(webConfig, s_jsonOptions));
    }

    private void EditField(WalletField field)
    {
        currentField = new FieldModel
        {
            Name = field.Name,
            Label = field.Label,
            FieldType = field.FieldType,
            IsRequired = field.IsRequired,
            IsEditable = field.IsEditable,
            ValidationRule = field.ValidationRule ?? string.Empty,
            DefaultValue = field.DefaultValue ?? string.Empty,
            MappedCredentialField = field.MappedCredentialField ?? string.Empty,
            DisplayOrder = field.DisplayOrder
        };
        isEditingField = true;
    }

    private async Task SaveField()
    {
        if (selectedTemplate == null)
        {
            return;
        }

        try
        {
            if (isEditingField)
            {
                selectedTemplate.UpdateField(currentField.Name, field =>
                {
                    field.SetMappedCredentialField(currentField.MappedCredentialField);
                    field.SetValidationRule(currentField.ValidationRule);
                    field.SetDefaultValue(currentField.DefaultValue);
                    field.UpdateEditability(currentField.IsEditable);
                });
            }
            else
            {
                var newField = new WalletField(
                    currentField.Name,
                    currentField.Label,
                    currentField.FieldType,
                    currentField.IsRequired,
                    selectedTemplate.Fields.Count);

                newField.SetMappedCredentialField(currentField.MappedCredentialField);
                newField.SetValidationRule(currentField.ValidationRule);
                newField.SetDefaultValue(currentField.DefaultValue);
                newField.UpdateEditability(currentField.IsEditable);

                selectedTemplate.AddField(newField);
            }

            ResetFieldForm();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving field");
        }
    }

    private void CancelFieldEdit()
    {
        ResetFieldForm();
    }

    private void ResetFieldForm()
    {
        currentField = new FieldModel();
        isEditingField = false;
    }

    private void RemoveField(WalletField field)
    {
        selectedTemplate?.RemoveField(field.Name);
    }

    private void MoveFieldUp(WalletField field)
    {
        if (selectedTemplate == null)
        {
            return;
        }

        var fields = selectedTemplate.Fields.OrderBy(f => f.DisplayOrder).ToList();
        var index = fields.IndexOf(field);
        if (index > 0)
        {
            field.UpdateDisplayOrder(field.DisplayOrder - 1);
            fields[index - 1].UpdateDisplayOrder(fields[index - 1].DisplayOrder + 1);
        }
    }

    private void MoveFieldDown(WalletField field)
    {
        if (selectedTemplate == null)
        {
            return;
        }

        var fields = selectedTemplate.Fields.OrderBy(f => f.DisplayOrder).ToList();
        var index = fields.IndexOf(field);
        if (index < fields.Count - 1)
        {
            field.UpdateDisplayOrder(field.DisplayOrder + 1);
            fields[index + 1].UpdateDisplayOrder(fields[index + 1].DisplayOrder - 1);
        }
    }

    private void AddCredentialType()
    {
        if (!string.IsNullOrWhiteSpace(newCredentialType))
        {
            newTemplate.SupportedCredentialTypes.Add(newCredentialType);
            newCredentialType = string.Empty;
        }
    }

    private void RemoveCredentialType(string type)
    {
        newTemplate.SupportedCredentialTypes.Remove(type);
    }

    private async Task SaveTemplate()
    {
        if (selectedTemplate == null)
        {
            return;
        }

        try
        {
            SavePlatformConfigs(); // Save platform configs before updating
            await WalletTemplateService.UpdateTemplateAsync(selectedTemplate);
            await LoadTemplates();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving template");
        }
    }

    private void PreviewTemplate()
    {
        previewPlatform = "generic";
        showPreview = true;
    }

    private void SetPreviewPlatform(string platform)
    {
        previewPlatform = platform;
    }

    private async Task ExportTemplate()
    {
        if (selectedTemplate == null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(selectedTemplate, s_jsonOptions);

        // In a real implementation, this would trigger a download
        Logger.LogInformation("Exported template: {Json}", json);
    }

    private async Task ExportAppleWallet()
    {
        if (selectedTemplate == null)
        {
            return;
        }

        try
        {
            var options = new AppleWalletOptions
            {
                TeamIdentifier = Configuration["WalletProviders:Apple:TeamIdentifier"] ?? "TEAM_ID_NOT_CONFIGURED",
                PassTypeIdentifier = Configuration["WalletProviders:Apple:PassTypeIdentifier"] ?? "pass.com.numbat.wallet",
                OrganizationName = Configuration["WalletProviders:Apple:OrganizationName"] ?? "NumbatWallet",
                Description = selectedTemplate.Description,
                LogoText = selectedTemplate.Name
            };

            // Create sample data from template fields
            var data = selectedTemplate.Fields.ToDictionary(
                f => f.Name,
                f => (object)(f.DefaultValue ?? string.Empty)
            );

            var pkpassBytes = await AppleWalletBuilder.GeneratePkpassAsync(
                selectedTemplate,
                data,
                options,
                CancellationToken.None);

            Logger.LogInformation("Generated Apple Wallet .pkpass file ({Size} bytes)", pkpassBytes.Length);
            // Note: File download in Blazor Server requires JavaScript interop or file endpoint
            // For now, logging completion - implement JS download in future enhancement
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting Apple Wallet");
        }
    }

    private async Task ExportGoogleWallet()
    {
        if (selectedTemplate == null)
        {
            return;
        }

        try
        {
            var options = new GoogleWalletOptions
            {
                IssuerId = Configuration["WalletProviders:Google:IssuerId"] ?? "ISSUER_ID_NOT_CONFIGURED",
                ClassId = $"numbat_{selectedTemplate.Id}",
                ObjectId = Guid.NewGuid().ToString(),
                CardTitle = selectedTemplate.Name,
                Header = selectedTemplate.Name
            };

            // Create sample data from template fields
            var data = selectedTemplate.Fields.ToDictionary(
                f => f.Name,
                f => (object)(f.DefaultValue ?? string.Empty)
            );

            var googlePass = await GoogleWalletBuilder.GenerateGooglePassAsync(
                selectedTemplate,
                data,
                options,
                CancellationToken.None);

            var jwt = await GoogleWalletBuilder.CreateJwtAsync(googlePass);
            var addToWalletLink = GoogleWalletBuilder.GetAddToWalletLink(jwt);

            Logger.LogInformation("Generated Google Wallet link: {Link}", addToWalletLink);
            // Note: Link display requires UI modal or clipboard copy - implement in future enhancement
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting Google Wallet");
        }
    }

    private async Task ExportWebWallet()
    {
        if (selectedTemplate == null)
        {
            return;
        }

        try
        {
            var options = new WebWalletOptions
            {
                Theme = Configuration["WalletProviders:Web:Theme"] ?? "light",
                PrimaryColor = Configuration["WalletProviders:Web:PrimaryColor"] ?? "#007AFF",
                EnableQrCode = bool.Parse(Configuration["WalletProviders:Web:EnableQrCode"] ?? "true"),
                EnableSharing = bool.Parse(Configuration["WalletProviders:Web:EnableSharing"] ?? "true"),
                EnableOfflineMode = bool.Parse(Configuration["WalletProviders:Web:EnableOfflineMode"] ?? "true")
            };

            // Create sample data from template fields
            var data = selectedTemplate.Fields.ToDictionary(
                f => f.Name,
                f => (object)(f.DefaultValue ?? string.Empty)
            );

            var webWallet = await WebWalletBuilder.GenerateWebWalletAsync(
                selectedTemplate,
                data,
                options,
                CancellationToken.None);

            Logger.LogInformation("Generated Web Wallet HTML");
            // Note: HTML download requires file endpoint or JavaScript interop - implement in future enhancement
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting Web Wallet");
        }
    }

    private async Task CloneTemplate(WalletTemplate template)
    {
        try
        {
            var cloned = new WalletTemplate(
                template.TenantId,
                $"{template.Name} (Copy)",
                template.Description,
                template.Type,
                "Admin User");

            foreach (var field in template.Fields)
            {
                cloned.AddField(field);
            }

            foreach (var credType in template.SupportedCredentialTypes)
            {
                cloned.AddSupportedCredentialType(credType);
            }

            await WalletTemplateService.CreateTemplateAsync(cloned);
            await LoadTemplates();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cloning template");
        }
    }

    private async Task ToggleTemplateStatus(WalletTemplate template)
    {
        try
        {
            if (template.IsActive)
            {
                template.Deactivate();
            }
            else
            {
                template.Activate();
            }

            await WalletTemplateService.UpdateTemplateAsync(template);
            await LoadTemplates();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error toggling template status");
        }
    }

    private async Task DeleteTemplate(WalletTemplate template)
    {
        try
        {
            await WalletTemplateService.DeleteTemplateAsync(template.Id);
            await LoadTemplates();
            if (selectedTemplate?.Id == template.Id)
            {
                selectedTemplate = null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting template");
        }
    }

    private void ApplyQuickTemplate(string templateType)
    {
        if (selectedTemplate == null)
        {
            return;
        }

        var fields = templateType switch
        {
            "driverLicense" => GetDriverLicenseFields(),
            "passport" => GetPassportFields(),
            "healthCard" => GetHealthCardFields(),
            "studentId" => GetStudentIdFields(),
            _ => new List<WalletField>()
        };

        foreach (var field in fields)
        {
            try
            {
                selectedTemplate.AddField(field);
            }
            catch
            {
                // Field might already exist
            }
        }
    }

    private List<WalletField> GetDriverLicenseFields()
    {
        var fields = new List<WalletField>
        {
            CreateField("licenseNumber", "License Number", "text", true, "org.iso.18013.5.1.mDL.license_number"),
            CreateField("fullName", "Full Name", "text", true, "org.iso.18013.5.1.mDL.family_name"),
            CreateField("dateOfBirth", "Date of Birth", "date", true, "org.iso.18013.5.1.mDL.birth_date"),
            CreateField("issueDate", "Issue Date", "date", true, "org.iso.18013.5.1.mDL.issue_date"),
            CreateField("expiryDate", "Expiry Date", "date", true, "org.iso.18013.5.1.mDL.expiry_date"),
            CreateField("address", "Address", "text", true, "org.iso.18013.5.1.mDL.resident_address"),
            CreateField("portrait", "Photo", "image", true, "org.iso.18013.5.1.mDL.portrait"),
            CreateField("signature", "Signature", "image", false, "org.iso.18013.5.1.mDL.signature"),
            CreateField("vehicleCategories", "Vehicle Categories", "text", true, "org.iso.18013.5.1.mDL.driving_privileges")
        };
        return fields;
    }

    private List<WalletField> GetPassportFields()
    {
        var fields = new List<WalletField>
        {
            CreateField("passportNumber", "Passport Number", "text", true, "passport.documentNumber"),
            CreateField("surname", "Surname", "text", true, "passport.familyName"),
            CreateField("givenNames", "Given Names", "text", true, "passport.givenNames"),
            CreateField("nationality", "Nationality", "text", true, "passport.nationality"),
            CreateField("dateOfBirth", "Date of Birth", "date", true, "passport.birthDate"),
            CreateField("placeOfBirth", "Place of Birth", "text", true, "passport.birthPlace"),
            CreateField("dateOfIssue", "Date of Issue", "date", true, "passport.issueDate"),
            CreateField("dateOfExpiry", "Date of Expiry", "date", true, "passport.expiryDate"),
            CreateField("issuingAuthority", "Issuing Authority", "text", true, "passport.issuingAuthority"),
            CreateField("photo", "Photo", "image", true, "passport.photo")
        };
        return fields;
    }

    private List<WalletField> GetHealthCardFields()
    {
        var fields = new List<WalletField>
        {
            CreateField("medicareNumber", "Medicare Number", "text", true, "health.medicareNumber"),
            CreateField("individualRefNumber", "Individual Reference Number", "number", true, "health.irf"),
            CreateField("fullName", "Full Name", "text", true, "health.fullName"),
            CreateField("validTo", "Valid To", "date", true, "health.expiryDate"),
            CreateField("cardColor", "Card Color", "text", false, "health.cardColor")
        };
        return fields;
    }

    private List<WalletField> GetStudentIdFields()
    {
        var fields = new List<WalletField>
        {
            CreateField("studentId", "Student ID", "text", true, "student.id"),
            CreateField("fullName", "Full Name", "text", true, "student.fullName"),
            CreateField("institution", "Institution", "text", true, "student.institution"),
            CreateField("faculty", "Faculty/School", "text", false, "student.faculty"),
            CreateField("course", "Course", "text", true, "student.course"),
            CreateField("validFrom", "Valid From", "date", true, "student.validFrom"),
            CreateField("validTo", "Valid To", "date", true, "student.validTo"),
            CreateField("photo", "Photo", "image", true, "student.photo")
        };
        return fields;
    }

    private WalletField CreateField(string name, string label, string type, bool required, string mapping)
    {
        var field = new WalletField(name, label, type, required, 0);
        field.SetMappedCredentialField(mapping);
        return field;
    }

    private class CreateTemplateModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WalletTemplateType Type { get; set; } = WalletTemplateType.Custom;
        public List<string> SupportedCredentialTypes { get; set; } = new();
    }

    private class FieldModel
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string FieldType { get; set; } = "text";
        public bool IsRequired { get; set; }
        public bool IsEditable { get; set; } = true;
        public string ValidationRule { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public string MappedCredentialField { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    private class PlatformConfigModel
    {
        // Branding
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public string ForegroundColor { get; set; } = "#000000";
        public string LabelColor { get; set; } = "#666666";
        public string LogoText { get; set; } = string.Empty;

        // iOS-specific
        public List<string> HeaderFields { get; set; } = new();
        public List<string> PrimaryFields { get; set; } = new();
        public List<string> SecondaryFields { get; set; } = new();
        public List<string> AuxiliaryFields { get; set; } = new();
        public List<string> BackFields { get; set; } = new();
        public string BarcodeFormat { get; set; } = "PKBarcodeFormatQR";

        // Android-specific
        public string CardTitle { get; set; } = string.Empty;
        public string Header { get; set; } = string.Empty;
        public string Subheader { get; set; } = string.Empty;
        public List<string> TextModules { get; set; } = new();
        public List<string> InfoModules { get; set; } = new();

        // Web-specific
        public string Theme { get; set; } = "light";
        public bool EnableQrCode { get; set; } = true;
        public bool EnableSharing { get; set; } = true;
        public bool EnablePrinting { get; set; } = true;
        public string CustomCss { get; set; } = string.Empty;
    }
}
