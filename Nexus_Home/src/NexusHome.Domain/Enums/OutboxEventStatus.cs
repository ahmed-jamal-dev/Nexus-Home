namespace NexusHome.Domain.Enums;

/// <summary>
/// Outbox event processing status.
/// Per v1.3 baseline Section 6.2.13.
/// </summary>
public enum OutboxEventStatus
{
    /// <summary>
    /// Event created, awaiting processing.
    /// </summary>
    Pending,

    /// <summary>
    /// Event is being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Event processed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Event processing failed (will retry).
    /// </summary>
    Failed,

    /// <summary>
    /// Event permanently failed (max retries exceeded).
    /// </summary>
    DeadLetter
}