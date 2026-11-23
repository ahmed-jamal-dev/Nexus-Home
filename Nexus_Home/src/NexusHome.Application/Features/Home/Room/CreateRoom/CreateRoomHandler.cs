namespace NexusHome.Application.Features.Home.Room.CreateRoom;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
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
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public CreateRoomHandler(
        IRoomRepository roomRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _roomRepository = roomRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<CreateRoomResponse>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (member, provisioner, or PlatformAdmin/SuperAdmin)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<CreateRoomResponse>.Failure(homeResult.Error);
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