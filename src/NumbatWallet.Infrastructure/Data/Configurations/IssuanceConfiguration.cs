using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Entities;
using System.Text.Json;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class IssuanceConfiguration : IEntityTypeConfiguration<Issuance>
{
    public void Configure(EntityTypeBuilder<Issuance> builder)
    {
        builder.ToTable("Issuances");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.TenantId)
            .IsRequired();

        builder.HasIndex(i => i.TenantId);

        builder.Property(i => i.CredentialType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.WalletId)
            .IsRequired();

        builder.HasIndex(i => i.WalletId);

        builder.Property(i => i.RequesterId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(i => i.Status);

        builder.Property(i => i.RequestedAt)
            .IsRequired();

        builder.Property(i => i.ApprovedAt);
        builder.Property(i => i.ApprovedBy)
            .HasMaxLength(256);

        builder.Property(i => i.RejectedAt);
        builder.Property(i => i.RejectedBy)
            .HasMaxLength(256);

        builder.Property(i => i.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(i => i.CompletedAt);
        builder.Property(i => i.CompletedBy)
            .HasMaxLength(256);

        builder.Property(i => i.CredentialId);
        builder.Property(i => i.ExpiryDate);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.UpdatedAt);

        // Store claims as JSONB
        builder.Property<Dictionary<string, object>>("_claims")
            .HasColumnName("Claims")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

        // Store metadata as JSONB
        builder.Property<Dictionary<string, string>>("_metadata")
            .HasColumnName("Metadata")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

        // Ignore domain events
        builder.Ignore(i => i.DomainEvents);
    }
}
