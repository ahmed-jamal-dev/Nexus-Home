namespace NexusHome.Domain.Entities;

/// <summary>
/// User account.
/// Per DATABASE_MVP_SUMMARY.md and v1.3 baseline Section 6.2.2.
/// Soft delete via DeletedAt, email anonymized on delete.
/// </summary>
public class User : Entity
{
    /// <summary>
    /// Email address (unique among active users).
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Password hash (bcrypt/Argon2).
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Full name.
    /// </summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Whether email has been verified.
    /// </summary>
    public bool EmailVerified { get; private set; } = false;

    /// <summary>
    /// Memberships of this user in homes.
    /// </summary>
    public ICollection<Membership> Memberships { get; private set; } = new List<Membership>();

    /// <summary>
    /// Invitations sent by this user.
    /// </summary>
    public ICollection<Invitation> SentInvitations { get; private set; } = new List<Invitation>();

    /// <summary>
    /// Device commands requested by this user.
    /// </summary>
    public ICollection<DeviceCommand> DeviceCommands { get; private set; } = new List<DeviceCommand>();

    /// <summary>
    /// AI action audit records for this user.
    /// </summary>
    public ICollection<AIActionAudit> AIActionAudits { get; private set; } = new List<AIActionAudit>();

    /// <summary>
    /// Refresh tokens for this user.
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    /// <summary>
    /// User settings.
    /// </summary>
    public Settings? Settings { get; private set; }

    /// <summary>
    /// Version for platform roles cache invalidation (incremented on role changes).
    /// Used in JWT prv claim for token invalidation.
    /// </summary>
    public int PlatformRolesVersion { get; private set; } = 1;

    /// <summary>
    /// Platform-level role assignments for this user.
    /// </summary>
    public ICollection<UserPlatformRole> UserPlatformRoles { get; private set; } = new List<UserPlatformRole>();

    protected User() { }

    public User(Guid id, string email, string passwordHash, string fullName)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
    }

    /// <summary>
    /// Updates the user's profile information.
    /// </summary>
    public void UpdateProfile(string fullName)
    {
        FullName = fullName;
        Touch();
    }

    /// <summary>
    /// Marks email as verified.
    /// </summary>
    public void VerifyEmail()
    {
        EmailVerified = true;
        Touch();
    }

    /// <summary>
    /// Activates the user account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary>
    /// Deactivates the user account (soft delete alternative).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Changes the password hash.
    /// </summary>
    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        Touch();
    }

    /// <summary>
    /// Anonymizes the user for GDPR "right to erasure" (soft delete path).
    /// Per v1.3 baseline Section 6.2.2: email = 'deleted_' || id || '@placeholder',
    /// full_name = 'Deleted User', password_hash = '', is_active = false.
    /// </summary>
    public void Anonymize()
    {
        Email = $"deleted_{Id}@placeholder";
        FullName = "Deleted User";
        PasswordHash = string.Empty;
        EmailVerified = false;
        IsActive = false;
        Delete(); // Sets DeletedAt
    }

    /// <summary>
    /// Increments the platform roles version for token invalidation.
    /// </summary>
    public void IncrementPlatformRolesVersion()
    {
        PlatformRolesVersion++;
        Touch();
    }
}