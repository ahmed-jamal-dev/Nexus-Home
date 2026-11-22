namespace NexusHome.Application.Features.Authentication.Login;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for LoginCommand.
/// </summary>
public sealed class LoginHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;

    public LoginHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IUserPlatformRoleRepository userPlatformRoleRepository)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _userPlatformRoleRepository = userPlatformRoleRepository;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Normalize email
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Find user
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user == null)
        {
            // Generic error to prevent email enumeration
            return Auth.InvalidCredentials;
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return Auth.UserInactive;
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Auth.InvalidCredentials;
        }

        // Generate tokens
        var tokens = await GenerateTokensAsync(user, cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(tokens, MapToUserDto(user)));
    }

    private async Task<TokenResponse> GenerateTokensAsync(User user, CancellationToken cancellationToken)
    {
        // Get user's platform roles
        var userPlatformRoles = await _userPlatformRoleRepository.GetByUserIdWithRolesAsync(user.Id, cancellationToken);
        var activeRoles = userPlatformRoles.Where(upr => upr.IsValid()).ToList();

        var roleKeys = activeRoles.Select(upr => upr.PlatformRole.Key).Distinct().ToList();
        var roleScopes = activeRoles
            .Select(upr => (upr.PlatformRole.Key, upr.ScopeId, upr.ScopeType))
            .ToList();

        var accessToken = _tokenService.GenerateAccessToken(user, roleKeys, roleScopes, user.PlatformRolesVersion);
        var (refreshTokenPlain, refreshTokenHash) = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            refreshTokenHash,
            user.Id,
            DateTimeOffset.UtcNow.AddDays(_tokenService.RefreshTokenExpirationDays)
        );

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new TokenResponse(
            accessToken,
            refreshTokenPlain,
            _tokenService.AccessTokenExpirationSeconds
        );
    }

    private static UserDto MapToUserDto(User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.EmailVerified,
        user.IsActive
    );
}