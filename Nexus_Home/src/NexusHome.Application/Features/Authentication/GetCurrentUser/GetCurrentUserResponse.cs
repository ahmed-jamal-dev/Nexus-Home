namespace NexusHome.Application.Features.Authentication.GetCurrentUser;

using NexusHome.Application.Common;

/// <summary>
/// Response for current user query.
/// </summary>
public sealed record GetCurrentUserResponse(
    UserDto User
);

/// <summary>
/// User data returned in authentication responses.
/// </summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    bool EmailVerified,
    bool IsActive
);