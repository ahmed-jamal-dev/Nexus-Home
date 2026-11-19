namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;

/// <summary>
/// Settings entity configuration.
/// Per-user settings (not per-home - O-03 unresolved).
/// Per v1.3 baseline Section 6.2.8.
/// </summary>
public class SettingsConfiguration : IEntityTypeConfiguration<Settings>
{
    public void Configure(EntityTypeBuilder<Settings> builder)
    {
        builder.ToTable("settings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(s => s.AvatarUrl)
            .HasMaxLength(500)
            .HasColumnName("avatar_url");

        builder.Property(s => s.NotificationEnabled)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("notification_enabled");

        builder.Property(s => s.Preferences)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'")
            .HasColumnName("preferences");

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // FK to users - CASCADE on delete
        builder.HasOne(s => s.User)
            .WithOne(u => u.Settings)
            .HasForeignKey<Settings>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One settings row per user
        builder.HasIndex(s => s.UserId)
            .IsUnique()
            .HasDatabaseName("uk_settings_user_id");
    }
}