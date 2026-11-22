namespace NexusHome.Application.Features.Home.Device.GetDevice;

using MediatR;
using NexusHome.Application.Common;
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
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;

    public GetDeviceHandler(
        IDeviceRepository deviceRepository,
        IHomeRepository homeRepository,
        IMembershipRepository membershipRepository)
    {
        _deviceRepository = deviceRepository;
        _homeRepository = homeRepository;
        _membershipRepository = membershipRepository;
    }

    public async Task<Result<GetDeviceResponse>> Handle(GetDeviceQuery request, CancellationToken cancellationToken)
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