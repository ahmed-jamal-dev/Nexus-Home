namespace NexusHome.Application.Features.Home.Room.UpdateRoom;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
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
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public UpdateRoomHandler(
        IRoomRepository roomRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _roomRepository = roomRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<UpdateRoomResponse>> Handle(UpdateRoomCommand request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (member, provisioner, or PlatformAdmin/SuperAdmin)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<UpdateRoomResponse>.Failure(homeResult.Error);
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