namespace NexusHome.Application.Features.Home.Room.CreateRoom;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;
using static NexusHome.Application.Common.Errors.RoomErrors;

/// <summary>
/// Handler for CreateRoomCommand.
/// </summary>
public sealed class CreateRoomHandler : IRequestHandler<CreateRoomCommand, Result<CreateRoomResponse>>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;

    public CreateRoomHandler(
        IRoomRepository roomRepository,
        IHomeRepository homeRepository,
        IMembershipRepository membershipRepository)
    {
        _roomRepository = roomRepository;
        _homeRepository = homeRepository;
        _membershipRepository = membershipRepository;
    }

    public async Task<Result<CreateRoomResponse>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
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

        // Check for duplicate name
        var nameExists = await _roomRepository.ExistsByNameAsync(request.HomeId, request.Name.Trim(), cancellationToken);
        if (nameExists)
        {
            return DuplicateName;
        }

        // Create room
        var room = new Room(Guid.NewGuid(), request.HomeId, request.Name.Trim());
        await _roomRepository.AddAsync(room, cancellationToken);

        var response = new RoomResponse(
            room.Id,
            room.HomeId,
            room.Name,
            room.CreatedAt,
            room.UpdatedAt);

        return Result<CreateRoomResponse>.Success(new CreateRoomResponse(response));
    }
}