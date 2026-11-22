namespace NexusHome.Application.Features.Home.Device.AssignDeviceToRoom;

/// <summary>
/// Response for AssignDeviceToRoomCommand.
/// </summary>
public sealed record AssignDeviceToRoomResponse(AddDevice.DeviceResponse Device);