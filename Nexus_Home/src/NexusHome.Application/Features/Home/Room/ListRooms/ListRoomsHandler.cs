namespace NexusHome.Application.Features.Home.Room.ListRooms;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;
using static NexusHome.Application.Common.Errors.RoomErrors;

/// <summary>
/// Handler for ListRoomsQuery.
/// </summary>
public sealed class ListRoomsHandler : IRequestHandler<ListRoomsQuery, Result<ListRoomsResponse>>
{
    private readonly IRoomRepository _roomRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public ListRoomsHandler(
        IRoomRepository roomRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _roomRepository = roomRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<ListRoomsResponse>> Handle(ListRoomsQuery request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (member, provisioner, or PlatformAdmin/SuperAdmin)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<ListRoomsResponse>.Failure(homeResult.Error);
        }

        // Get rooms with pagination (limit + 1 to check for next page)
        var limit = Math.Min(request.Limit, 100);
        var rooms = await _roomRepository.ListAsync(request.HomeId, request.Cursor, limit + 1, cancellationToken);

        var hasMore = rooms.Count > limit;
        if (hasMore)
        {
            rooms = rooms.Take(limit).ToList();
        }

        string? nextCursor = null;
        if (hasMore && rooms.Count > 0)
        {
            nextCursor = rooms.Last().Id.ToString();
        }

        var roomResponses = rooms.Select(r => new RoomResponse(
            r.Id,
            r.HomeId,
            r.Name,
            r.CreatedAt,
            r.UpdatedAt)).ToList();

        return Result<ListRoomsResponse>.Success(new ListRoomsResponse(roomResponses, nextCursor, hasMore));
    }
}