using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Aggregates;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .ValueGeneratedNever();

        builder.Property(w => w.WalletName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(w => w.PersonId)
            .IsRequired();

        builder.Property(w => w.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(w => w.TenantId)
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .IsRequired();

        builder.Property(w => w.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(w => w.ModifiedAt);
        builder.Property(w => w.ModifiedBy)
            .HasMaxLength(256);

        // Indexes
        builder.HasIndex(w => w.PersonId);
        builder.HasIndex(w => w.TenantId);
        builder.HasIndex(w => w.Status);
        builder.HasIndex(w => new { w.TenantId, w.PersonId });

        // Ignore domain events
        builder.Ignore(w => w.DomainEvents);
    }
}