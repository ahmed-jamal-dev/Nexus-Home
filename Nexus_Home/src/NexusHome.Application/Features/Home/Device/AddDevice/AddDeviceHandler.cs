namespace NexusHome.Application.Features.Home.Device.AddDevice;

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
/// Handler for AddDeviceCommand.
/// </summary>
public sealed class AddDeviceHandler : IRequestHandler<AddDeviceCommand, Result<AddDeviceResponse>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;
    private readonly IOutboxEventRepository _outboxEventRepository;

    public AddDeviceHandler(
        IDeviceRepository deviceRepository,
        HomeAuthorizationService homeAuthorizationService,
        IOutboxEventRepository outboxEventRepository)
    {
        _deviceRepository = deviceRepository;
        _homeAuthorizationService = homeAuthorizationService;
        _outboxEventRepository = outboxEventRepository;
    }

    public async Task<Result<AddDeviceResponse>> Handle(AddDeviceCommand request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (member, provisioner, or PlatformAdmin/SuperAdmin)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<AddDeviceResponse>.Failure(homeResult.Error);
        }

        // Check for duplicate ExternalId
        var externalIdExists = await _deviceRepository.ExistsByExternalIdAsync(request.ExternalId.Trim(), cancellationToken);
        if (externalIdExists)
        {
            return DuplicateExternalId;
        }

        // Create device
        var device = new Device(
            Guid.NewGuid(),
            request.HomeId,
            request.ExternalId.Trim(),
            request.Name.Trim(),
            request.DeviceType.Trim());

        if (request.Capabilities != null)
        {
            device.UpdateCapabilities(request.Capabilities);
        }

        if (request.Metadata != null)
        {
            device.UpdateMetadata(request.Metadata);
        }

        if (!string.IsNullOrEmpty(request.FirmwareVersion) ||
            !string.IsNullOrEmpty(request.Manufacturer) ||
            !string.IsNullOrEmpty(request.Model))
        {
            device.UpdateMetadata(
                request.Name.Trim(),
                request.DeviceType.Trim(),
                request.FirmwareVersion,
                request.Manufacturer,
                request.Model);
        }

        await _deviceRepository.AddAsync(device, cancellationToken);

        // Publish device.registered outbox event
        var deviceRegisteredEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "device.registered",
            Payload = JsonSerializer.Serialize(new
            {
                homeId = request.HomeId,
                deviceId = device.Id,
                externalId = device.ExternalId,
                deviceType = device.DeviceType,
                roomId = device.RoomId,
                registeredAt = DateTimeOffset.UtcNow
            }),
            CorrelationId = request.HomeId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(deviceRegisteredEvent, cancellationToken);

        var response = new DeviceResponse(
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

        return Result<AddDeviceResponse>.Success(new AddDeviceResponse(response));
    }
}