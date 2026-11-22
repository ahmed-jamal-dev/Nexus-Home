namespace NexusHome.Infrastructure.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;

/// <summary>
/// EF Core configuration for PlatformAuditLog entity.
/// </summary>
public sealed class PlatformAuditLogConfiguration : IEntityTypeConfiguration<PlatformAuditLog>
{
    public void Configure(EntityTypeBuilder<PlatformAuditLog> builder)
    {
        builder.ToTable("platform_audit_logs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.ActorUserId)
            .IsRequired();

        builder.Property(log => log.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(log => log.TargetType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(log => log.TargetId)
            .IsRequired();

        builder.Property(log => log.Details)
            .HasMaxLength(4000);

        builder.Property(log => log.IpAddress)
            .HasMaxLength(45);

        builder.Property(log => log.UserAgent)
            .HasMaxLength(500);

        builder.Property(log => log.Success)
            .IsRequired();

        builder.Property(log => log.ErrorMessage)
            .HasMaxLength(1000);

        // PlatformAuditLog is immutable - ignore UpdatedAt and DeletedAt from base Entity
        builder.Ignore(log => log.UpdatedAt);
        builder.Ignore(log => log.DeletedAt);

        builder.HasIndex(log => log.ActorUserId)
            .HasDatabaseName("ix_platform_audit_logs_actor_user_id");

        builder.HasIndex(log => log.TargetType)
            .HasDatabaseName("ix_platform_audit_logs_target_type");

        builder.HasIndex(log => log.TargetId)
            .HasDatabaseName("ix_platform_audit_logs_target_id");

        builder.HasIndex(log => log.CreatedAt)
            .HasDatabaseName("ix_platform_audit_logs_created_at");

        builder.HasIndex(log => new { log.TargetType, log.TargetId })
            .HasDatabaseName("ix_platform_audit_logs_target");

        builder.HasOne(log => log.ActorUser)
            .WithMany()
            .HasForeignKey(log => log.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}