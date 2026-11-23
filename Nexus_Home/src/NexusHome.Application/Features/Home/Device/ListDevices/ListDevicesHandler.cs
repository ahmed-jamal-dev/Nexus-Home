namespace NexusHome.Application.Features.Home.Device.ListDevices;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Common.Authorization;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for ListDevicesQuery.
/// </summary>
public sealed class ListDevicesHandler : IRequestHandler<ListDevicesQuery, Result<ListDevicesResponse>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly HomeAuthorizationService _homeAuthorizationService;

    public ListDevicesHandler(
        IDeviceRepository deviceRepository,
        HomeAuthorizationService homeAuthorizationService)
    {
        _deviceRepository = deviceRepository;
        _homeAuthorizationService = homeAuthorizationService;
    }

    public async Task<Result<ListDevicesResponse>> Handle(ListDevicesQuery request, CancellationToken cancellationToken)
    {
        // Verify home exists and user is authorized (member, provisioner, or PlatformAdmin/SuperAdmin)
        var homeResult = await _homeAuthorizationService.TryAuthorizeHomeAsync(request.HomeId, request.UserId, cancellationToken);
        if (homeResult.IsFailure)
        {
            return Result<ListDevicesResponse>.Failure(homeResult.Error);
        }

        // Get devices with pagination (limit + 1 to check for next page)
        var limit = Math.Min(request.Limit, 100);
        var devices = await _deviceRepository.ListAsync(request.HomeId, request.RoomId, request.Cursor, limit + 1, cancellationToken);

        var hasMore = devices.Count > limit;
        if (hasMore)
        {
            devices = devices.Take(limit).ToList();
        }

        string? nextCursor = null;
        if (hasMore && devices.Count > 0)
        {
            var lastDevice = devices.Last();
            var cursorBytes = System.Text.Encoding.UTF8.GetBytes($"{lastDevice.CreatedAt:O}|{lastDevice.Id}");
            nextCursor = Convert.ToBase64String(cursorBytes);
        }

        var deviceResponses = devices.Select(d => new AddDevice.DeviceResponse(
            d.Id,
            d.HomeId,
            d.RoomId,
            d.ExternalId,
            d.Name,
            d.DeviceType,
            d.Capabilities,
            d.Metadata,
            d.FirmwareVersion,
            d.Manufacturer,
            d.Model,
            d.ConnectionStatus.ToString(),
            d.LastSeenAt,
            d.ReportedState,
            d.ReportedAt,
            d.StateVersion,
            d.CreatedAt,
            d.UpdatedAt,
            d.RemovedAt)).ToList();

        return Result<ListDevicesResponse>.Success(new ListDevicesResponse(deviceResponses, nextCursor, hasMore));
    }
}