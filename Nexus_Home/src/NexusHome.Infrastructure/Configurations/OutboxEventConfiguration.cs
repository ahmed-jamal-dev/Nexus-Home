namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using NexusHome.Infrastructure.Entities;

/// <summary>
/// OutboxEvent entity configuration.
/// Transactional outbox pattern for PG→Redis/SignalR consistency.
/// Per v1.3 baseline Section 6.2.13.
/// </summary>
public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("outbox_events");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(o => o.EventType)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("event_type");

        builder.Property(o => o.Payload)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("payload");

        builder.Property(o => o.CorrelationId)
            .HasMaxLength(100)
            .HasColumnName("correlation_id");

        builder.Property(o => o.CausationId)
            .HasMaxLength(100)
            .HasColumnName("causation_id");

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(OutboxEventStatus.Pending)
            .HasColumnName("status");

        builder.Property(o => o.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(o => o.LastError)
            .HasMaxLength(1000)
            .HasColumnName("last_error");

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(o => o.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(o => o.NextRetryAt)
            .HasColumnName("next_retry_at");

        // Indexes for outbox processor
        builder.HasIndex(o => o.Status)
            .HasDatabaseName("idx_outbox_events_status");

        builder.HasIndex(o => new { o.Status, o.NextRetryAt })
            .HasDatabaseName("idx_outbox_events_pending_retry")
            .HasFilter("status = 'pending' AND next_retry_at IS NOT NULL");

        builder.HasIndex(o => o.CreatedAt)
            .HasDatabaseName("idx_outbox_events_created_at");

        builder.HasIndex(o => o.CorrelationId)
            .HasDatabaseName("idx_outbox_events_correlation_id");
    }
}