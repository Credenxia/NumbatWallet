using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Aggregates;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class PresentationConfiguration : IEntityTypeConfiguration<Presentation>
{
    public void Configure(EntityTypeBuilder<Presentation> builder)
    {
        builder.ToTable("Presentations");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.CredentialId)
            .IsRequired();

        builder.Property(p => p.WalletId)
            .IsRequired();

        builder.Property(p => p.VerifierId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.Purpose)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.DisclosedClaimsJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.PresentedAt)
            .IsRequired();

        builder.Property(p => p.ExpiresAt)
            .IsRequired();

        builder.Property(p => p.VerifiedAt);

        builder.Property(p => p.VerificationCount)
            .IsRequired();

        builder.Property(p => p.TenantId)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.ModifiedAt);
        builder.Property(p => p.ModifiedBy)
            .HasMaxLength(256);

        // Indexes
        builder.HasIndex(p => p.WalletId);
        builder.HasIndex(p => p.CredentialId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => new { p.TenantId, p.WalletId });

        // Relationships (no navigation properties on the aggregate)
        builder.HasOne<Credential>()
            .WithMany()
            .HasForeignKey(p => p.CredentialId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore(p => p.DomainEvents);
    }
}
