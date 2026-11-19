namespace NexusHome.Domain.Enums;

/// <summary>
/// Membership role within a home.
/// </summary>
public enum Role
{
    /// <summary>
    /// Home owner - manages the home, members, device registry, settings, and permission grants.
    /// </summary>
    Owner,

    /// <summary>
    /// Home member - can use allowed home features and control permitted devices.
    /// </summary>
    Member,

    /// <summary>
    /// Home guest - starts with restricted access and receives only explicitly allowed capabilities.
    /// </summary>
    Guest
}