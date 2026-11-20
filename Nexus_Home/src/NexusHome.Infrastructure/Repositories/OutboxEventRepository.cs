namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Events;
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
            Status = Domain.Enums.OutboxEventStatus.Pending,
            CreatedAt = outboxEvent.CreatedAt
        };

        _context.OutboxEvents.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}