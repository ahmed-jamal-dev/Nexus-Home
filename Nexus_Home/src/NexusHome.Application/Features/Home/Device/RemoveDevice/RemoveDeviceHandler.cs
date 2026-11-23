namespace NexusHome.Application.Features.Home.Device.RemoveDevice;

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
/// Handler for RemoveDeviceCommand.
/// </summary>
public sealed class RemoveDeviceHandler : IRequestHandler<RemoveDeviceCommand, Result<RemoveDeviceResponse>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;
    private readonly IOutboxEventRepository _outboxEventRepository;

    public RemoveDeviceHandler(
        IDeviceRepository deviceRepository,
        HomeAuthorizationService homeAuthorizationService,
        IOutboxEventRepository outboxEventRepository)
    {
        _deviceRepository = deviceRepository;
        _homeAuthorizationService = homeAuthorizationService;
        _outboxEventRepository = outboxEventRepository;
    }

    public async Task<Result<RemoveDeviceResponse>> Handle(RemoveDeviceCommand request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (member, provisioner, or PlatformAdmin/SuperAdmin)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<RemoveDeviceResponse>.Failure(homeResult.Error);
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