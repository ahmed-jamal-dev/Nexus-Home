namespace NexusHome.Application.Features.Authentication.GetCurrentUser;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for GetCurrentUserQuery.
/// </summary>
public sealed class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, Result<GetCurrentUserResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<GetCurrentUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Auth.UserNotFound;
        }

        if (!user.IsActive)
        {
            return Auth.UserInactive;
        }

        return Result<GetCurrentUserResponse>.Success(new GetCurrentUserResponse(MapToUserDto(user)));
    }

    private static UserDto MapToUserDto(User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.EmailVerified,
        user.IsActive
    );
}