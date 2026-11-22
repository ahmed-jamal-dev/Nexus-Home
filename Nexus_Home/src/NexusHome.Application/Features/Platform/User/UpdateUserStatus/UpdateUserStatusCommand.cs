namespace NexusHome.Application.Features.Platform.User.UpdateUserStatus;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to update user active status.
/// </summary>
public sealed record UpdateUserStatusCommand(
    Guid RequesterUserId,
    Guid TargetUserId,
    bool IsActive) : IRequest<Result<UpdateUserStatusResponse>>;

/// <summary>
/// Response for UpdateUserStatusCommand.
/// </summary>
public sealed record UpdateUserStatusResponse(
    Guid UserId,
    bool IsActive);