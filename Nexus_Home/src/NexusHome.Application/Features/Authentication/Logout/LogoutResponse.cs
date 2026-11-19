namespace NexusHome.Application.Features.Authentication.Logout;

/// <summary>
/// Response for successful logout.
/// </summary>
public sealed record LogoutResponse(
    bool Success
);