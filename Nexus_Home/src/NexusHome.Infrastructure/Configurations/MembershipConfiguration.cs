namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;

/// <summary>
/// Membership entity configuration.
/// Per v1.3 baseline Section 6.2.4.
/// </summary>
public class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("memberships");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(m => m.UserId)
            .IsRequired();

        builder.Property(m => m.HomeId)
            .IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(m => m.JoinedAt)
            .HasColumnName("joined_at")
            .HasDefaultValueSql("now()");

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(m => m.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // FK to users - CASCADE on delete
        builder.HasOne(m => m.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK to homes - CASCADE on delete
        builder.HasOne(m => m.Home)
            .WithMany(h => h.Memberships)
            .HasForeignKey(m => m.HomeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Partial unique index: at most one ACTIVE owner per home
        builder.HasIndex(m => m.HomeId)
            .IsUnique()
            .HasFilter("\"Role\" = 'owner' AND \"IsActive\" = true")
            .HasDatabaseName("uk_memberships_one_owner_per_home");

        // Indexes
        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("idx_memberships_user_id");

        builder.HasIndex(m => m.HomeId)
            .HasDatabaseName("idx_memberships_home_id");

        builder.HasIndex(m => new { m.UserId, m.HomeId })
            .HasDatabaseName("idx_memberships_user_home_active")
            .HasFilter("\"IsActive\" = true");
    }
}