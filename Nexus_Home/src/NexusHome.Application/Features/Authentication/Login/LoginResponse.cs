namespace NexusHome.Application.Features.Authentication.Login;

using NexusHome.Application.Common;

/// <summary>
/// Response for successful user login.
/// </summary>
public sealed record LoginResponse(
    TokenResponse Tokens,
    UserDto User
);

/// <summary>
/// Token response containing access and refresh tokens.
/// </summary>
public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    string TokenType = "Bearer"
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