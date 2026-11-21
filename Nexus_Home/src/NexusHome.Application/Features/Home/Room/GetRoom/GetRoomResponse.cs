namespace NexusHome.Application.Features.Home.Room.GetRoom;

/// <summary>
/// Response for GetRoomQuery.
/// </summary>
public sealed record GetRoomResponse(
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