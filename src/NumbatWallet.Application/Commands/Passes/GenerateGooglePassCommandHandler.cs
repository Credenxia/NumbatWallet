using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Commands.Passes;

public sealed class GenerateGooglePassCommandHandler(
    IPersonRepository personRepository,
    IWalletRepository walletRepository,
    IWalletTemplateRepository walletTemplateRepository,
    IGoogleWalletBuilder googleWalletBuilder,
    ILogger<GenerateGooglePassCommandHandler> logger) : ICommandHandler<GenerateGooglePassCommand, WalletPackageDto>
{
    public async Task<WalletPackageDto> HandleAsync(
        GenerateGooglePassCommand command,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating Google Wallet pass for person {PersonId} in tenant {TenantId}",
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

        var data = BuildPassData(person, wallet, template, command.AdditionalData);

        var options = new GoogleWalletOptions
        {
            ClassId = $"{command.TenantId}.{template.Type}",
            ObjectId = $"{command.TenantId}.{wallet.Id}",
            CardTitle = template.Name,
            Header = person.GetFullName(),
            Subheader = template.Description,
            HexBackgroundColor = template.Metadata.TryGetValue("backgroundColor", out var bg)
                ? bg.ToString()! : "#1a2744",
            Logo = template.Metadata.TryGetValue("logoUrl", out var logoUrl)
                ? logoUrl.ToString()! : "",
            BarcodeType = "QR_CODE",
            BarcodeValue = wallet.WalletDid,
            BarcodeAlternateText = wallet.WalletDid
        };

        MapFieldsToGooglePass(template, data, options);

        var googlePass = await googleWalletBuilder.GenerateGooglePassAsync(
            template, data, options, cancellationToken);

        var jwt = await googleWalletBuilder.CreateJwtAsync(googlePass);
        var saveUrl = googleWalletBuilder.GetAddToWalletLink(jwt);

        logger.LogInformation("Generated Google Wallet pass for person {PersonId}, save URL ready",
            command.PersonId);

        return new WalletPackageDto
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            TemplateName = template.Name,
            Platform = "GoogleWallet",
            PackageUrl = saveUrl,
            ContentType = "application/jwt",
            PackageType = "application/jwt",
            FileName = $"{template.Name.Replace(' ', '_')}_google.jwt",
            GeneratedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Data = data,
            Metadata = new Dictionary<string, object>
            {
                ["jwt"] = jwt,
                ["saveUrl"] = saveUrl,
                ["classId"] = options.ClassId,
                ["objectId"] = options.ObjectId
            },
            IsSuccess = true
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

    private static void MapFieldsToGooglePass(
        Domain.Entities.WalletTemplate template,
        Dictionary<string, object> data,
        GoogleWalletOptions options)
    {
        foreach (var field in template.Fields.OrderBy(f => f.DisplayOrder))
        {
            var value = data.TryGetValue(field.Name, out var v) ? v.ToString() ?? "" : field.DefaultValue ?? "";

            var placement = field.Properties.TryGetValue("googlePlacement", out var p) ? p.ToString() : "text";
            if (placement == "info")
            {
                options.InfoModulesData[field.Label] = value;
            }
            else
            {
                options.TextModulesData[field.Label] = value;
            }
        }
    }
}
