namespace NexusHome.Application.Common.Authorization;

using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using static NexusHome.Application.Common.Errors;
using DomainHome = NexusHome.Domain.Entities.Home;

/// <summary>
/// Shared authorization logic for home-scoped operations.
/// Encapsulates the pattern: validate home exists → check user authorization (member/provisioner/SuperAdmin).
/// </summary>
public sealed class HomeAuthorizationService
{
    private readonly IHomeRepository _homeRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IUserPlatformRoleRepository _userPlatformRoleRepository;

    public HomeAuthorizationService(
        IHomeRepository homeRepository,
        IMembershipRepository membershipRepository,
        IUserPlatformRoleRepository userPlatformRoleRepository)
    {
        _homeRepository = homeRepository;
        _membershipRepository = membershipRepository;
        _userPlatformRoleRepository = userPlatformRoleRepository;
    }

    /// <summary>
    /// Validates that the home exists, is not deleted, and the user is authorized to access it.
    /// Returns the home if authorized, null if home not found, or throws if unauthorized (to avoid leaking existence).
    /// </summary>
    /// <param name="homeId">The home ID to validate</param>
    /// <param name="userId">The user ID requesting access</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The home if authorized, null if home doesn't exist or is deleted</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when home exists but user is not authorized</exception>
    public async Task<DomainHome?> AuthorizeHomeAccessAsync(
        Guid homeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var home = await _homeRepository.GetByIdAsync(homeId, cancellationToken);
        if (home == null)
        {
            return null; // Home not found or deleted
        }

        var isAuthorized = await IsUserAuthorizedForHomeAsync(home, userId, cancellationToken);
        if (!isAuthorized)
        {
            // Don't leak home existence - return null instead of throwing
            // The caller should return HomeNotFound error
            return null;
        }

        return home;
    }

    /// <summary>
    /// Checks if a user is authorized to access a home (member, provisioner, or SuperAdmin).
    /// </summary>
    public async Task<bool> IsUserAuthorizedForHomeAsync(
        DomainHome home,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Check if user is a home member (Owner or Member)
        var isMember = await _membershipRepository.IsActiveMemberAsync(userId, home.Id, cancellationToken);

        // Check if user is the home provisioner
        var isProvisioner = home.ProvisionedByTechnicianId == userId;

        // Check if user is PlatformAdmin or SuperAdmin
        var isPlatformAdmin = await _userPlatformRoleRepository.HasPlatformAdminOrHigherAsync(userId, cancellationToken);

        return isMember || isProvisioner || isPlatformAdmin;
    }

    /// <summary>
    /// Validates home exists and user is authorized. Returns HomeNotFound error if not.
    /// Convenience method for handlers that want to return Result directly.
    /// </summary>
    public async Task<Result<DomainHome>> TryAuthorizeHomeAsync(
        Guid homeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var home = await AuthorizeHomeAccessAsync(homeId, userId, cancellationToken);
        if (home == null)
        {
            return Result<DomainHome>.Failure(Errors.Home.HomeNotFound);
        }
        return Result<DomainHome>.Success(home);
    }
}