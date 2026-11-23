namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Events;
using NexusHome.Domain.Enums;

/// <summary>
/// Repository interface for outbox events.
/// </summary>
public interface IOutboxEventRepository
{
    /// <summary>
    /// Adds a new outbox event.
    /// </summary>
    Task AddAsync(OutboxEvent outboxEvent, CancellationToken ct = default);

    /// <summary>
    /// Gets pending outbox events for processing.
    /// </summary>
    Task<List<OutboxEvent>> GetPendingAsync(int batchSize, CancellationToken ct = default);

    /// <summary>
    /// Marks an outbox event as completed (processed successfully).
    /// </summary>
    Task MarkAsCompletedAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Marks an outbox event as failed and increments retry count.
    /// </summary>
    Task MarkAsFailedAsync(Guid eventId, string error, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of pending events for monitoring.
    /// </summary>
    Task<int> GetPendingCountAsync(CancellationToken ct = default);
}