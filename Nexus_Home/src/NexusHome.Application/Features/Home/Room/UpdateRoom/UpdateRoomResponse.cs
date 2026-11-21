namespace NexusHome.Application.Features.Home.Room.UpdateRoom;

/// <summary>
/// Response for UpdateRoomCommand.
/// </summary>
public sealed record UpdateRoomResponse(
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