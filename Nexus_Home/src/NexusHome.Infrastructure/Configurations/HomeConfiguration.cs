namespace NexusHome.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusHome.Domain.Entities;

/// <summary>
/// Home entity configuration.
/// Per v1.3 baseline Section 6.2.3.
/// </summary>
public class HomeConfiguration : IEntityTypeConfiguration<Home>
{
    public void Configure(EntityTypeBuilder<Home> builder)
    {
        builder.ToTable("homes");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(h => h.AddressLine1)
            .HasMaxLength(255);

        builder.Property(h => h.AddressLine2)
            .HasMaxLength(255);

        builder.Property(h => h.City)
            .HasMaxLength(100);

        builder.Property(h => h.StateProvince)
            .HasMaxLength(100);

        builder.Property(h => h.PostalCode)
            .HasMaxLength(20);

        builder.Property(h => h.Timezone)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("UTC");

        builder.Property(h => h.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(h => h.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        // Index for deleted_at queries
        builder.HasIndex(h => h.DeletedAt)
            .HasDatabaseName("idx_homes_deleted_at")
            .HasFilter("deleted_at IS NOT NULL");
    }
}