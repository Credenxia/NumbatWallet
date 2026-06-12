using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Aggregates;

namespace NumbatWallet.Infrastructure.Data.Configurations;

public class PresentationRequestConfiguration : IEntityTypeConfiguration<PresentationRequest>
{
    public void Configure(EntityTypeBuilder<PresentationRequest> builder)
    {
        builder.ToTable("PresentationRequests");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.VerifierId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.Purpose)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.RequestedCredentialType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.RequestedClaimsJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(p => p.PresentationDefinitionJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(p => p.Nonce)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.ExpiresAt)
            .IsRequired();

        builder.Property(p => p.FulfilledAt);
        builder.Property(p => p.FulfilledByPresentationId);

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

        // Indexes
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => new { p.TenantId, p.VerifierId });

        // Ignore domain events
        builder.Ignore(p => p.DomainEvents);
    }
}
