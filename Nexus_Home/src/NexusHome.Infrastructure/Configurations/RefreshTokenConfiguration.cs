namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;

/// <summary>
/// RefreshToken entity configuration.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(64) // SHA-256 hex = 64 chars
            .HasColumnName("token_hash");

        builder.Property(rt => rt.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired()
            .HasColumnName("expires_at");

        builder.Property(rt => rt.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(rt => rt.ReplacedByTokenHash)
            .HasMaxLength(64)
            .HasColumnName("replaced_by_token_hash");

        builder.Property(rt => rt.RevocationReason)
            .HasMaxLength(255)
            .HasColumnName("revocation_reason");

        builder.Property(rt => rt.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(rt => rt.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // FK to users - CASCADE on delete (refresh tokens deleted when user deleted)
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("idx_refresh_tokens_user_id");

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("uk_refresh_tokens_token_hash");

        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("idx_refresh_tokens_expires_at")
            .HasFilter("revoked_at IS NULL");

        // Partial index for active tokens (not revoked)
        builder.HasIndex(rt => new { rt.UserId, rt.TokenHash })
            .HasDatabaseName("idx_refresh_tokens_user_hash_active")
            .HasFilter("revoked_at IS NULL");
    }
}