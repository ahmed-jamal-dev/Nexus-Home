namespace NexusHome.Domain.Enums;

/// <summary>
/// Device command lifecycle states.
/// 7 states per v1.3 baseline Section 10.1.
/// Device protocol (NexusLink) only has Acknowledged, Succeeded, Failed.
/// Backend tracks additional states.
/// </summary>
public enum CommandStatus
{
    /// <summary>
    /// Created, not yet dispatched to MQTT.
    /// </summary>
    Pending,

    /// <summary>
    /// Published to MQTT (QoS 1), awaiting device acknowledgement.
    /// </summary>
    Sent,

    /// <summary>
    /// Device validated command, will execute.
    /// </summary>
    Acknowledged,

    /// <summary>
    /// Device completed successfully (terminal).
    /// </summary>
    Succeeded,

    /// <summary>
    /// Device execution failed (terminal).
    /// </summary>
    Failed,

    /// <summary>
    /// No device result before expires_at (terminal).
    /// </summary>
    TimedOut,

    /// <summary>
    /// Backend requested cancellation (terminal).
    /// </summary>
    Cancelled
}