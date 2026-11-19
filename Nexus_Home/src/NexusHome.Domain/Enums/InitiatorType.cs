namespace NexusHome.Domain.Enums;

/// <summary>
/// Type of command initiator.
/// </summary>
public enum InitiatorType
{
    /// <summary>
    /// User-initiated command.
    /// </summary>
    User,

    /// <summary>
    /// AI-proposed command.
    /// </summary>
    Ai,

    /// <summary>
    /// System-initiated command.
    /// </summary>
    System
}