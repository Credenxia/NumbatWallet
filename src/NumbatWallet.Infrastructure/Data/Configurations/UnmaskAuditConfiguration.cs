using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for UnmaskAudit entity
/// Optimized for compliance querying and reporting
/// </summary>
public class UnmaskAuditConfiguration : IEntityTypeConfiguration<UnmaskAudit>
{
    public void Configure(EntityTypeBuilder<UnmaskAudit> builder)
    {
        builder.ToTable("unmask_audits");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.EntityId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.FieldName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Classification)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(u => u.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.TenantId)
            .IsRequired();

        builder.Property(u => u.UnmaskedAt)
            .IsRequired();

        builder.Property(u => u.DurationSeconds)
            .IsRequired();

        builder.Property(u => u.ExpiresAt)
            .IsRequired();

        builder.Property(u => u.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(u => u.UserAgent)
            .HasMaxLength(500);

        builder.Property(u => u.ApprovalReference)
            .HasMaxLength(200);

        // Indexes for efficient compliance querying
        builder.HasIndex(u => u.UserId)
            .HasDatabaseName("IX_UnmaskAudits_UserId");

        builder.HasIndex(u => new { u.EntityType, u.EntityId })
            .HasDatabaseName("IX_UnmaskAudits_Entity");

        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("IX_UnmaskAudits_TenantId");

        builder.HasIndex(u => u.UnmaskedAt)
            .HasDatabaseName("IX_UnmaskAudits_UnmaskedAt");

        builder.HasIndex(u => new { u.TenantId, u.UnmaskedAt })
            .HasDatabaseName("IX_UnmaskAudits_TenantId_UnmaskedAt");

        builder.HasIndex(u => u.Classification)
            .HasDatabaseName("IX_UnmaskAudits_Classification");

        builder.HasIndex(u => new { u.UserId, u.UnmaskedAt })
            .HasDatabaseName("IX_UnmaskAudits_UserId_UnmaskedAt");
    }
}
