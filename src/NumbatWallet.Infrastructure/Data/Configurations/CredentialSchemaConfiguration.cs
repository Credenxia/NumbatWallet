using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Entities;
using System.Text.Json;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class CredentialSchemaConfiguration : IEntityTypeConfiguration<CredentialSchema>
{
    public void Configure(EntityTypeBuilder<CredentialSchema> builder)
    {
        builder.ToTable("CredentialSchemas");

        builder.HasKey(cs => cs.Id);
        builder.Property(cs => cs.Id)
            .ValueGeneratedNever();

        builder.Property(cs => cs.TenantId)
            .IsRequired();

        builder.HasIndex(cs => cs.TenantId);

        builder.Property(cs => cs.SchemaId)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(cs => cs.SchemaId)
            .IsUnique();

        builder.Property(cs => cs.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(cs => cs.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(cs => cs.Version)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cs => cs.Type)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cs => cs.IsActive)
            .IsRequired();

        builder.Property(cs => cs.CreatedAt)
            .IsRequired();

        builder.Property(cs => cs.UpdatedAt);

        // Ignore read-only properties that expose the backing fields
        builder.Ignore(cs => cs.Attributes);
        builder.Ignore(cs => cs.Contexts);
        builder.Ignore(cs => cs.Metadata);

        // Store attributes (CredentialField list) as JSONB - accessing private backing field
        builder.Property<List<CredentialField>>("_attributes")
            .HasColumnName("Attributes")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<CredentialField>>(v, (JsonSerializerOptions?)null) ?? new List<CredentialField>());

        // Store contexts as JSONB
        builder.Property<List<string>>("_contexts")
            .HasColumnName("Contexts")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        // Store metadata as JSONB
        builder.Property<Dictionary<string, object>>("_metadata")
            .HasColumnName("Metadata")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

        // Indexes
        builder.HasIndex(cs => cs.IsActive);
        builder.HasIndex(cs => cs.Name);
    }
}
