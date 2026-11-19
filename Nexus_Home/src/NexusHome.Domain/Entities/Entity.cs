namespace NexusHome.Domain.Entities;

/// <summary>
/// Base entity with common properties.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Creation timestamp (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last update timestamp (UTC).
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Soft delete timestamp (UTC). Null if not deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Whether the entity is soft-deleted.
    /// </summary>
    public bool IsDeleted => DeletedAt.HasValue;

    /// <summary>
    /// Marks the entity as updated.
    /// </summary>
    protected void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    /// <summary>
    /// Soft deletes the entity.
    /// </summary>
    public virtual void Delete() => DeletedAt = DateTimeOffset.UtcNow;

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    public virtual void Restore() => DeletedAt = null;

    protected Entity() { }

    protected Entity(Guid id)
    {
        Id = id;
    }
}