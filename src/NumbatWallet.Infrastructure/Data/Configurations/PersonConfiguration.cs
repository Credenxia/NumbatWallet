using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.ValueObjects;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("Persons");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.DateOfBirth);

        builder.Property(p => p.IsVerified)
            .IsRequired();

        builder.Property(p => p.VerifiedAt);

        builder.Property(p => p.VerificationLevel)
            .HasConversion<string>()
            .HasMaxLength(50);

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

        // Value objects
        builder.OwnsOne(p => p.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(256);

            email.HasIndex(e => e.Value)
                .IsUnique();
        });

        builder.OwnsOne(p => p.PhoneNumber, phone =>
        {
            phone.Property(ph => ph.Value)
                .HasColumnName("PhoneNumber")
                .IsRequired()
                .HasMaxLength(50);

            phone.HasIndex(ph => ph.Value);
        });

        // Indexes
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.IsVerified);
        builder.HasIndex(p => new { p.TenantId, p.IsVerified });

        // Ignore domain events
        builder.Ignore(p => p.DomainEvents);
    }
}