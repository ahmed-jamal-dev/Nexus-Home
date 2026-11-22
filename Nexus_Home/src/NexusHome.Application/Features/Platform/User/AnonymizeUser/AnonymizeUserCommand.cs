namespace NexusHome.Application.Features.Platform.User.AnonymizeUser;

using MediatR;
using NexusHome.Application.Common;

/// <summary>
/// Command to anonymize a user (GDPR right to erasure).
/// </summary>
public sealed record AnonymizeUserCommand(
    Guid RequesterUserId,
    Guid TargetUserId) : IRequest<Result<AnonymizeUserResponse>>;

/// <summary>
/// Response for user anonymization.
/// </summary>
public sealed record AnonymizeUserResponse(
    Guid UserId,
    string Email,
    bool IsActive);