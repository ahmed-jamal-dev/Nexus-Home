namespace NexusHome.Application.Features.Platform.User.CreateUser;

using MediatR;
using NexusHome.Application.Common;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;

/// <summary>
/// Handler for CreateUserCommand.
/// </summary>
public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<CreateUserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPlatformRoleRepository _platformRoleRepository;
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;
    private readonly IPlatformAuditLogRepository _auditLogRepository;

    public CreateUserHandler(
        IUserRepository userRepository,
        IHomeRepository homeRepository,
        IPasswordHasher passwordHasher,
        IPlatformRoleRepository platformRoleRepository,
        IUserPlatformRoleRepository userPlatformRoleRepository,
        IPlatformAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _homeRepository = homeRepository;
        _passwordHasher = passwordHasher;
        _platformRoleRepository = platformRoleRepository;
        _userPlatformRoleRepository = userPlatformRoleRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<CreateUserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Verify requester has PlatformAdmin or higher
        var hasPlatformAdmin = await _userPlatformRoleRepository.HasPlatformAdminOrHigherAsync(request.RequesterUserId, cancellationToken);
        if (!hasPlatformAdmin)
        {
            return Result<CreateUserResponse>.Failure(Platform.InsufficientPrivilege);
        }

        // Normalize email
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (existingUser != null)
        {
            return Result<CreateUserResponse>.Failure(Platform.UserAlreadyExists);
        }

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create user
        var user = new User(Guid.NewGuid(), normalizedEmail, passwordHash, request.FullName.Trim());
        await _userRepository.AddAsync(user, cancellationToken);

        // Assign roles
        foreach (var roleAssignment in request.RoleAssignments)
        {
            var role = await _platformRoleRepository.GetByKeyAsync(roleAssignment.RoleKey, cancellationToken);
            if (role == null)
            {
                continue; // Skip invalid roles
            }

            // Validate scope for Technician role
            if (role.Key == "Technician")
            {
                if (!roleAssignment.ScopeId.HasValue || string.IsNullOrEmpty(roleAssignment.ScopeType))
                {
                    return Result<CreateUserResponse>.Failure(Platform.ScopeRequired);
                }

                if (roleAssignment.ScopeType != "Home")
                {
                    return Result<CreateUserResponse>.Failure(Platform.InvalidScope);
                }

                // Verify home exists
                var home = await _homeRepository.GetByIdAsync(roleAssignment.ScopeId.Value, cancellationToken);
                if (home == null)
                {
                    return Result<CreateUserResponse>.Failure(Platform.HomeNotFound);
                }
            }

            var userPlatformRole = UserPlatformRole.Create(
                user.Id,
                role.Id,
                request.RequesterUserId,
                roleAssignment.ScopeId,
                roleAssignment.ScopeType);

            await _userPlatformRoleRepository.AddAsync(userPlatformRole, cancellationToken);
        }

        // Audit log
        var auditLog = PlatformAuditLog.Create(
            request.RequesterUserId,
            "UserCreated",
            "User",
            user.Id,
            $"{{\"email\":\"{user.Email}\",\"roles\":[{string.Join(",", request.RoleAssignments.Select(r => $"\"{r.RoleKey}\""))}]}}");

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        return Result<CreateUserResponse>.Success(new CreateUserResponse(
            user.Id,
            user.Email,
            user.FullName));
    }
}