namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;

/// <summary>
/// DeviceTelemetry entity configuration.
/// TimescaleDB hypertable with composite PK (device_id, timestamp).
/// Per v1.3 baseline Section 6.2.12.
/// </summary>
public class DeviceTelemetryConfiguration : IEntityTypeConfiguration<DeviceTelemetry>
{
    public void Configure(EntityTypeBuilder<DeviceTelemetry> builder)
    {
        builder.ToTable("device_telemetry");

        // Composite primary key for TimescaleDB hypertable (device_id, timestamp)
        builder.HasKey(t => new { t.DeviceId, t.Timestamp });

        builder.Property(t => t.DeviceId)
            .IsRequired()
            .HasColumnName("device_id");

        builder.Property(t => t.Timestamp)
            .IsRequired()
            .HasColumnName("timestamp");

        builder.Property(t => t.MessageId)
            .IsRequired()
            .HasColumnName("message_id");

        builder.Property(t => t.ReadingIndex)
            .IsRequired()
            .HasColumnName("reading_index");

        builder.Property(t => t.MetricName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("metric_name");

        builder.Property(t => t.MetricValue)
            .IsRequired()
            .HasColumnName("metric_value");

        builder.Property(t => t.MetricUnit)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("metric_unit");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        // FK to devices - RESTRICT on delete
        builder.HasOne(t => t.Device)
            .WithMany(d => d.Telemetry)
            .HasForeignKey(t => t.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for time-series queries
        builder.HasIndex(t => new { t.DeviceId, t.Timestamp })
            .HasDatabaseName("idx_device_telemetry_device_time");

        builder.HasIndex(t => t.MetricName)
            .HasDatabaseName("idx_device_telemetry_metric_name");

        builder.HasIndex(t => t.Timestamp)
            .HasDatabaseName("idx_device_telemetry_timestamp");
    }
}