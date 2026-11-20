namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Events;

/// <summary>
/// Repository interface for outbox events.
/// </summary>
public interface IOutboxEventRepository
{
    /// <summary>
    /// Adds a new outbox event.
    /// </summary>
    Task AddAsync(OutboxEvent outboxEvent, CancellationToken ct = default);
}