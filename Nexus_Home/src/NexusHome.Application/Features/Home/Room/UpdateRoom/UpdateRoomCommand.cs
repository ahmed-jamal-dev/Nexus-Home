namespace NexusHome.Application.Features.Home.Room.UpdateRoom;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to update a room name.
/// </summary>
public sealed record UpdateRoomCommand(
    Guid UserId,
    Guid HomeId,
    Guid RoomId,
    string Name) : IRequest<Result<UpdateRoomResponse>>;