namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Entities;

/// <summary>
/// Repository for user operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by email.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user with the given email exists.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user.
    /// </summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with their platform roles for platform administration list view.
    /// </summary>
    Task<List<(Guid UserId, string Email, string FullName, bool IsActive, bool EmailVerified, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, List<string> RoleKeys)>> GetUsersWithPlatformRolesAsync(
        string? search = null,
        bool? isActive = null,
        string? role = null,
        string? cursor = null,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total count of users matching filter for platform administration.
    /// </summary>
    Task<int> GetUsersCountAsync(
        string? search = null,
        bool? isActive = null,
        string? role = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user detail with platform roles and home memberships for platform administration.
    /// </summary>
    Task<(User User, List<(UserPlatformRole Role, string AssignedByEmail)> PlatformRoles, List<(Membership Membership, string UserEmail)> Memberships)?> GetUserDetailWithRolesAndMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total user count.
    /// </summary>
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active user count.
    /// </summary>
    Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default);
}