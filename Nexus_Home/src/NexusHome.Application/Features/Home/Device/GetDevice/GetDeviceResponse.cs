namespace NexusHome.Application.Features.Home.Device.GetDevice;

/// <summary>
/// Response for GetDeviceQuery.
/// </summary>
public sealed record GetDeviceResponse(AddDevice.DeviceResponse Device);