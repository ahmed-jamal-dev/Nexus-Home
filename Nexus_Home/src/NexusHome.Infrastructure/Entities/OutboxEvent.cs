namespace NexusHome.Infrastructure.Entities;

using NexusHome.Domain.Enums;

/// <summary>
/// Outbox event for transactional outbox pattern.
/// Ensures PG→Redis/SignalR consistency.
/// Per v1.3 baseline Section 6.2.13.
/// </summary>
public class OutboxEvent
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event type name (e.g., "DeviceStateChanged", "DeviceCommandCompleted").
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event payload as JSON.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Causation ID for event chaining.
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Current processing status.
    /// </summary>
    public OutboxEventStatus Status { get; set; } = OutboxEventStatus.Pending;

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Last error message if processing failed.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Processing completion timestamp.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>
    /// Next retry timestamp.
    /// </summary>
    public DateTimeOffset? NextRetryAt { get; set; }
}