namespace NexusHome.Application.Features.Home.Room.GetRoom;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to retrieve a room by ID.
/// </summary>
public sealed record GetRoomQuery(
    Guid UserId,
    Guid HomeId,
    Guid RoomId) : IRequest<Result<GetRoomResponse>>;