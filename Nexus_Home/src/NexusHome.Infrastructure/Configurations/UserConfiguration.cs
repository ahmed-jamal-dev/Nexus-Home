namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;

/// <summary>
/// User entity configuration.
/// Per v1.3 baseline Section 6.2.2.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.EmailVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // Platform Administration: roles version for token invalidation
        builder.Property(u => u.PlatformRolesVersion)
            .HasColumnName("platform_roles_version")
            .HasDefaultValue(1)
            .IsRequired();

        // Partial unique index: email unique only among non-deleted users
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uk_users_email_active");

        // Index for deleted_at queries
        builder.HasIndex(u => u.DeletedAt)
            .HasDatabaseName("idx_users_deleted_at")
            .HasFilter("deleted_at IS NOT NULL");

        // Navigation: Platform roles
        builder.HasMany(u => u.UserPlatformRoles)
            .WithOne(upr => upr.User)
            .HasForeignKey(upr => upr.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}