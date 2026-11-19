namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;

/// <summary>
/// Room entity configuration.
/// Per v1.3 baseline Section 6.2.5.
/// </summary>
public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.HomeId)
            .IsRequired();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // FK to homes - CASCADE on delete
        builder.HasOne(r => r.Home)
            .WithMany(h => h.Rooms)
            .HasForeignKey(r => r.HomeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Required for composite FK from devices (home_id, room_id) -> rooms (home_id, id)
        builder.HasIndex(r => new { r.HomeId, r.Id })
            .IsUnique()
            .HasDatabaseName("uk_rooms_home_id");

        // Business rule: unique room name per home (among non-deleted)
        builder.HasIndex(r => new { r.HomeId, r.Name })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uk_rooms_home_name");

        // Index
        builder.HasIndex(r => r.HomeId)
            .HasDatabaseName("idx_rooms_home_id");
    }
}