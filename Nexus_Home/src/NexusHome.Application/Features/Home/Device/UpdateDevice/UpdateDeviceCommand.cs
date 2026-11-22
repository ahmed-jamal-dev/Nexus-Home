namespace NexusHome.Application.Features.Home.Device.UpdateDevice;

using MediatR;
using NexusHome.Application.Common;
using System.Collections.Generic;

/// <summary>
/// Command to update a device.
/// </summary>
public sealed record UpdateDeviceCommand(
    Guid UserId,
    Guid HomeId,
    Guid DeviceId,
    string? Name = null,
    string? DeviceType = null,
    Dictionary<string, object>? Capabilities = null,
    Dictionary<string, object>? Metadata = null,
    string? FirmwareVersion = null,
    string? Manufacturer = null,
    string? Model = null) : IRequest<Result<UpdateDeviceResponse>>;