using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class CertificateRevocationConfiguration : IEntityTypeConfiguration<CertificateRevocation>
{
    public void Configure(EntityTypeBuilder<CertificateRevocation> builder)
    {
        builder.ToTable("CertificateRevocations");

        builder.HasKey(cr => cr.Id);
        builder.Property(cr => cr.Id)
            .ValueGeneratedNever();

        builder.Property(cr => cr.SerialNumber)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(cr => cr.SerialNumber);

        builder.Property(cr => cr.Thumbprint)
            .HasMaxLength(128);

        builder.HasIndex(cr => cr.Thumbprint);

        builder.Property(cr => cr.RevocationDate)
            .IsRequired();

        builder.Property(cr => cr.Reason)
            .IsRequired();

        builder.Property(cr => cr.Comment)
            .HasMaxLength(1000);

        builder.Property(cr => cr.RevokedBy)
            .HasMaxLength(256);

        builder.Property(cr => cr.InvalidityDate);

        builder.Property(cr => cr.IsHold)
            .IsRequired();

        builder.Property(cr => cr.CreatedAt)
            .IsRequired();

        builder.Property(cr => cr.UpdatedAt);

        // Indexes
        builder.HasIndex(cr => cr.IsHold);
        builder.HasIndex(cr => cr.RevocationDate);
    }
}
