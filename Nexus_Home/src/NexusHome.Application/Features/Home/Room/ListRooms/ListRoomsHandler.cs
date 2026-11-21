namespace NexusHome.Application.Features.Home.Room.ListRooms;

using MediatR;
using NexusHome.Application.Common;
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
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;

    public ListRoomsHandler(
        IRoomRepository roomRepository,
        IHomeRepository homeRepository,
        IMembershipRepository membershipRepository)
    {
        _roomRepository = roomRepository;
        _homeRepository = homeRepository;
        _membershipRepository = membershipRepository;
    }

    public async Task<Result<ListRoomsResponse>> Handle(ListRoomsQuery request, CancellationToken cancellationToken)
    {
        // Verify home exists and is not deleted
        var home = await _homeRepository.GetByIdAsync(request.HomeId, cancellationToken);
        if (home == null)
        {
            return Errors.Home.HomeNotFound;
        }

        // Check membership - must be Owner or Member
        var isMember = await _membershipRepository.IsActiveMemberAsync(request.UserId, request.HomeId, cancellationToken);
        if (!isMember)
        {
            return Errors.Home.HomeNotFound; // Don't leak home existence
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