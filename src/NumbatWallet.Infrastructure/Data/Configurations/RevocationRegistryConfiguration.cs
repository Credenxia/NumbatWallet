using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class RevocationRegistryConfiguration : IEntityTypeConfiguration<RevocationRegistry>
{
    public void Configure(EntityTypeBuilder<RevocationRegistry> builder)
    {
        builder.ToTable("RevocationRegistries");

        builder.HasKey(rr => rr.Id);
        builder.Property(rr => rr.Id)
            .ValueGeneratedNever();

        builder.Property(rr => rr.IssuerId)
            .IsRequired();

        builder.HasIndex(rr => rr.IssuerId);

        builder.Property(rr => rr.RegistryId)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(rr => rr.RegistryId)
            .IsUnique();

        builder.Property(rr => rr.CredentialType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(rr => rr.IsActive)
            .IsRequired();

        builder.Property(rr => rr.MaxCredentials)
            .IsRequired();

        builder.Property(rr => rr.CurrentCredentials)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(rr => rr.CreatedAt)
            .IsRequired();

        builder.Property(rr => rr.FullAt);

        // Indexes
        builder.HasIndex(rr => rr.IsActive);
        builder.HasIndex(rr => new { rr.IssuerId, rr.CredentialType });
    }
}
