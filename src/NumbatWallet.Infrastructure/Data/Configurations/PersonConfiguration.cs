using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Infrastructure.Data.Converters;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        // Table will be named "persons" due to snake_case convention
        builder.ToTable("Persons");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        // Protected fields stored as JSONB
        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasConversion(new ProtectedFieldConverter());

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasConversion(new ProtectedFieldConverter());

        // DateOfBirth is also sensitive - store as protected JSONB
        builder.Property(p => p.DateOfBirth)
            .HasColumnType("jsonb")
            .HasConversion(
                v => new ProtectedFieldConverter().ConvertToProviderTyped(v.ToString("yyyy-MM-dd")),
                v => DateOnly.Parse(new ProtectedFieldConverter().ConvertFromProviderTyped(v)));

        // IsVerified is a computed property - ignore it
        builder.Ignore(p => p.IsVerified);

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

        // Value objects - stored as protected JSONB
        builder.OwnsOne(p => p.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasColumnType("jsonb")
                .HasConversion(new ProtectedFieldConverter());

            // Index on the searchable token (will be added via interceptor)
            email.HasIndex(e => e.Value);
        });

        builder.OwnsOne(p => p.PhoneNumber, phone =>
        {
            phone.Property(ph => ph.Value)
                .HasColumnName("PhoneNumberValue")
                .IsRequired()
                .HasMaxLength(20)
                .HasConversion(new ProtectedFieldConverter());

            phone.Property(ph => ph.CountryCode)
                .HasColumnName("PhoneNumberCountryCode")
                .HasMaxLength(5);

            // Index on the searchable token (will be added via interceptor)
            phone.HasIndex(ph => ph.Value);
        });

        // Indexes
        builder.HasIndex(p => p.TenantId);

        // Ignore domain events
        builder.Ignore(p => p.DomainEvents);
    }
}