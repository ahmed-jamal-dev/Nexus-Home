namespace NexusHome.Application.Features.Home.Device.ListDevices;

/// <summary>
/// Response for ListDevicesQuery.
/// </summary>
public sealed record ListDevicesResponse(
    IReadOnlyList<AddDevice.DeviceResponse> Devices,
    string? NextCursor,
    bool HasMore);