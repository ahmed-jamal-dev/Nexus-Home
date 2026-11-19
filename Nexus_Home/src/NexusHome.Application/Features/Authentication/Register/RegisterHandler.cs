namespace NexusHome.Application.Features.Authentication.Register;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for RegisterCommand.
/// </summary>
public sealed class RegisterHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Normalize email
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Check if email already exists
        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return Auth.EmailAlreadyExists;
        }

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create user
        var user = new User(Guid.NewGuid(), normalizedEmail, passwordHash, request.FullName.Trim());

        await _userRepository.AddAsync(user, cancellationToken);

        // Generate tokens
        var tokens = await GenerateTokensAsync(user, cancellationToken);

        return Result<RegisterResponse>.Success(new RegisterResponse(tokens, MapToUserDto(user)));
    }

    private async Task<TokenResponse> GenerateTokensAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
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