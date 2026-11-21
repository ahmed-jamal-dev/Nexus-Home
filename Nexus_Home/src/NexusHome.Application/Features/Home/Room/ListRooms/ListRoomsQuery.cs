namespace NexusHome.Application.Features.Home.Room.ListRooms;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to list rooms for a home with cursor-based pagination.
/// </summary>
public sealed record ListRoomsQuery(
    Guid UserId,
    Guid HomeId,
    string? Cursor,
    int Limit = 20) : IRequest<Result<ListRoomsResponse>>;