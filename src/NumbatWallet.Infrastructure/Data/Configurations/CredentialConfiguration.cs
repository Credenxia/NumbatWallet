using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Aggregates;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("Credentials");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.WalletId)
            .IsRequired();

        builder.Property(c => c.IssuerId)
            .IsRequired();

        builder.Property(c => c.CredentialType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(c => c.CredentialData)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(c => c.SchemaId)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.IssuedAt)
            .IsRequired();

        builder.Property(c => c.ExpiresAt);
        builder.Property(c => c.RevokedAt);

        builder.Property(c => c.RevocationReason)
            .HasMaxLength(500);

        builder.Property(c => c.SuspensionReason)
            .HasMaxLength(500);

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.ModifiedAt);
        builder.Property(c => c.ModifiedBy)
            .HasMaxLength(256);

        // Indexes
        builder.HasIndex(c => c.WalletId);
        builder.HasIndex(c => c.IssuerId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TenantId, c.WalletId });
        builder.HasIndex(c => new { c.TenantId, c.Status });

        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);
    }
}