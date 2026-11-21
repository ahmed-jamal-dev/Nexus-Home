namespace NexusHome.Application.Features.Home.Room.UpdateRoom;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;
using static NexusHome.Application.Common.Errors.RoomErrors;

/// <summary>
/// Handler for UpdateRoomCommand.
/// </summary>
public sealed class UpdateRoomHandler : IRequestHandler<UpdateRoomCommand, Result<UpdateRoomResponse>>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;

    public UpdateRoomHandler(
        IRoomRepository roomRepository,
        IHomeRepository homeRepository,
        IMembershipRepository membershipRepository)
    {
        _roomRepository = roomRepository;
        _homeRepository = homeRepository;
        _membershipRepository = membershipRepository;
    }

    public async Task<Result<UpdateRoomResponse>> Handle(UpdateRoomCommand request, CancellationToken cancellationToken)
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

        // Check for duplicate name (excluding current room)
        var nameExists = await _roomRepository.ExistsByNameAsync(request.HomeId, request.Name.Trim(), cancellationToken);
        if (nameExists && !room.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return DuplicateName;
        }

        // Update room name
        room.Rename(request.Name.Trim());
        await _roomRepository.UpdateAsync(room, cancellationToken);

        var response = new RoomResponse(
            room.Id,
            room.HomeId,
            room.Name,
            room.CreatedAt,
            room.UpdatedAt);

        return Result<UpdateRoomResponse>.Success(new UpdateRoomResponse(response));
    }
}