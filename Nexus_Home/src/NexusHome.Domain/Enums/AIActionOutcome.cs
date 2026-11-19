namespace NexusHome.Domain.Enums;

/// <summary>
/// AI Action Audit outcome classification.
/// Backend classification, not direct device protocol mapping.
/// </summary>
public enum AIActionOutcome
{
    /// <summary>
    /// Action executed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Action failed during execution.
    /// </summary>
    Failure,

    /// <summary>
    /// Action was denied by authorization.
    /// </summary>
    Denied,

    /// <summary>
    /// Action resulted in an error.
    /// </summary>
    Error
}