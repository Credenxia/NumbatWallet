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

        // Value objects - stored as protected JSONB.
        // Email/Phone are ENCRYPTED at rest (AES-256-GCM, non-deterministic ciphertext).
        // Exact-match lookups (login, uniqueness) go through the deterministic HMAC search-token
        // shadow columns below, populated by SearchTokenInterceptor on every save.
        builder.OwnsOne(p => p.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasColumnType("jsonb")
                .HasConversion(new ProtectedFieldConverter(encrypt: true));
        });

        builder.OwnsOne(p => p.PhoneNumber, phone =>
        {
            phone.Property(ph => ph.Value)
                .HasColumnName("PhoneNumberValue")
                .IsRequired()
                .HasMaxLength(500) // Increased to accommodate encrypted data
                .HasConversion(new ProtectedFieldConverter(encrypt: true));

            phone.Property(ph => ph.CountryCode)
                .HasColumnName("PhoneNumberCountryCode")
                .HasMaxLength(5);
        });

        // Deterministic search tokens (HMAC-SHA256 of the normalized value, deployment-wide
        // pepper) — the ONLY queryable form of email/phone now that the values are encrypted.
        // Shadow properties: written by SearchTokenInterceptor, queried via EF.Property.
        builder.Property<string?>("EmailSearchToken")
            .HasColumnName("email_search_token")
            .HasMaxLength(64);

        builder.Property<string?>("PhoneSearchToken")
            .HasColumnName("phone_search_token")
            .HasMaxLength(64);

        builder.HasIndex("EmailSearchToken");
        builder.HasIndex("PhoneSearchToken");

        // PIN security fields
        builder.Property(p => p.PinHash)
            .HasMaxLength(500); // BCrypt hash is ~60 chars, allow extra for future algorithms

        builder.Property(p => p.FailedPinAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.PinLockedUntil);

        builder.Property(p => p.LastPinAttemptAt);

        // Indexes
        builder.HasIndex(p => p.TenantId);

        // Ignore domain events
        builder.Ignore(p => p.DomainEvents);
    }
}
