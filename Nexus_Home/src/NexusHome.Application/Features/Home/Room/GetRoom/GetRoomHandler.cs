namespace NexusHome.Application.Features.Home.Room.GetRoom;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;
using static NexusHome.Application.Common.Errors.RoomErrors;

/// <summary>
/// Handler for GetRoomQuery.
/// </summary>
public sealed class GetRoomHandler : IRequestHandler<GetRoomQuery, Result<GetRoomResponse>>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;

    public GetRoomHandler(
        IRoomRepository roomRepository,
        IHomeRepository homeRepository,
        IMembershipRepository membershipRepository)
    {
        _roomRepository = roomRepository;
        _homeRepository = homeRepository;
        _membershipRepository = membershipRepository;
    }

    public async Task<Result<GetRoomResponse>> Handle(GetRoomQuery request, CancellationToken cancellationToken)
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

        // Get room
        var room = await _roomRepository.GetByIdAsync(request.RoomId, cancellationToken);
        if (room == null || room.HomeId != request.HomeId)
        {
            return RoomNotFound;
        }

        var response = new RoomResponse(
            room.Id,
            room.HomeId,
            room.Name,
            room.CreatedAt,
            room.UpdatedAt);

        return Result<GetRoomResponse>.Success(new GetRoomResponse(response));
    }
}