namespace NexusHome.Infrastructure.Entities;

/// <summary>
/// Telemetry deduplication ledger for exactly-once ingestion.
/// Per v1.3 baseline Section 6.2.14.
/// </summary>
public class TelemetryDedupLedger
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Device ID.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Message ID from device (for deduplication).
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the message was received.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Expiration timestamp for cleanup.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}