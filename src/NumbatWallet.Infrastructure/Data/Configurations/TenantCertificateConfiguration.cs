using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class TenantCertificateConfiguration : IEntityTypeConfiguration<TenantCertificate>
{
    public void Configure(EntityTypeBuilder<TenantCertificate> builder)
    {
        builder.ToTable("TenantCertificates");

        builder.HasKey(tc => tc.Id);
        builder.Property(tc => tc.Id)
            .ValueGeneratedNever();

        builder.Property(tc => tc.TenantId)
            .IsRequired();

        builder.HasIndex(tc => tc.TenantId);

        builder.Property(tc => tc.CertificateData)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(tc => tc.Thumbprint)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(tc => tc.Thumbprint)
            .IsUnique();

        builder.Property(tc => tc.SubjectDn)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(tc => tc.IssuerDn)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(tc => tc.ValidFrom)
            .IsRequired();

        builder.Property(tc => tc.ValidTo)
            .IsRequired();

        builder.Property(tc => tc.IsActive)
            .IsRequired();

        builder.Property(tc => tc.Purpose)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(tc => tc.TrustLevel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(tc => tc.CreatedAt)
            .IsRequired();

        builder.Property(tc => tc.RevokedAt);

        builder.Property(tc => tc.RevocationReason)
            .HasMaxLength(1000);

        builder.Property(tc => tc.LastUsedAt);

        builder.Property(tc => tc.UsageCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(tc => tc.SerialNumber)
            .HasMaxLength(128);

        builder.Property(tc => tc.NotBefore)
            .IsRequired();

        builder.Property(tc => tc.NotAfter)
            .IsRequired();

        builder.Property(tc => tc.IsBlocked)
            .IsRequired()
            .HasDefaultValue(false);

        // Computed property
        builder.Ignore(tc => tc.IsRevoked);

        // Indexes
        builder.HasIndex(tc => tc.IsActive);
        builder.HasIndex(tc => tc.ValidTo);
        builder.HasIndex(tc => tc.SerialNumber);
    }
}
