namespace NexusHome.Application.Features.Home.Device.AddDevice;

using System.Collections.Generic;

/// <summary>
/// Response for AddDeviceCommand.
/// </summary>
public sealed record AddDeviceResponse(DeviceResponse Device);

/// <summary>
/// Device data transfer object.
/// </summary>
public sealed record DeviceResponse(
    Guid Id,
    Guid HomeId,
    Guid? RoomId,
    string ExternalId,
    string Name,
    string DeviceType,
    Dictionary<string, object> Capabilities,
    Dictionary<string, object> Metadata,
    string? FirmwareVersion,
    string? Manufacturer,
    string? Model,
    string ConnectionStatus,
    DateTimeOffset? LastSeenAt,
    Dictionary<string, object>? ReportedState,
    DateTimeOffset? ReportedAt,
    long StateVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? RemovedAt);