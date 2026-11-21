namespace NexusHome.Application.Features.Home.Room.CreateRoom;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to create a room in a home.
/// </summary>
public sealed record CreateRoomCommand(
    Guid UserId,
    Guid HomeId,
    string Name) : IRequest<Result<CreateRoomResponse>>;