namespace NexusHome.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure.Configurations;
using NexusHome.Infrastructure.Entities;

/// <summary>
/// Main DbContext for Nexus Home.
/// Per v1.3 baseline Section 6.2.
/// </summary>
public class NexusHomeDbContext : DbContext
{
    public NexusHomeDbContext(DbContextOptions<NexusHomeDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Suppress warning 10625 about DeviceId1/DeviceId2 shadow properties created by convention
        // when AIActionAudit has both DeviceId (direct FK) and CommandId (FK to DeviceCommand which has Device navigation)
        // This is a known EF Core behavior - the shadow property is not persisted to database
        optionsBuilder.ConfigureWarnings(w => w.Ignore(new EventId(10625, "ShadowPropertyCreatedForRelationship")));
    }

    // Domain entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Home> Homes => Set<Home>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceTelemetry> DeviceTelemetry => Set<DeviceTelemetry>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Settings> Settings => Set<Settings>();
    public DbSet<AIActionAudit> AIActions => Set<AIActionAudit>();
    public DbSet<DeviceCommand> DeviceCommands => Set<DeviceCommand>();

    // Infrastructure entities
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<TelemetryDedupLedger> TelemetryDedupLedger => Set<TelemetryDedupLedger>();

    // Domain entities - Authentication
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new HomeConfiguration());
        modelBuilder.ApplyConfiguration(new MembershipConfiguration());
        modelBuilder.ApplyConfiguration(new RoomConfiguration());
        modelBuilder.ApplyConfiguration(new DeviceConfiguration());
        modelBuilder.ApplyConfiguration(new DeviceTelemetryConfiguration());
        modelBuilder.ApplyConfiguration(new InvitationConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new SettingsConfiguration());
        modelBuilder.ApplyConfiguration(new AIActionAuditConfiguration());
        modelBuilder.ApplyConfiguration(new DeviceCommandConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxEventConfiguration());
        modelBuilder.ApplyConfiguration(new TelemetryDedupLedgerConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());

        // Global query filters for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
        modelBuilder.Entity<Home>().HasQueryFilter(h => h.DeletedAt == null);
        modelBuilder.Entity<Room>().HasQueryFilter(r => r.DeletedAt == null);
        modelBuilder.Entity<Device>().HasQueryFilter(d => d.RemovedAt == null);

        // Matching query filters for dependent entities to avoid EF Core warnings
        modelBuilder.Entity<DeviceTelemetry>().HasQueryFilter(t => t.Device != null && t.Device.RemovedAt == null);
        modelBuilder.Entity<DeviceCommand>().HasQueryFilter(c => c.Device != null && c.Device.RemovedAt == null);
        modelBuilder.Entity<Invitation>().HasQueryFilter(i => i.Home != null && i.Home.DeletedAt == null);
        modelBuilder.Entity<Membership>().HasQueryFilter(m => m.Home != null && m.Home.DeletedAt == null);
        modelBuilder.Entity<Permission>().HasQueryFilter(p => p.Home != null && p.Home.DeletedAt == null);
        modelBuilder.Entity<Settings>().HasQueryFilter(s => s.User != null && s.User.DeletedAt == null);

        // RefreshToken: filter to valid tokens only (not revoked, not expired)
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(rt => rt.RevokedAt == null && rt.ExpiresAt > DateTimeOffset.UtcNow);

        // TimescaleDB hypertable setup - will be executed via migration
        // SELECT create_hypertable('device_telemetry', 'timestamp', chunk_time_interval => INTERVAL '1 day');
        // SELECT add_compression_policy('device_telemetry', INTERVAL '7 days');
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is User u)
            {
                u.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    u.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Home h)
            {
                h.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    h.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Membership m)
            {
                m.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    m.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Room r)
            {
                r.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    r.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Device d)
            {
                d.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    d.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Invitation i)
            {
                i.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    i.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Permission p)
            {
                p.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    p.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Settings s)
            {
                s.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    s.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is AIActionAudit a)
            {
                // AIActionAudit is immutable - no UpdatedAt
                if (entry.State == EntityState.Added)
                    a.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is DeviceCommand dc)
            {
                dc.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    dc.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is OutboxEvent o)
            {
                // OutboxEvent is immutable - no UpdatedAt
                if (entry.State == EntityState.Added)
                    o.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is TelemetryDedupLedger t)
            {
                // TelemetryDedupLedger is immutable - no UpdatedAt
                if (entry.State == EntityState.Added)
                    t.ReceivedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is RefreshToken rt)
            {
                rt.UpdatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    rt.CreatedAt = DateTime.UtcNow;
            }
        }
    }
}