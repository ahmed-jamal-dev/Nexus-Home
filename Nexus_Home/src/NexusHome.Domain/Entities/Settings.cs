namespace NexusHome.Domain.Entities;

/// <summary>
/// Per-user personal settings.
/// Per DATABASE_MVP_SUMMARY.md and v1.3 baseline Section 6.2.8.
/// Unique index on user_id (one row per user).
/// notification_enabled is dedicated column for high-frequency access.
/// preferences is JSONB for extensible settings.
/// Home-level settings are future decision (O-03).
/// </summary>
public class Settings : Entity
{
    /// <summary>
    /// User ID (unique - one settings row per user).
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Avatar URL.
    /// </summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// Whether notifications are enabled (high-frequency access).
    /// </summary>
    public bool NotificationEnabled { get; private set; } = true;

    /// <summary>
    /// Extensible user preferences (JSONB).
    /// </summary>
    public Dictionary<string, object> Preferences { get; private set; } = new();

    /// <summary>
    /// Navigation to user.
    /// </summary>
    public User? User { get; private set; }

    protected Settings() { }

    public Settings(Guid id, Guid userId)
        : base(id)
    {
        UserId = userId;
    }

    /// <summary>
    /// Updates the avatar URL.
    /// </summary>
    public void UpdateAvatar(string? avatarUrl)
    {
        AvatarUrl = avatarUrl;
        Touch();
    }

    /// <summary>
    /// Enables or disables notifications.
    /// </summary>
    public void SetNotificationEnabled(bool enabled)
    {
        NotificationEnabled = enabled;
        Touch();
    }

    /// <summary>
    /// Updates preferences (merges with existing).
    /// </summary>
    public void UpdatePreferences(Dictionary<string, object> preferences)
    {
        foreach (var kvp in preferences ?? new Dictionary<string, object>())
        {
            Preferences[kvp.Key] = kvp.Value;
        }
        Touch();
    }

    /// <summary>
    /// Removes a preference.
    /// </summary>
    public void RemovePreference(string key)
    {
        Preferences.Remove(key);
        Touch();
    }
}