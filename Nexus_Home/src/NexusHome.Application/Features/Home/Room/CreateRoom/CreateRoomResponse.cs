namespace NexusHome.Application.Features.Home.Room.CreateRoom;

/// <summary>
/// Response for CreateRoomCommand.
/// </summary>
public sealed record CreateRoomResponse(
    RoomResponse Room);

/// <summary>
/// Room data transfer object.
/// </summary>
public sealed record RoomResponse(
    Guid Id,
    Guid HomeId,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);