namespace NexusHome.Application.Features.Home.Room.DeleteRoom;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to soft delete a room.
/// </summary>
public sealed record DeleteRoomCommand(
    Guid UserId,
    Guid HomeId,
    Guid RoomId) : IRequest<Result<DeleteRoomResponse>>;