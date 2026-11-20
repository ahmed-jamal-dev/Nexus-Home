namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;

/// <summary>
/// Invitation entity configuration.
/// Per v1.3 baseline Section 6.2.7.
/// Supports both member invitations (InviteeEmail set) and claim credentials (InviteeEmail=null, Role=Owner).
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

        // Nullable for claim credentials
        builder.Property(i => i.InviteeEmail)
            .HasMaxLength(255)
            .HasColumnName("invitee_email")
            .IsRequired(false);

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

        // CHECK constraint: claim credentials must have role='Owner' and no invitee_email
        // Regular invitations have invitee_email set
        // Note: Role column is "Role" (capital R) in the database, enum values are capitalized
        builder.HasCheckConstraint("chk_invitations_claim_credential",
            "(\"Role\" = 'Owner' AND invitee_email IS NULL) OR (invitee_email IS NOT NULL)");

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

        // Unique token (partial: only for non-empty tokens)
        // Note: Token column is "Token" (capital T) in the database
        builder.HasIndex(i => i.Token)
            .IsUnique()
            .HasFilter("\"Token\" <> ''")
            .HasDatabaseName("uk_invitations_token");

        // Business rule: at most one pending invitation per email per home (for member invitations only)
        builder.HasIndex(i => new { i.HomeId, i.InviteeEmail })
            .IsUnique()
            .HasFilter("is_accepted = false AND invitee_email IS NOT NULL")
            .HasDatabaseName("uk_invitations_home_email_pending");

        // Index for claim credential lookup: pending claim credentials by home
        // Using database column names for PostgreSQL partial index filter
        // Note: Role column is "Role" (capital R), enum values are capitalized ('Owner')
        builder.HasIndex(i => i.HomeId)
            .HasFilter("is_accepted = false AND \"Role\" = 'Owner' AND invitee_email IS NULL AND \"DeletedAt\" IS NULL")
            .HasDatabaseName("idx_invitations_home_pending_claim");

        // Indexes
        builder.HasIndex(i => i.InviteeEmail)
            .HasDatabaseName("idx_invitations_invitee_email");

        builder.HasIndex(i => i.ExpiresAt)
            .HasDatabaseName("idx_invitations_expires_at");
    }
}