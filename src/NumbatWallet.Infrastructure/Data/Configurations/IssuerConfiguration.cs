using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Aggregates;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class IssuerConfiguration : IEntityTypeConfiguration<Issuer>
{
    public void Configure(EntityTypeBuilder<Issuer> builder)
    {
        builder.ToTable("Issuers");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.Code)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(i => i.TrustedDomain)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.IsActive)
            .IsRequired();

        builder.Property(i => i.TenantId)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.ModifiedAt);
        builder.Property(i => i.ModifiedBy)
            .HasMaxLength(256);

        // Indexes
        builder.HasIndex(i => i.Code)
            .IsUnique();

        builder.HasIndex(i => i.TrustedDomain)
            .IsUnique();

        builder.HasIndex(i => i.TenantId);
        builder.HasIndex(i => i.IsActive);
        builder.HasIndex(i => new { i.TenantId, i.IsActive });

        // Ignore domain events
        builder.Ignore(i => i.DomainEvents);
    }
}