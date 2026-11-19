namespace NexusHome.Application.Features.Authentication.RefreshToken;

using NexusHome.Application.Common;

/// <summary>
/// Response for successful token refresh.
/// </summary>
public sealed record RefreshTokenResponse(
    TokenResponse Tokens
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