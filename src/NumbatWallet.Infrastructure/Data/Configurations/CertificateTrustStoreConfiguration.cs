using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Entities;
using System.Text.Json;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class CertificateTrustStoreConfiguration : IEntityTypeConfiguration<CertificateTrustStore>
{
    public void Configure(EntityTypeBuilder<CertificateTrustStore> builder)
    {
        builder.ToTable("CertificateTrustStores");

        builder.HasKey(cts => cts.Id);
        builder.Property(cts => cts.Id)
            .ValueGeneratedNever();

        builder.Property(cts => cts.TenantId)
            .IsRequired();

        builder.HasIndex(cts => cts.TenantId);

        builder.Property(cts => cts.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(cts => cts.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(cts => cts.IsActive)
            .IsRequired();

        builder.Property(cts => cts.CreatedAt)
            .IsRequired();

        builder.Property(cts => cts.UpdatedAt);

        // Store collections as JSONB
        builder.Property<List<Guid>>("_trustedCertificateIds")
            .HasColumnName("TrustedCertificateIds")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>());

        builder.Property<List<Guid>>("_trustedAuthorityIds")
            .HasColumnName("TrustedAuthorityIds")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>());

        builder.Property<List<string>>("_revokedThumbprints")
            .HasColumnName("RevokedThumbprints")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
    }
}
