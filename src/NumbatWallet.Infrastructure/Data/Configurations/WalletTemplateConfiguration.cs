using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for WalletTemplate entity
/// Uses public properties with private setters for EF Core compatibility
/// </summary>
public class WalletTemplateConfiguration : IEntityTypeConfiguration<WalletTemplate>
{
    public void Configure(EntityTypeBuilder<WalletTemplate> builder)
    {
        builder.ToTable("WalletTemplates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId)
            .IsRequired();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(t => t.Type)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(t => t.Version)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.IsActive)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(200);

        // Configure Fields as owned entity collection
        builder.OwnsMany(t => t.Fields, fieldsBuilder =>
        {
            fieldsBuilder.ToTable("WalletTemplateFields");

            fieldsBuilder.WithOwner().HasForeignKey("WalletTemplateId");

            fieldsBuilder.Property<int>("Id")
                .ValueGeneratedOnAdd();

            fieldsBuilder.HasKey("Id");

            fieldsBuilder.Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(100);

            fieldsBuilder.Property(f => f.Label)
                .IsRequired()
                .HasMaxLength(200);

            fieldsBuilder.Property(f => f.FieldType)
                .IsRequired()
                .HasMaxLength(50);

            fieldsBuilder.Property(f => f.IsRequired)
                .IsRequired();

            fieldsBuilder.Property(f => f.IsEditable)
                .IsRequired();

            fieldsBuilder.Property(f => f.DisplayOrder)
                .IsRequired();

            fieldsBuilder.Property(f => f.ValidationRule)
                .HasMaxLength(500);

            fieldsBuilder.Property(f => f.DefaultValue)
                .HasMaxLength(500);

            fieldsBuilder.Property(f => f.MappedCredentialField)
                .HasMaxLength(200);

            // Store Properties dictionary as JSON
            fieldsBuilder.Property(f => f.Properties)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
        });

        // Store SupportedCredentialTypes as JSON array
        builder.Property(t => t.SupportedCredentialTypes)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .IsRequired();

        // Store Metadata as JSON object
        builder.Property(t => t.Metadata)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .IsRequired();

        // Create indexes for performance
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.Type);
        builder.HasIndex(t => t.IsActive);
        builder.HasIndex(t => new { t.TenantId, t.Type });
    }
}
