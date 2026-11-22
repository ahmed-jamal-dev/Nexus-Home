namespace NexusHome.Application.Features.Home.Device.ListDevices;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Query to list devices for a home with cursor-based pagination.
/// </summary>
public sealed record ListDevicesQuery(
    Guid UserId,
    Guid HomeId,
    Guid? RoomId = null,
    string? Cursor = null,
    int Limit = 20) : IRequest<Result<ListDevicesResponse>>;