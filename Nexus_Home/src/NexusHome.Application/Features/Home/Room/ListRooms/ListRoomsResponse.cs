namespace NexusHome.Application.Features.Home.Room.ListRooms;

/// <summary>
/// Response for ListRoomsQuery.
/// </summary>
public sealed record ListRoomsResponse(
    IReadOnlyList<RoomResponse> Rooms,
    string? NextCursor,
    bool HasMore);

/// <summary>
/// Room data transfer object.
/// </summary>
public sealed record RoomResponse(
    Guid Id,
    Guid HomeId,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);