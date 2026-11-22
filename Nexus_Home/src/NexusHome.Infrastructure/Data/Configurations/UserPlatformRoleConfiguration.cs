namespace NexusHome.Infrastructure.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;

/// <summary>
/// EF Core configuration for UserPlatformRole entity.
/// </summary>
public sealed class UserPlatformRoleConfiguration : IEntityTypeConfiguration<UserPlatformRole>
{
    public void Configure(EntityTypeBuilder<UserPlatformRole> builder)
    {
        builder.ToTable("user_platform_roles");

        builder.HasKey(upr => upr.Id);

        builder.Property(upr => upr.UserId)
            .IsRequired();

        builder.Property(upr => upr.PlatformRoleId)
            .IsRequired();

        builder.Property(upr => upr.ScopeId);

        builder.Property(upr => upr.ScopeType)
            .HasMaxLength(50);

        builder.Property(upr => upr.AssignedBy)
            .IsRequired();

        builder.Property(upr => upr.AssignedAt)
            .IsRequired();

        builder.Property(upr => upr.ExpiresAt);

        builder.Property(upr => upr.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(upr => upr.UserId)
            .HasDatabaseName("ix_user_platform_roles_user_id");

        builder.HasIndex(upr => upr.PlatformRoleId)
            .HasDatabaseName("ix_user_platform_roles_role_id");

        builder.HasIndex(upr => new { upr.ScopeType, upr.ScopeId })
            .HasDatabaseName("ix_user_platform_roles_scope");

        builder.HasIndex(upr => new { upr.UserId, upr.PlatformRoleId, upr.ScopeType, upr.ScopeId })
            .IsUnique()
            .HasDatabaseName("ix_user_platform_roles_unique");

        builder.HasOne(upr => upr.User)
            .WithMany(u => u.UserPlatformRoles)
            .HasForeignKey(upr => upr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(upr => upr.PlatformRole)
            .WithMany(r => r.UserPlatformRoles)
            .HasForeignKey(upr => upr.PlatformRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filter for soft-delete
        builder.HasQueryFilter(upr => upr.IsActive);
    }
}