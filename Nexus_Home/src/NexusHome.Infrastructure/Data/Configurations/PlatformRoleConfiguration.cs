namespace NexusHome.Infrastructure.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;

/// <summary>
/// EF Core configuration for PlatformRole entity.
/// </summary>
public sealed class PlatformRoleConfiguration : IEntityTypeConfiguration<PlatformRole>
{
    // Fixed seed timestamp for reproducible migrations
    private static readonly DateTimeOffset SeedTimestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<PlatformRole> builder)
    {
        builder.ToTable("platform_roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Key)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.HierarchyLevel)
            .IsRequired();

        builder.Property(r => r.IsSystemRole)
            .IsRequired();

        builder.HasIndex(r => r.Key)
            .IsUnique()
            .HasDatabaseName("ix_platform_roles_key");

        // Seed system roles with fixed IDs (matching design doc)
        // Using CreateWithId to set fixed IDs and timestamps
        // SupportAgent deferred to Phase 1.5 - excluded from MVP seed
        builder.HasData(
            PlatformRole.CreateWithId(
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                "SuperAdmin",
                "Super Administrator",
                "Full platform access including role management and user administration",
                100,
                true,
                SeedTimestamp,
                SeedTimestamp),
            PlatformRole.CreateWithId(
                Guid.Parse("00000000-0000-0000-0000-000000000002"),
                "PlatformAdmin",
                "Platform Administrator",
                "Platform administration including user management and home oversight",
                80,
                true,
                SeedTimestamp,
                SeedTimestamp),
            PlatformRole.CreateWithId(
                Guid.Parse("00000000-0000-0000-0000-000000000003"),
                "Technician",
                "Technician",
                "Provisions and manages assigned homes only",
                50,
                true,
                SeedTimestamp,
                SeedTimestamp)
            // SupportAgent (ID: 00000000-0000-0000-0000-000000000004) deferred to Phase 1.5
        );
    }
}