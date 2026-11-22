namespace NexusHome.Application.Features.Home.Device.AddDevice;

using MediatR;
using NexusHome.Application.Common;
using System.Collections.Generic;

/// <summary>
/// Command to add a device to a home.
/// </summary>
public sealed record AddDeviceCommand(
    Guid UserId,
    Guid HomeId,
    string ExternalId,
    string Name,
    string DeviceType,
    Dictionary<string, object>? Capabilities = null,
    Dictionary<string, object>? Metadata = null,
    string? FirmwareVersion = null,
    string? Manufacturer = null,
    string? Model = null) : IRequest<Result<AddDeviceResponse>>;