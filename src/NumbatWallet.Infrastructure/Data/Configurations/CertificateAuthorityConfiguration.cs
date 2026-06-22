using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class CertificateAuthorityConfiguration : IEntityTypeConfiguration<CertificateAuthority>
{
    public void Configure(EntityTypeBuilder<CertificateAuthority> builder)
    {
        builder.ToTable("CertificateAuthorities");

        builder.HasKey(ca => ca.Id);
        builder.Property(ca => ca.Id)
            .ValueGeneratedNever();

        builder.Property(ca => ca.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(ca => ca.CertificateData)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(ca => ca.Thumbprint)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(ca => ca.Thumbprint)
            .IsUnique();

        builder.Property(ca => ca.SubjectDn)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(ca => ca.IsTrusted)
            .IsRequired();

        builder.Property(ca => ca.TrustLevel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(ca => ca.CrlUrl)
            .HasMaxLength(500);

        builder.Property(ca => ca.OcspUrl)
            .HasMaxLength(500);

        builder.Property(ca => ca.ValidFrom)
            .IsRequired();

        builder.Property(ca => ca.ValidTo)
            .IsRequired();

        builder.Property(ca => ca.CreatedAt)
            .IsRequired();

        builder.Property(ca => ca.LastValidatedAt);

        // Indexes
        builder.HasIndex(ca => ca.IsTrusted);
        builder.HasIndex(ca => ca.ValidTo);
    }
}
