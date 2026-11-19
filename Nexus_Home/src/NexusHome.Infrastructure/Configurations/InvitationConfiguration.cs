namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;

/// <summary>
/// Invitation entity configuration.
/// Per v1.3 baseline Section 6.2.7.
/// </summary>
public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.HomeId)
            .IsRequired();

        builder.Property(i => i.InviteeEmail)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("invitee_email");

        builder.Property(i => i.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.Token)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(i => i.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(i => i.IsAccepted)
            .HasColumnName("is_accepted")
            .HasDefaultValue(false);

        builder.Property(i => i.InvitedByUserId)
            .HasColumnName("invited_by_user_id")
            .IsRequired(false);

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // FK to homes - CASCADE on delete
        builder.HasOne(i => i.Home)
            .WithMany(h => h.Invitations)
            .HasForeignKey(i => i.HomeId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK to users (inviter) - SET NULL on delete
        builder.HasOne(i => i.InvitedByUser)
            .WithMany(u => u.SentInvitations)
            .HasForeignKey(i => i.InvitedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique token
        builder.HasIndex(i => i.Token)
            .IsUnique()
            .HasDatabaseName("uk_invitations_token");

        // Business rule: at most one pending invitation per email per home
        builder.HasIndex(i => new { i.HomeId, i.InviteeEmail })
            .IsUnique()
            .HasFilter("is_accepted = false")
            .HasDatabaseName("uk_invitations_home_email_pending");

        // Indexes
        builder.HasIndex(i => i.HomeId)
            .HasDatabaseName("idx_invitations_home_id");

        builder.HasIndex(i => i.InviteeEmail)
            .HasDatabaseName("idx_invitations_invitee_email");

        builder.HasIndex(i => i.ExpiresAt)
            .HasDatabaseName("idx_invitations_expires_at");
    }
}