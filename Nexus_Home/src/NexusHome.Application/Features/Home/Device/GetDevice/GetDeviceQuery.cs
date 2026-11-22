namespace NexusHome.Application.Features.Home.Device.GetDevice;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to get a device by ID.
/// </summary>
public sealed record GetDeviceQuery(
    Guid UserId,
    Guid HomeId,
    Guid DeviceId) : IRequest<Result<GetDeviceResponse>>;