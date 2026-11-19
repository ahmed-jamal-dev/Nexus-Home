namespace NexusHome.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
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

        // Global query filters for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
        modelBuilder.Entity<Home>().HasQueryFilter(h => h.DeletedAt == null);
        modelBuilder.Entity<Room>().HasQueryFilter(r => r.DeletedAt == null);
        modelBuilder.Entity<Device>().HasQueryFilter(d => d.RemovedAt == null);

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
        }
    }
}