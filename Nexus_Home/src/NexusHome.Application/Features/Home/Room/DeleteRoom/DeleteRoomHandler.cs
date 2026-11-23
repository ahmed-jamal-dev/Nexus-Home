namespace NexusHome.Application.Features.Home.Room.DeleteRoom;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
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
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public DeleteRoomHandler(
        IRoomRepository roomRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _roomRepository = roomRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<DeleteRoomResponse>> Handle(DeleteRoomCommand request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (member, provisioner, or PlatformAdmin/SuperAdmin)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<DeleteRoomResponse>.Failure(homeResult.Error);
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