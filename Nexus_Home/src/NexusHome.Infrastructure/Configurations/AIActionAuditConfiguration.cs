namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;

/// <summary>
/// AIActionAudit entity configuration.
/// Per v1.3 baseline Section 6.2.10.
/// </summary>
public class AIActionAuditConfiguration : IEntityTypeConfiguration<AIActionAudit>
{
    public void Configure(EntityTypeBuilder<AIActionAudit> builder)
    {
        builder.ToTable("ai_action_audit");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.UserId)
            .IsRequired(false);

        builder.Property(a => a.DeviceId)
            .IsRequired(false)
            .HasColumnName("device_id");

        builder.Property(a => a.CorrelationId)
            .IsRequired(false)
            .HasColumnName("correlation_id");

        builder.Property(a => a.CommandId)
            .IsRequired(false)
            .HasColumnName("command_id");

        builder.Property(a => a.ActionType)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("action_type");

        builder.Property(a => a.ActionParameters)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'")
            .HasColumnName("action_parameters");

        builder.Property(a => a.Outcome)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("outcome");

        builder.Property(a => a.Timestamp)
            .IsRequired()
            .HasColumnName("timestamp");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        // FK to users - SET NULL on delete
        builder.HasOne(a => a.User)
            .WithMany(u => u.AIActionAudits)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // FK to devices - RESTRICT on delete
        builder.HasOne(a => a.Device)
            .WithMany()
            .HasForeignKey(a => a.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("idx_ai_action_audit_user_id");

        builder.HasIndex(a => a.DeviceId)
            .HasDatabaseName("idx_ai_action_audit_device_id");

        builder.HasIndex(a => a.ActionType)
            .HasDatabaseName("idx_ai_action_audit_action_type");

        builder.HasIndex(a => a.Outcome)
            .HasDatabaseName("idx_ai_action_audit_outcome");

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("idx_ai_action_audit_timestamp");
    }
}