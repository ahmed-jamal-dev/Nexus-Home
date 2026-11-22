namespace NexusHome.Domain.Entities;

using NexusHome.Domain.Enums;

/// <summary>
/// Device registry entry.
/// Per DATABASE_MVP_SUMMARY.md and v1.3 baseline Section 6.2.6.
/// - Required home, optional room.
/// - Composite FK devices(home_id, room_id) -> rooms(home_id, id) MATCH SIMPLE.
/// - Globally unique external_id.
/// - Soft delete via RemovedAt (not DeletedAt).
/// - RESTRICT on home->devices, device->telemetry/audit/commands.
/// - Durable reported state: reported_state (JSONB), reported_at, state_version.
/// </summary>
public class Device : Entity
{
    /// <summary>
    /// Home ID (required).
    /// </summary>
    public Guid HomeId { get; private set; }

    /// <summary>
    /// Room ID (optional - device may have no room).
    /// </summary>
    public Guid? RoomId { get; private set; }

    /// <summary>
    /// Globally unique device identity (e.g., MAC address, serial number).
    /// </summary>
    public string ExternalId { get; private set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Device type (e.g., "climate", "light", "sensor").
    /// </summary>
    public string DeviceType { get; private set; } = string.Empty;

    /// <summary>
    /// Structured capability flags (JSONB).
    /// </summary>
    public Dictionary<string, object> Capabilities { get; private set; } = new();

    /// <summary>
    /// Additional metadata (JSONB).
    /// </summary>
    public Dictionary<string, object> Metadata { get; private set; } = new();

    /// <summary>
    /// Firmware version.
    /// </summary>
    public string? FirmwareVersion { get; private set; }

    /// <summary>
    /// Manufacturer.
    /// </summary>
    public string? Manufacturer { get; private set; }

    /// <summary>
    /// Model.
    /// </summary>
    public string? Model { get; private set; }

    /// <summary>
    /// Connection status.
    /// </summary>
    public ConnectionStatus ConnectionStatus { get; private set; } = ConnectionStatus.Unknown;

    /// <summary>
    /// Last seen timestamp.
    /// </summary>
    public DateTimeOffset? LastSeenAt { get; private set; }

    /// <summary>
    /// Last trusted device-reported state (JSONB).
    /// Only updated from device state messages or command results.
    /// </summary>
    public Dictionary<string, object>? ReportedState { get; private set; }

    /// <summary>
    /// When the reported state was received.
    /// </summary>
    public DateTimeOffset? ReportedAt { get; private set; }

    /// <summary>
    /// Optimistic concurrency version for state updates.
    /// </summary>
    public long StateVersion { get; private set; } = 0;

    /// <summary>
    /// Soft removal timestamp.
    /// </summary>
    public DateTimeOffset? RemovedAt { get; private set; }

    /// <summary>
    /// Whether the device is soft-removed.
    /// </summary>
    public bool IsRemoved => RemovedAt.HasValue;

    /// <summary>
    /// Navigation to home.
    /// </summary>
    public Home? Home { get; private set; }

    /// <summary>
    /// Navigation to room.
    /// </summary>
    public Room? Room { get; private set; }

    /// <summary>
    /// Telemetry readings.
    /// </summary>
    public ICollection<DeviceTelemetry> Telemetry { get; private set; } = new List<DeviceTelemetry>();

    /// <summary>
    /// Device commands.
    /// </summary>
    public ICollection<DeviceCommand> Commands { get; private set; } = new List<DeviceCommand>();

    /// <summary>
    /// AI action audits for this device.
    /// </summary>
    public ICollection<AIActionAudit> AIActionAudits { get; private set; } = new List<AIActionAudit>();

    protected Device() { }

    public Device(Guid id, Guid homeId, string externalId, string name, string deviceType)
        : base(id)
    {
        HomeId = homeId;
        ExternalId = externalId;
        Name = name;
        DeviceType = deviceType;
    }

    /// <summary>
    /// Assigns the device to a room.
    /// The room must belong to the same home (enforced by composite FK).
    /// </summary>
    public void AssignToRoom(Guid? roomId)
    {
        RoomId = roomId;
        Touch();
    }

    /// <summary>
    /// Updates device metadata.
    /// </summary>
    public void UpdateMetadata(string? name, string? deviceType, string? firmwareVersion,
        string? manufacturer, string? model)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name;
        if (!string.IsNullOrWhiteSpace(deviceType)) DeviceType = deviceType;
        if (firmwareVersion != null) FirmwareVersion = firmwareVersion;
        if (manufacturer != null) Manufacturer = manufacturer;
        if (model != null) Model = model;
        Touch();
    }

    /// <summary>
    /// Updates capabilities (JSONB).
    /// </summary>
    public void UpdateCapabilities(Dictionary<string, object> capabilities)
    {
        Capabilities = capabilities ?? new Dictionary<string, object>();
        Touch();
    }

    /// <summary>
    /// Updates metadata (JSONB).
    /// </summary>
    public void UpdateMetadata(Dictionary<string, object> metadata)
    {
        Metadata = metadata ?? new Dictionary<string, object>();
        Touch();
    }

    /// <summary>
    /// Updates the reported state from a trusted device message.
    /// Only trusted device reports update these fields; requested command values do not.
    /// </summary>
    public void UpdateReportedState(Dictionary<string, object> reportedState, DateTimeOffset reportedAt)
    {
        ReportedState = reportedState;
        ReportedAt = reportedAt;
        StateVersion++;
        ConnectionStatus = ConnectionStatus.Online;
        LastSeenAt = reportedAt;
        Touch();
    }

    /// <summary>
    /// Updates connection status to online.
    /// </summary>
    public void MarkOnline(DateTimeOffset? lastSeenAt = null)
    {
        ConnectionStatus = ConnectionStatus.Online;
        LastSeenAt = lastSeenAt ?? DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Updates connection status to offline.
    /// </summary>
    public void MarkOffline()
    {
        ConnectionStatus = ConnectionStatus.Offline;
        Touch();
    }

    /// <summary>
    /// Marks connection status as unknown (backend-derived after missed heartbeats).
    /// </summary>
    public void MarkUnknown()
    {
        ConnectionStatus = ConnectionStatus.Unknown;
        Touch();
    }

    /// <summary>
    /// Soft removes the device.
    /// </summary>
    public void Remove()
    {
        RemovedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Restores a soft-removed device.
    /// </summary>
    public override void Restore()
    {
        RemovedAt = null;
        Touch();
    }
}