namespace NexusHome.Application.Features.Home.Room.GetRoom;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
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
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public GetRoomHandler(
        IRoomRepository roomRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _roomRepository = roomRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<GetRoomResponse>> Handle(GetRoomQuery request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (member, provisioner, or PlatformAdmin/SuperAdmin)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<GetRoomResponse>.Failure(homeResult.Error);
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