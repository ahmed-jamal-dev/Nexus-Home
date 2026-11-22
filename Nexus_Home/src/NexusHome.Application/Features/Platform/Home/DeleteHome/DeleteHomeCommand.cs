namespace NexusHome.Application.Features.Platform.Home.DeleteHome;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to delete a home (platform admin only).
/// </summary>
public sealed record DeleteHomeCommand(
    Guid RequesterUserId,
    Guid HomeId) : IRequest<Result<DeleteHomeResponse>>;

/// <summary>
/// Response for DeleteHomeCommand.
/// </summary>
public sealed record DeleteHomeResponse(
    Guid HomeId,
    bool Deleted);