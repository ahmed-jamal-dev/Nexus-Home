namespace NexusHome.Application.Features.Home.Device.AssignDeviceToRoom;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Events;
using System.Text.Json;
using static NexusHome.Application.Common.Errors;
using static NexusHome.Application.Common.Errors.DeviceErrors;

/// <summary>
/// Handler for AssignDeviceToRoomCommand.
/// </summary>
public sealed class AssignDeviceToRoomHandler : IRequestHandler<AssignDeviceToRoomCommand, Result<AssignDeviceToRoomResponse>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;
    private readonly IOutboxEventRepository _outboxEventRepository;

    public AssignDeviceToRoomHandler(
        IDeviceRepository deviceRepository,
        IRoomRepository roomRepository,
        HomeAuthorizationService homeAuthorizationService,
        IOutboxEventRepository outboxEventRepository)
    {
        _deviceRepository = deviceRepository;
        _roomRepository = roomRepository;
        _homeAuthorizationService = homeAuthorizationService;
        _outboxEventRepository = outboxEventRepository;
    }

    public async Task<Result<AssignDeviceToRoomResponse>> Handle(AssignDeviceToRoomCommand request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (member, provisioner, or PlatformAdmin/SuperAdmin)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<AssignDeviceToRoomResponse>.Failure(homeResult.Error);
        }

        // Get device with home for authorization check
        var device = await _deviceRepository.GetByIdWithHomeAsync(request.DeviceId, cancellationToken);
        if (device == null || device.HomeId != request.HomeId)
        {
            return DeviceNotFound;
        }

        if (device.IsRemoved)
        {
            return DeviceRemoved;
        }

        var oldRoomId = device.RoomId;

        // If RoomId is provided, validate it
        if (request.RoomId.HasValue)
        {
            var room = await _roomRepository.GetByIdAsync(request.RoomId.Value, cancellationToken);
            if (room == null)
            {
                return InvalidRoomAssignment;
            }

            if (room.HomeId != request.HomeId)
            {
                return InvalidRoomAssignment;
            }

            // Room entity inherits IsDeleted from Entity base class
            if (room.IsDeleted)
            {
                return RoomDeleted;
            }
        }

        // Assign device to room (null unassigns)
        device.AssignToRoom(request.RoomId);
        await _deviceRepository.UpdateAsync(device, cancellationToken);

        // Publish device.room.assigned outbox event
        var deviceRoomAssignedEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "device.room.assigned",
            Payload = JsonSerializer.Serialize(new
            {
                deviceId = device.Id,
                oldRoomId,
                newRoomId = request.RoomId,
                assignedAt = DateTimeOffset.UtcNow
            }),
            CorrelationId = request.HomeId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(deviceRoomAssignedEvent, cancellationToken);

        var response = new AddDevice.DeviceResponse(
            device.Id,
            device.HomeId,
            device.RoomId,
            device.ExternalId,
            device.Name,
            device.DeviceType,
            device.Capabilities,
            device.Metadata,
            device.FirmwareVersion,
            device.Manufacturer,
            device.Model,
            device.ConnectionStatus.ToString(),
            device.LastSeenAt,
            device.ReportedState,
            device.ReportedAt,
            device.StateVersion,
            device.CreatedAt,
            device.UpdatedAt,
            device.RemovedAt);

        return Result<AssignDeviceToRoomResponse>.Success(new AssignDeviceToRoomResponse(response));
    }
}