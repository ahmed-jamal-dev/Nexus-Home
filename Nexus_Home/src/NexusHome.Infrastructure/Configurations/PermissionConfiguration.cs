namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using NexusHome.Infrastructure.Converters;

/// <summary>
/// Permission entity configuration.
/// Per v1.3 baseline Section 6.2.9.
///
/// Enum mapping strategy: Custom ValueConverters for enum <-> PostgreSQL enum label strings.
/// This approach keeps Domain layer free of Npgsql dependency (Clean Architecture).
/// Converters map C# enum members to their PostgreSQL enum labels (e.g., DeviceControl -> "device:control").
/// </summary>
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.GrantorMembershipId)
            .HasColumnName("grantor_membership_id")
            .IsRequired(false);

        builder.Property(p => p.GranteeMembershipId)
            .HasColumnName("grantee_membership_id")
            .IsRequired();

        builder.Property(p => p.PermissionType)
            .IsRequired()
            .HasConversion(new PermissionTypeConverter())
            .HasColumnName("permission_type");

        builder.Property(p => p.TargetType)
            .IsRequired()
            .HasConversion(new TargetTypeConverter())
            .HasColumnName("target_type");

        builder.Property(p => p.TargetId)
            .IsRequired()
            .HasColumnName("target_id");

        builder.Property(p => p.Effect)
            .IsRequired()
            .HasConversion(new EffectConverter())
            .HasMaxLength(10)
            .HasDefaultValue(Effect.Grant)
            .HasColumnName("effect");

        builder.Property(p => p.GrantedAt)
            .HasColumnName("granted_at")
            .HasDefaultValueSql("now()");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // FK to memberships (grantor) - SET NULL on delete
        builder.HasOne(p => p.GrantorMembership)
            .WithMany(m => m.GrantedPermissions)
            .HasForeignKey(p => p.GrantorMembershipId)
            .OnDelete(DeleteBehavior.SetNull);

        // FK to memberships (grantee) - CASCADE on delete
        builder.HasOne(p => p.GranteeMembership)
            .WithMany(m => m.ReceivedPermissions)
            .HasForeignKey(p => p.GranteeMembershipId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK to homes - CASCADE on delete
        builder.HasOne(p => p.Home)
            .WithMany(h => h.Permissions)
            .HasForeignKey(p => p.HomeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Trigger-enforced referential integrity for polymorphic target
        // CHECK constraint: target_type IN ('device', 'home', 'room', 'camera', 'microphone') -- enforced by DB trigger
        // Trigger: BEFORE INSERT OR UPDATE ON permissions
        //   IF target_type = 'device' AND NOT EXISTS (SELECT 1 FROM devices WHERE id = target_id AND home_id = NEW.home_id) THEN RAISE
        //   IF target_type = 'home' AND NOT EXISTS (SELECT 1 FROM homes WHERE id = target_id) AND NEW.home_id = target_id THEN RAISE
        //   IF target_type = 'room' AND NOT EXISTS (SELECT 1 FROM rooms WHERE id = target_id AND home_id = NEW.home_id) THEN RAISE
        //   IF target_type = 'camera' AND NOT EXISTS (SELECT 1 FROM devices WHERE id = target_id AND capabilities->>'camera' = 'true' AND home_id = NEW.home_id) THEN RAISE
        //   IF target_type = 'microphone' AND NOT EXISTS (SELECT 1 FROM devices WHERE id = target_id AND capabilities->>'microphone' = 'true' AND home_id = NEW.home_id) THEN RAISE
        // This will be added via raw SQL in migration

        // Partial unique index: prevent duplicate grant/deny for same grantee+permission+target+effect
        builder.HasIndex(p => new { p.GranteeMembershipId, p.PermissionType, p.TargetType, p.TargetId, p.Effect })
            .IsUnique()
            .HasDatabaseName("uk_permissions_grantee_perm_target_effect");

        // Indexes
        builder.HasIndex(p => p.GrantorMembershipId)
            .HasDatabaseName("idx_permissions_grantor_membership_id");

        builder.HasIndex(p => new { p.TargetType, p.TargetId })
            .HasDatabaseName("idx_permissions_target");

        builder.HasIndex(p => p.PermissionType)
            .HasDatabaseName("idx_permissions_permission_type");
    }
}