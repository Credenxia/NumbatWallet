using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Commands.Passes;

public sealed class GenerateApplePassCommandHandler(
    IPersonRepository personRepository,
    IWalletRepository walletRepository,
    IWalletTemplateRepository walletTemplateRepository,
    IAppleWalletBuilder appleWalletBuilder,
    ILogger<GenerateApplePassCommandHandler> logger) : ICommandHandler<GenerateApplePassCommand, WalletPackageDto>
{
    public async Task<WalletPackageDto> HandleAsync(
        GenerateApplePassCommand command,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating Apple Wallet pass for person {PersonId} in tenant {TenantId}",
            command.PersonId, command.TenantId);

        var person = await personRepository.GetWithWalletsAsync(command.PersonId, cancellationToken)
            ?? throw new InvalidOperationException($"Person {command.PersonId} not found");

        var wallet = person.Wallets.FirstOrDefault(w => w.Status == WalletStatus.Active)
            ?? throw new InvalidOperationException($"No active wallet found for person {command.PersonId}");

        var template = command.TemplateId.HasValue
            ? await walletTemplateRepository.GetByIdAsync(command.TemplateId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Template {command.TemplateId} not found")
            : (await walletTemplateRepository.GetByTenantIdAsync(command.TenantId, cancellationToken))
                .FirstOrDefault(t => t.IsActive)
                ?? throw new InvalidOperationException("No active wallet template found for tenant");

        var credentials = await walletRepository.GetByPersonIdAsync(command.PersonId, cancellationToken);
        var data = BuildPassData(person, wallet, template, command.AdditionalData);

        var options = new AppleWalletOptions
        {
            OrganizationName = template.Metadata.TryGetValue("organizationName", out var orgName)
                ? orgName.ToString()! : "NumbatWallet",
            Description = template.Description,
            BackgroundColor = template.Metadata.TryGetValue("backgroundColor", out var bg)
                ? bg.ToString()! : "#FFFFFF",
            ForegroundColor = template.Metadata.TryGetValue("foregroundColor", out var fg)
                ? fg.ToString()! : "#000000",
            LabelColor = template.Metadata.TryGetValue("labelColor", out var lbl)
                ? lbl.ToString()! : "#666666",
            LogoText = template.Metadata.TryGetValue("logoText", out var logo)
                ? logo.ToString()! : template.Name,
            EnableBarcode = true,
            BarcodeFormat = "PKBarcodeFormatQR",
            BarcodeMessage = wallet.WalletDid
        };

        MapFieldsToApplePass(template, data, options);

        var pkpassData = await appleWalletBuilder.GeneratePkpassAsync(
            template, data, options, cancellationToken);

        logger.LogInformation("Generated Apple Wallet pass ({Size} bytes) for person {PersonId}",
            pkpassData.Length, command.PersonId);

        return new WalletPackageDto
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            TemplateName = template.Name,
            Platform = "AppleWallet",
            PackageData = pkpassData,
            ContentType = "application/vnd.apple.pkpass",
            PackageType = "application/vnd.apple.pkpass",
            FileName = $"{template.Name.Replace(' ', '_')}.pkpass",
            GeneratedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Data = data,
            IsSuccess = true,
            QrCodeData = null
        };
    }

    private static Dictionary<string, object> BuildPassData(
        Domain.Aggregates.Person person,
        Domain.Aggregates.Wallet wallet,
        Domain.Entities.WalletTemplate template,
        Dictionary<string, object> additionalData)
    {
        var data = new Dictionary<string, object>
        {
            ["personId"] = person.Id.ToString(),
            ["firstName"] = person.FirstName,
            ["lastName"] = person.LastName,
            ["fullName"] = person.GetFullName(),
            ["walletId"] = wallet.Id.ToString(),
            ["walletDid"] = wallet.WalletDid,
            ["templateType"] = template.Type.ToString(),
            ["issuedAt"] = DateTimeOffset.UtcNow.ToString("O")
        };

        foreach (var field in template.Fields.Where(f => f.DefaultValue is not null))
        {
            data.TryAdd(field.Name, field.DefaultValue!);
        }

        foreach (var (key, value) in additionalData)
        {
            data[key] = value;
        }

        return data;
    }

    private static void MapFieldsToApplePass(
        Domain.Entities.WalletTemplate template,
        Dictionary<string, object> data,
        AppleWalletOptions options)
    {
        foreach (var field in template.Fields.OrderBy(f => f.DisplayOrder))
        {
            var value = data.TryGetValue(field.Name, out var v) ? v.ToString() ?? "" : field.DefaultValue ?? "";
            var appleField = new AppleWalletField
            {
                Key = field.Name,
                Label = field.Label,
                Value = value
            };

            var placement = field.Properties.TryGetValue("applePlacement", out var p) ? p.ToString() : null;
            switch (placement)
            {
                case "header":
                    options.HeaderFields.Add(appleField);
                    break;
                case "primary":
                    options.PrimaryFields.Add(appleField);
                    break;
                case "auxiliary":
                    options.AuxiliaryFields.Add(appleField);
                    break;
                case "back":
                    options.BackFields.Add(appleField);
                    break;
                default:
                    options.SecondaryFields.Add(appleField);
                    break;
            }
        }
    }
}
