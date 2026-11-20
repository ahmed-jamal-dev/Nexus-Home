namespace NexusHome.Domain.Events;

using System.Text.Json.Serialization;

/// <summary>
/// Domain event for outbox pattern.
/// Application layer publishes these, Infrastructure maps to OutboxEvent entity.
/// </summary>
public sealed record OutboxEvent
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Event type name (e.g., "home.provisioned", "claim.credential.generated").
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Event payload as JSON string.
    /// </summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Causation ID for event chaining.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}