namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;

/// <summary>
/// Device entity configuration.
/// Per v1.3 baseline Section 6.2.6.
/// </summary>
public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(d => d.HomeId)
            .IsRequired();

        builder.Property(d => d.RoomId)
            .IsRequired(false);

        builder.Property(d => d.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(d => d.DeviceType)
            .IsRequired()
            .HasMaxLength(100);

        // JSONB columns
        builder.Property(d => d.Capabilities)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'");

        builder.Property(d => d.Metadata)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'");

        builder.Property(d => d.FirmwareVersion)
            .HasMaxLength(50);

        builder.Property(d => d.Manufacturer)
            .HasMaxLength(100);

        builder.Property(d => d.Model)
            .HasMaxLength(100);

        builder.Property(d => d.ConnectionStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ConnectionStatus.Unknown)
            .HasSentinel(ConnectionStatus.Online);

        builder.Property(d => d.LastSeenAt)
            .HasColumnName("last_seen_at");

        // Durable reported state
        builder.Property(d => d.ReportedState)
            .HasColumnName("reported_state")
            .HasColumnType("jsonb");

        builder.Property(d => d.ReportedAt)
            .HasColumnName("reported_at");

        builder.Property(d => d.StateVersion)
            .HasColumnName("state_version")
            .HasDefaultValue(0L);

        builder.Property(d => d.RemovedAt)
            .HasColumnName("removed_at");

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // FK to homes - RESTRICT on delete (prevent cascade removal of devices when home deleted)
        builder.HasOne(d => d.Home)
            .WithMany(h => h.Devices)
            .HasForeignKey(d => d.HomeId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to rooms - SET NULL on delete (room deletion clears device.room_id)
        builder.HasOne(d => d.Room)
            .WithMany(r => r.Devices)
            .HasForeignKey(d => d.RoomId)
            .OnDelete(DeleteBehavior.SetNull);

        // Explicitly configure Device -> Commands to prevent convention inference
        // from DeviceCommand.Device navigation, which would create DeviceId1 shadow property on AIActionAudit
        builder.HasMany(d => d.Commands)
            .WithOne(dc => dc.Device)
            .HasForeignKey(dc => dc.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Globally unique external_id
        builder.HasIndex(d => d.ExternalId)
            .IsUnique()
            .HasDatabaseName("uk_devices_external_id");

        // Required for composite FK from devices (home_id, room_id) -> rooms (home_id, id)
        builder.HasIndex(d => new { d.HomeId, d.Id })
            .IsUnique()
            .HasDatabaseName("uk_devices_home_id");

        // Composite FK: device's room must belong to same home (MATCH SIMPLE allows NULL room_id)
        // This is enforced via the unique index on rooms (home_id, id) and the FK above
        // The actual composite FK is: ALTER TABLE devices ADD CONSTRAINT fk_devices_rooms
        //     FOREIGN KEY (home_id, room_id) REFERENCES rooms (home_id, id) MATCH SIMPLE;
        // This will be added via raw SQL in migration

        // Indexes
        builder.HasIndex(d => d.RoomId)
            .HasDatabaseName("idx_devices_room_id");

        builder.HasIndex(d => d.ConnectionStatus)
            .HasDatabaseName("idx_devices_connection_status");

        builder.HasIndex(d => d.RemovedAt)
            .HasDatabaseName("idx_devices_removed_at")
            .HasFilter("removed_at IS NOT NULL");
    }
}