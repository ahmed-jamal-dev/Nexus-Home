namespace NexusHome.Application.Features.Home.Room.DeleteRoom;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;
using static NexusHome.Application.Common.Errors.RoomErrors;

/// <summary>
/// Handler for DeleteRoomCommand.
/// </summary>
public sealed class DeleteRoomHandler : IRequestHandler<DeleteRoomCommand, Result<DeleteRoomResponse>>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;

    public DeleteRoomHandler(
        IRoomRepository roomRepository,
        IHomeRepository homeRepository,
        IMembershipRepository membershipRepository)
    {
        _roomRepository = roomRepository;
        _homeRepository = homeRepository;
        _membershipRepository = membershipRepository;
    }

    public async Task<Result<DeleteRoomResponse>> Handle(DeleteRoomCommand request, CancellationToken cancellationToken)
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

        // Soft delete room
        await _roomRepository.SoftDeleteAsync(request.RoomId, cancellationToken);

        return Result<DeleteRoomResponse>.Success(new DeleteRoomResponse(true));
    }
}