namespace NexusHome.Infrastructure.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Enums;

/// <summary>
/// Background service that processes outbox events.
/// Per v1.3 baseline Section 6.2.13 - Transactional outbox pattern for PG→Redis/SignalR consistency.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxProcessorOptions _options;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger,
        IOptions<OutboxProcessorOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor starting. Polling interval: {Interval}s, Batch size: {BatchSize}",
            _options.PollingIntervalSeconds, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox batch");
            }

            // Wait for next polling interval
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Outbox processor stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();

        var pendingEvents = await repository.GetPendingAsync(_options.BatchSize, cancellationToken);

        if (pendingEvents.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Processing {Count} outbox events", pendingEvents.Count);

        foreach (var outboxEvent in pendingEvents)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ProcessEventAsync(outboxEvent, scope.ServiceProvider, cancellationToken);
                await repository.MarkAsCompletedAsync(outboxEvent.Id, cancellationToken);
                _logger.LogDebug("Processed outbox event {EventId} of type {EventType}", outboxEvent.Id, outboxEvent.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox event {EventId} of type {EventType}", outboxEvent.Id, outboxEvent.EventType);
                await repository.MarkAsFailedAsync(outboxEvent.Id, ex.Message, cancellationToken);
            }
        }
    }

    private async Task ProcessEventAsync(Domain.Events.OutboxEvent outboxEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        // Dispatch to appropriate handler based on event type
        // This is a placeholder - actual implementation would use a dispatcher or Mediator
        // to route events to the appropriate handlers (e.g., SignalR notification, external webhook, etc.)

        _logger.LogInformation("Processing outbox event: {EventType} (ID: {EventId})", outboxEvent.EventType, outboxEvent.Id);

        // TODO: Implement actual event processing logic
        // Examples:
        // - For "home.provisioned": notify provisioning service, update caches
        // - For "claim.credential.generated": send notification to user
        // - For "home.claimed": update membership cache, notify SignalR clients
        // - For "device.state.changed": publish to Redis for SignalR real-time updates
        // - For "device.command.completed": notify command sender

        // For now, just log and mark as completed
        await Task.CompletedTask;
    }
}

/// <summary>
/// Configuration options for the outbox processor.
/// </summary>
public sealed class OutboxProcessorOptions
{
    /// <summary>
    /// How often to poll for pending events (in seconds).
    /// </summary>
    public int PollingIntervalSeconds { get; init; } = 30;

    /// <summary>
    /// Maximum number of events to process in a single batch.
    /// </summary>
    public int BatchSize { get; init; } = 100;
}