namespace NexusHome.Domain.Entities;

/// <summary>
/// Room within a home.
/// Per DATABASE_MVP_SUMMARY.md and v1.3 baseline Section 6.2.5.
/// Soft delete via DeletedAt.
/// Unique room name per home (among non-deleted).
/// </summary>
public class Room : Entity
{
    /// <summary>
    /// Home ID.
    /// </summary>
    public Guid HomeId { get; private set; }

    /// <summary>
    /// Room name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Navigation to home.
    /// </summary>
    public Home? Home { get; private set; }

    /// <summary>
    /// Devices in this room.
    /// </summary>
    public ICollection<Device> Devices { get; private set; } = new List<Device>();

    protected Room() { }

    public Room(Guid id, Guid homeId, string name)
        : base(id)
    {
        HomeId = homeId;
        Name = name;
    }

    /// <summary>
    /// Updates the room name.
    /// </summary>
    public void Rename(string newName)
    {
        Name = newName;
        Touch();
    }
}