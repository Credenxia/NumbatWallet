using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NumbatWallet.Domain.Entities;
using System.Text.Json;

namespace NumbatWallet.Infrastructure.Data.EntityConfigurations;

public class IssuanceEntityConfiguration : IEntityTypeConfiguration<Issuance>
{
    public void Configure(EntityTypeBuilder<Issuance> builder)
    {
        builder.ToTable("issuances");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.CredentialType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.WalletId)
            .IsRequired();

        builder.Property(e => e.RequesterId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.RequestedAt)
            .IsRequired();

        builder.Property(e => e.ApprovedBy)
            .HasMaxLength(200);

        builder.Property(e => e.RejectedBy)
            .HasMaxLength(200);

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(500);

        builder.Property(e => e.CompletedBy)
            .HasMaxLength(200);

        // Configure Claims as JSON column
        var claimsConverter = new ValueConverter<Dictionary<string, object>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
        );

        builder.Property("_claims")
            .HasColumnName("claims")
            .HasColumnType("jsonb")
            .HasConversion(claimsConverter)
            .Metadata.SetField("_claims");

        // Configure Metadata as JSON column
        var metadataConverter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
        );

        builder.Property("_metadata")
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(metadataConverter)
            .Metadata.SetField("_metadata");

        // Indexes
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.WalletId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CredentialType);
        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasIndex(e => new { e.TenantId, e.WalletId });

        // Ignore domain events for EF Core
        builder.Ignore(e => e.DomainEvents);
    }
}