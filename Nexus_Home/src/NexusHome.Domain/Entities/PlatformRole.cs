namespace NexusHome.Domain.Entities;

/// <summary>
/// Platform-level role definition with hierarchy.
/// </summary>
public sealed class PlatformRole : Entity
{
    /// <summary>
    /// Unique role key (e.g., "SuperAdmin", "PlatformAdmin", "Technician", "SupportAgent").
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Display name for the role.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Description of role responsibilities.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Hierarchy level for authorization comparison (higher = more privilege).
    /// SuperAdmin: 100, PlatformAdmin: 80, Technician: 50, SupportAgent: 30.
    /// </summary>
    public int HierarchyLevel { get; private set; }

    /// <summary>
    /// Whether this is a system role (cannot be deleted).
    /// </summary>
    public bool IsSystemRole { get; private set; }

    /// <summary>
    /// Navigation property for user role assignments.
    /// </summary>
    public ICollection<UserPlatformRole> UserPlatformRoles { get; private set; } = new List<UserPlatformRole>();

    private PlatformRole() { } // EF Core

    /// <summary>
    /// Creates a new platform role.
    /// </summary>
    public static PlatformRole Create(string key, string name, string description, int hierarchyLevel, bool isSystemRole = false)
    {
        var role = new PlatformRole
        {
            Key = key,
            Name = name,
            Description = description,
            HierarchyLevel = hierarchyLevel,
            IsSystemRole = isSystemRole
        };
        return role;
    }

    /// <summary>
    /// Creates a new platform role with a fixed ID (for seed data).
    /// </summary>
    public static PlatformRole CreateWithId(Guid id, string key, string name, string description, int hierarchyLevel, bool isSystemRole = false, DateTimeOffset? createdAt = null, DateTimeOffset? updatedAt = null)
    {
        var role = new PlatformRole
        {
            Id = id,
            Key = key,
            Name = name,
            Description = description,
            HierarchyLevel = hierarchyLevel,
            IsSystemRole = isSystemRole,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow
        };
        return role;
    }

    /// <summary>
    /// Updates the role details.
    /// </summary>
    public void Update(string name, string description, int hierarchyLevel)
    {
        Name = name;
        Description = description;
        HierarchyLevel = hierarchyLevel;
        Touch();
    }
}