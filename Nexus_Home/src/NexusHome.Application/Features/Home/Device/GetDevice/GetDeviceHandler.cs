namespace NexusHome.Application.Features.Home.Device.GetDevice;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;
using static NexusHome.Application.Common.Errors.DeviceErrors;

/// <summary>
/// Handler for GetDeviceQuery.
/// </summary>
public sealed class GetDeviceHandler : IRequestHandler<GetDeviceQuery, Result<GetDeviceResponse>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public GetDeviceHandler(
        IDeviceRepository deviceRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _deviceRepository = deviceRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<GetDeviceResponse>> Handle(GetDeviceQuery request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (member, provisioner, or PlatformAdmin/SuperAdmin)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<GetDeviceResponse>.Failure(homeResult.Error);
        }

        // Get device
        var device = await _deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken);
        if (device == null || device.HomeId != request.HomeId)
        {
            return DeviceNotFound;
        }

        if (device.IsRemoved)
        {
            return DeviceRemoved;
        }

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

        return Result<GetDeviceResponse>.Success(new GetDeviceResponse(response));
    }
}