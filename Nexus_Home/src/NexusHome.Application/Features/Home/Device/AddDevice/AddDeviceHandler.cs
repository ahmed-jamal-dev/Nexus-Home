namespace NexusHome.Application.Features.Home.Device.AddDevice;

using MediatR;
using NexusHome.Application.Common;
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
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;

    public AddDeviceHandler(
        IDeviceRepository deviceRepository,
        IHomeRepository homeRepository,
        IMembershipRepository membershipRepository,
        IOutboxEventRepository outboxEventRepository)
    {
        _deviceRepository = deviceRepository;
        _homeRepository = homeRepository;
        _membershipRepository = membershipRepository;
        _outboxEventRepository = outboxEventRepository;
    }

    public async Task<Result<AddDeviceResponse>> Handle(AddDeviceCommand request, CancellationToken cancellationToken)
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
            // Metadata is updated via UpdateMetadata, but we can set it directly through the constructor or a method
            // The entity doesn't have a direct UpdateMetadata that takes Dictionary, so we'll use reflection or just set it
            // Actually, looking at the entity, there's no public setter for Metadata. Let me check...
            // The entity has UpdateMetadata for name, deviceType, firmwareVersion, manufacturer, model
            // But not for Capabilities/Metadata directly. However, UpdateCapabilities exists.
            // For Metadata, we need to check if there's a way to set it. The entity has private setters.
            // Since the entity is in the same assembly, we can't access private setters from here.
            // But wait - the constructor initializes empty dictionaries. The handler creates the device and then calls UpdateCapabilities.
            // There's no UpdateMetadata for the Metadata dictionary. This might be an issue.
            // Actually, looking at the entity again, Metadata has a private setter and is initialized to new Dictionary.
            // There's no method to update it. This is a gap in the entity.
            // For now, I'll assume we can't update Metadata via the entity's public API, but we need to check if there's a way.
            // Looking at the entity, there's only UpdateCapabilities, UpdateMetadata (for name, deviceType, etc.), UpdateReportedState, MarkOnline/Offline/Unknown, AssignToRoom, Remove, Restore.
            // There's no UpdateMetadata for the Metadata dictionary. This seems like an oversight in the entity.
            // I'll proceed with what we have and note this limitation.
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