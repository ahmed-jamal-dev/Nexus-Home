namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Events;
using NexusHome.Domain.Enums;
using NexusHome.Infrastructure.Data;
using DomainOutboxEvent = NexusHome.Domain.Events.OutboxEvent;
using InfrastructureOutboxEvent = NexusHome.Infrastructure.Entities.OutboxEvent;

/// <summary>
/// EF Core implementation of outbox event repository.
/// Maps domain OutboxEvent to infrastructure OutboxEvent entity.
/// </summary>
public sealed class OutboxEventRepository : IOutboxEventRepository
{
    private readonly NexusHomeDbContext _context;

    public OutboxEventRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(DomainOutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        var entity = new InfrastructureOutboxEvent
        {
            Id = outboxEvent.Id,
            EventType = outboxEvent.EventType,
            Payload = outboxEvent.Payload,
            CorrelationId = outboxEvent.CorrelationId,
            CausationId = outboxEvent.CausationId,
            Status = OutboxEventStatus.Pending,
            CreatedAt = outboxEvent.CreatedAt
        };

        _context.OutboxEvents.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<DomainOutboxEvent>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var entities = await _context.OutboxEvents
            .Where(o => o.Status == OutboxEventStatus.Pending
                && (o.NextRetryAt == null || o.NextRetryAt <= DateTimeOffset.UtcNow))
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task MarkAsCompletedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.OutboxEvents
            .Where(o => o.Id == eventId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, OutboxEventStatus.Completed)
                .SetProperty(o => o.ProcessedAt, DateTimeOffset.UtcNow), cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(Guid eventId, string error, CancellationToken cancellationToken = default)
    {
        var entity = await _context.OutboxEvents
            .Where(o => o.Id == eventId)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            return;
        }

        var newRetryCount = entity.RetryCount + 1;
        var nextRetryAt = CalculateNextRetry(newRetryCount);

        var newStatus = newRetryCount >= 5 ? OutboxEventStatus.DeadLetter : OutboxEventStatus.Failed;

        await _context.OutboxEvents
            .Where(o => o.Id == eventId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, newStatus)
                .SetProperty(o => o.RetryCount, newRetryCount)
                .SetProperty(o => o.LastError, error)
                .SetProperty(o => o.NextRetryAt, nextRetryAt), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OutboxEvents
            .Where(o => o.Status == OutboxEventStatus.Pending
                && (o.NextRetryAt == null || o.NextRetryAt <= DateTimeOffset.UtcNow))
            .CountAsync(cancellationToken);
    }

    private static DomainOutboxEvent MapToDomain(InfrastructureOutboxEvent entity)
    {
        return new DomainOutboxEvent
        {
            Id = entity.Id,
            EventType = entity.EventType,
            Payload = entity.Payload,
            CorrelationId = entity.CorrelationId,
            CausationId = entity.CausationId,
            CreatedAt = entity.CreatedAt
        };
    }

    private static DateTimeOffset? CalculateNextRetry(int retryCount)
    {
        // Exponential backoff: 1min, 2min, 4min, 8min, 16min
        var delayMinutes = Math.Pow(2, retryCount - 1);
        return DateTimeOffset.UtcNow.AddMinutes(delayMinutes);
    }
}