using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Aggregates;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.Description)
            .HasMaxLength(1000);

        builder.Property(o => o.ContactEmail)
            .HasMaxLength(256);

        builder.Property(o => o.ContactPhone)
            .HasMaxLength(50);

        builder.Property(o => o.Website)
            .HasMaxLength(500);

        builder.Property(o => o.IsActive)
            .IsRequired();

        builder.Property(o => o.TenantId)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(o => o.ModifiedAt);

        builder.Property(o => o.ModifiedBy)
            .HasMaxLength(256);

        // Indexes
        builder.HasIndex(o => o.TenantId);
        builder.HasIndex(o => o.Name);

        // Ignore domain events
        builder.Ignore(o => o.DomainEvents);
    }
}
