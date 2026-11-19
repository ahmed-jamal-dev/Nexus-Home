namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure.Entities;

/// <summary>
/// TelemetryDedupLedger entity configuration.
/// Per v1.3 baseline Section 6.2.14.
/// Used for exactly-once telemetry ingestion deduplication.
/// </summary>
public class TelemetryDedupLedgerConfiguration : IEntityTypeConfiguration<TelemetryDedupLedger>
{
    public void Configure(EntityTypeBuilder<TelemetryDedupLedger> builder)
    {
        builder.ToTable("telemetry_dedup_ledger");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.DeviceId)
            .IsRequired()
            .HasColumnName("device_id");

        builder.Property(t => t.MessageId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("message_id");

        builder.Property(t => t.ReceivedAt)
            .HasColumnName("received_at")
            .HasDefaultValueSql("now()");

        builder.Property(t => t.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        // Composite unique index for deduplication
        builder.HasIndex(t => new { t.DeviceId, t.MessageId })
            .IsUnique()
            .HasDatabaseName("uk_telemetry_dedup_device_message");

        // Index for cleanup job
        builder.HasIndex(t => t.ExpiresAt)
            .HasDatabaseName("idx_telemetry_dedup_expires_at");
    }
}