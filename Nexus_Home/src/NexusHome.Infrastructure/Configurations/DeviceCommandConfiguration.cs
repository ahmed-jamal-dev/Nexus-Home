namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;

/// <summary>
/// DeviceCommand entity configuration.
/// Per v1.3 baseline Section 6.2.9.
/// </summary>
public class DeviceCommandConfiguration : IEntityTypeConfiguration<DeviceCommand>
{
    public void Configure(EntityTypeBuilder<DeviceCommand> builder)
    {
        builder.ToTable("device_commands");

        builder.HasKey(dc => dc.Id);

        builder.Property(dc => dc.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(dc => dc.CommandId)
            .IsRequired()
            .HasColumnName("command_id");

        builder.Property(dc => dc.CorrelationId)
            .IsRequired()
            .HasColumnName("correlation_id");

        builder.Property(dc => dc.HomeId)
            .IsRequired()
            .HasColumnName("home_id");

        builder.Property(dc => dc.DeviceId)
            .IsRequired()
            .HasColumnName("device_id");

        builder.Property(dc => dc.RequestedByUserId)
            .HasColumnName("requested_by_user_id");

        builder.Property(dc => dc.InitiatorType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("initiator_type");

        builder.Property(dc => dc.Type)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("type");

        builder.Property(dc => dc.Parameters)
            .HasColumnType("jsonb")
            .HasColumnName("parameters");

        builder.Property(dc => dc.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(CommandStatus.Pending)
            .HasColumnName("status");

        builder.Property(dc => dc.IdempotencyKey)
            .HasMaxLength(100)
            .HasColumnName("idempotency_key");

        builder.Property(dc => dc.SentAt)
            .HasColumnName("sent_at");

        builder.Property(dc => dc.AcknowledgedAt)
            .HasColumnName("acknowledged_at");

        builder.Property(dc => dc.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(dc => dc.ExpiresAt)
            .IsRequired()
            .HasColumnName("expires_at");

        builder.Property(dc => dc.ReportedState)
            .HasColumnType("jsonb")
            .HasColumnName("reported_state");

        builder.Property(dc => dc.ErrorCode)
            .HasMaxLength(50)
            .HasColumnName("error_code");

        builder.Property(dc => dc.ErrorMessage)
            .HasMaxLength(500)
            .HasColumnName("error_message");

        builder.Property(dc => dc.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(dc => dc.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // FK to homes - RESTRICT on delete
        builder.HasOne(dc => dc.Home)
            .WithMany()
            .HasForeignKey(dc => dc.HomeId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to devices - RESTRICT on delete
        builder.HasOne(dc => dc.Device)
            .WithMany(d => d.Commands)
            .HasForeignKey(dc => dc.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to users (requester) - CASCADE on delete per D-18
        builder.HasOne(dc => dc.RequestedByUser)
            .WithMany(u => u.DeviceCommands)
            .HasForeignKey(dc => dc.RequestedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique command_id per device (for device deduplication)
        builder.HasIndex(dc => new { dc.DeviceId, dc.CommandId })
            .IsUnique()
            .HasDatabaseName("uk_device_commands_device_command_id");

        // Unique idempotency_key per user for non-terminal states
        builder.HasIndex(dc => new { dc.RequestedByUserId, dc.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL AND status NOT IN ('succeeded', 'failed', 'timed_out', 'cancelled')")
            .HasDatabaseName("uk_device_commands_user_idempotency_nonterminal");

        // Indexes
        builder.HasIndex(dc => dc.HomeId)
            .HasDatabaseName("idx_device_commands_home_id");

        builder.HasIndex(dc => dc.DeviceId)
            .HasDatabaseName("idx_device_commands_device_id");

        builder.HasIndex(dc => dc.RequestedByUserId)
            .HasDatabaseName("idx_device_commands_requested_by_user_id");

        builder.HasIndex(dc => dc.Status)
            .HasDatabaseName("idx_device_commands_status");

        builder.HasIndex(dc => dc.CorrelationId)
            .HasDatabaseName("idx_device_commands_correlation_id");

        builder.HasIndex(dc => dc.ExpiresAt)
            .HasDatabaseName("idx_device_commands_expires_at");
    }
}