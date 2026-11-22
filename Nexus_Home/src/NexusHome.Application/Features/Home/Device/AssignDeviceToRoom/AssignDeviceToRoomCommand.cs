namespace NexusHome.Application.Features.Home.Device.AssignDeviceToRoom;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to assign or unassign a device to/from a room.
/// </summary>
public sealed record AssignDeviceToRoomCommand(
    Guid UserId,
    Guid HomeId,
    Guid DeviceId,
    Guid? RoomId) : IRequest<Result<AssignDeviceToRoomResponse>>;