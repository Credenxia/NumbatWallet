using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for AdminUser entity
/// </summary>
public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Ignore(u => u.DisplayName); // Computed property

        // Store roles as JSON - using field access via EF Core shadow property
        builder.Property<List<string>>("_roles")
            .HasColumnName("roles")
            .HasColumnType("jsonb");

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.IsLocked)
            .IsRequired();

        builder.Property(u => u.LockReason)
            .HasMaxLength(500);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.LastLoginAt);

        builder.Property(u => u.LastPasswordChangeAt);

        builder.Property(u => u.TenantId)
            .IsRequired();

        // Indexes
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.TenantId);
        builder.HasIndex(u => u.IsActive);
        builder.HasIndex(u => new { u.LastName, u.FirstName });
    }
}