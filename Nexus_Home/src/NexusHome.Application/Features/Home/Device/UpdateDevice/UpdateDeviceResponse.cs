namespace NexusHome.Application.Features.Home.Device.UpdateDevice;

/// <summary>
/// Response for UpdateDeviceCommand.
/// </summary>
public sealed record UpdateDeviceResponse(AddDevice.DeviceResponse Device);