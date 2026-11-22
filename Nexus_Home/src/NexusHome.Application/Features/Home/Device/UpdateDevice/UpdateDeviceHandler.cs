namespace NexusHome.Application.Features.Home.Device.UpdateDevice;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Events;
using System.Text.Json;
using static NexusHome.Application.Common.Errors;
using static NexusHome.Application.Common.Errors.DeviceErrors;

/// <summary>
/// Handler for UpdateDeviceCommand.
/// </summary>
public sealed class UpdateDeviceHandler : IRequestHandler<UpdateDeviceCommand, Result<UpdateDeviceResponse>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;

    public UpdateDeviceHandler(
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

    public async Task<Result<UpdateDeviceResponse>> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
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

        // Track updated fields for outbox event
        var updatedFields = new List<string>();

        // Update name
        if (!string.IsNullOrWhiteSpace(request.Name) && !device.Name.Equals(request.Name.Trim(), StringComparison.Ordinal))
        {
            device.UpdateMetadata(request.Name.Trim(), null, null, null, null);
            updatedFields.Add("name");
        }

        // Update device type
        if (!string.IsNullOrWhiteSpace(request.DeviceType) && !device.DeviceType.Equals(request.DeviceType.Trim(), StringComparison.Ordinal))
        {
            device.UpdateMetadata(null, request.DeviceType.Trim(), null, null, null);
            updatedFields.Add("deviceType");
        }

        // Update capabilities (full replacement)
        if (request.Capabilities != null)
        {
            device.UpdateCapabilities(request.Capabilities);
            updatedFields.Add("capabilities");
        }

        // Update metadata (full replacement)
        if (request.Metadata != null)
        {
            device.UpdateMetadata(request.Metadata);
            updatedFields.Add("metadata");
        }

        // Update firmware version
        if (request.FirmwareVersion != null && !string.Equals(device.FirmwareVersion, request.FirmwareVersion, StringComparison.Ordinal))
        {
            device.UpdateMetadata(null, null, request.FirmwareVersion, null, null);
            updatedFields.Add("firmwareVersion");
        }

        // Update manufacturer
        if (request.Manufacturer != null && !string.Equals(device.Manufacturer, request.Manufacturer, StringComparison.Ordinal))
        {
            device.UpdateMetadata(null, null, null, request.Manufacturer, null);
            updatedFields.Add("manufacturer");
        }

        // Update model
        if (request.Model != null && !string.Equals(device.Model, request.Model, StringComparison.Ordinal))
        {
            device.UpdateMetadata(null, null, null, null, request.Model);
            updatedFields.Add("model");
        }

        await _deviceRepository.UpdateAsync(device, cancellationToken);

        // Publish device.updated outbox event
        var deviceUpdatedEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "device.updated",
            Payload = JsonSerializer.Serialize(new
            {
                deviceId = device.Id,
                updatedFields,
                updatedAt = DateTimeOffset.UtcNow
            }),
            CorrelationId = request.HomeId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(deviceUpdatedEvent, cancellationToken);

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

        return Result<UpdateDeviceResponse>.Success(new UpdateDeviceResponse(response));
    }
}