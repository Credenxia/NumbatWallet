using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class SupportedCredentialTypeConfiguration : IEntityTypeConfiguration<SupportedCredentialType>
{
    public void Configure(EntityTypeBuilder<SupportedCredentialType> builder)
    {
        builder.ToTable("SupportedCredentialTypes");

        builder.HasKey(sct => sct.Id);
        builder.Property(sct => sct.Id)
            .ValueGeneratedNever();

        builder.Property(sct => sct.IssuerId)
            .IsRequired();

        builder.HasIndex(sct => sct.IssuerId);

        builder.Property(sct => sct.TypeName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(sct => sct.SchemaId)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(sct => sct.SchemaVersion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sct => sct.IsActive)
            .IsRequired();

        builder.Property(sct => sct.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(sct => sct.IsActive);
        builder.HasIndex(sct => sct.TypeName);
        builder.HasIndex(sct => new { sct.IssuerId, sct.TypeName });
    }
}
