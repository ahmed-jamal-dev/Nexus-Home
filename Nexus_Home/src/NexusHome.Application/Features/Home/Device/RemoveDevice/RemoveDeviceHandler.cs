namespace NexusHome.Application.Features.Home.Device.RemoveDevice;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Events;
using System.Text.Json;
using static NexusHome.Application.Common.Errors;
using static NexusHome.Application.Common.Errors.DeviceErrors;

/// <summary>
/// Handler for RemoveDeviceCommand.
/// </summary>
public sealed class RemoveDeviceHandler : IRequestHandler<RemoveDeviceCommand, Result<RemoveDeviceResponse>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;

    public RemoveDeviceHandler(
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

    public async Task<Result<RemoveDeviceResponse>> Handle(RemoveDeviceCommand request, CancellationToken cancellationToken)
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

        // Soft delete device
        await _deviceRepository.SoftDeleteAsync(request.DeviceId, cancellationToken);

        // Publish device.removed outbox event
        var deviceRemovedEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "device.removed",
            Payload = JsonSerializer.Serialize(new
            {
                homeId = request.HomeId,
                deviceId = request.DeviceId,
                removedAt = DateTimeOffset.UtcNow
            }),
            CorrelationId = request.HomeId.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _outboxEventRepository.AddAsync(deviceRemovedEvent, cancellationToken);

        return Result<RemoveDeviceResponse>.Success(new RemoveDeviceResponse(true));
    }
}