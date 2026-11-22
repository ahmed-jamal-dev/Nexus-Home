namespace NexusHome.Application.Features.Home.Device.RemoveDevice;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to soft delete a device.
/// </summary>
public sealed record RemoveDeviceCommand(
    Guid UserId,
    Guid HomeId,
    Guid DeviceId) : IRequest<Result<RemoveDeviceResponse>>;