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

        builder.Property(w => w.WalletDid)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(w => w.PersonId)
            .IsRequired();

        builder.Property(w => w.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(w => w.SuspensionReason)
            .HasMaxLength(1000);

        builder.Property(w => w.LockReason)
            .HasMaxLength(1000);

        builder.Property(w => w.ExternalId)
            .HasMaxLength(256);

        builder.Property(w => w.ExpiresAt);

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
        builder.HasIndex(w => w.WalletDid)
            .IsUnique();
        builder.HasIndex(w => w.PersonId);
        builder.HasIndex(w => w.TenantId);
        builder.HasIndex(w => w.Status);
        builder.HasIndex(w => new { w.TenantId, w.PersonId });

        // Relationships
        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(w => w.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Credentials)
            .WithOne(c => c.Wallet)
            .HasForeignKey(c => c.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore(w => w.DomainEvents);
    }
}
