namespace NexusHome.Domain.Entities;

using NexusHome.Domain.Enums;

/// <summary>
/// Durable device command lifecycle record with idempotency support.
/// Per DATABASE_MVP_SUMMARY.md and v1.3 baseline Section 6.2.9.
/// - Composite FK to devices (home_id, device_id) -> devices (home_id, id) ON DELETE RESTRICT
/// - FK to homes ON DELETE RESTRICT
/// - FK to users (requested_by_user_id) ON DELETE CASCADE (D-18 confirmed)
/// - Idempotency: unique composite index on (requested_by_user_id, idempotency_key) for non-terminal states
/// - Command type pattern: ^[a-z]+(\.[a-z_]+)+$ (matches NexusLink command.type)
/// - 7 states: Pending, Sent, Acknowledged, Succeeded, Failed, TimedOut, Cancelled
/// - Terminal states are immutable
/// </summary>
public class DeviceCommand : Entity
{
    /// <summary>
    /// NexusLink commandId (device deduplicates via last 20 commandIds).
    /// </summary>
    public Guid CommandId { get; private set; }

    /// <summary>
    /// End-to-end trace correlation ID.
    /// </summary>
    public Guid CorrelationId { get; private set; }

    /// <summary>
    /// Home ID.
    /// </summary>
    public Guid HomeId { get; private set; }

    /// <summary>
    /// Device ID.
    /// </summary>
    public Guid DeviceId { get; private set; }

    /// <summary>
    /// Requesting user ID (nullable for system commands).
    /// CASCADE on delete per D-18 (DATABASE_MVP_SUMMARY.md line 23).
    /// </summary>
    public Guid? RequestedByUserId { get; private set; }

    /// <summary>
    /// Initiator type (user, ai, system).
    /// </summary>
    public InitiatorType InitiatorType { get; private set; }

    /// <summary>
    /// Command type (matches NexusLink command.type pattern).
    /// </summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>
    /// Command parameters (JSONB).
    /// </summary>
    public Dictionary<string, object>? Parameters { get; private set; }

    /// <summary>
    /// Current command status.
    /// </summary>
    public CommandStatus Status { get; private set; } = CommandStatus.Pending;

    /// <summary>
    /// Client-provided idempotency key.
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>
    /// When the command was sent to MQTT.
    /// </summary>
    public DateTimeOffset? SentAt { get; private set; }

    /// <summary>
    /// When the device acknowledged the command.
    /// </summary>
    public DateTimeOffset? AcknowledgedAt { get; private set; }

    /// <summary>
    /// When the command completed (terminal).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Command deadline (expiry).
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Reported state from command.result Succeeded.
    /// </summary>
    public Dictionary<string, object>? ReportedState { get; private set; }

    /// <summary>
    /// Error code (from device or backend).
    /// </summary>
    public string? ErrorCode { get; private set; }

    /// <summary>
    /// Error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Navigation to home.
    /// </summary>
    public Home? Home { get; private set; }

    /// <summary>
    /// Navigation to device.
    /// </summary>
    public Device? Device { get; private set; }

    /// <summary>
    /// Navigation to requesting user.
    /// </summary>
    public User? RequestedByUser { get; private set; }

    /// <summary>
    /// AI action audits for this command.
    /// </summary>
    public ICollection<AIActionAudit> AIActionAudits { get; private set; } = new List<AIActionAudit>();

    protected DeviceCommand() { }

    public DeviceCommand(Guid id, Guid commandId, Guid correlationId, Guid homeId,
        Guid deviceId, InitiatorType initiatorType, string type, DateTimeOffset expiresAt,
        Guid? requestedByUserId = null, Dictionary<string, object>? parameters = null,
        string? idempotencyKey = null)
        : base(id)
    {
        CommandId = commandId;
        CorrelationId = correlationId;
        HomeId = homeId;
        DeviceId = deviceId;
        InitiatorType = initiatorType;
        Type = type;
        ExpiresAt = expiresAt;
        RequestedByUserId = requestedByUserId;
        Parameters = parameters;
        IdempotencyKey = idempotencyKey;
        Status = CommandStatus.Pending;
    }

    /// <summary>
    /// Checks if the command is in a terminal state.
    /// </summary>
    public bool IsTerminal => Status is CommandStatus.Succeeded or CommandStatus.Failed
        or CommandStatus.TimedOut or CommandStatus.Cancelled;

    /// <summary>
    /// Checks if the command can be cancelled.
    /// </summary>
    public bool CanCancel => !IsTerminal;

    /// <summary>
    /// Marks the command as sent to MQTT.
    /// </summary>
    public void MarkSent()
    {
        if (Status != CommandStatus.Pending)
            throw new InvalidOperationException($"Cannot mark as Sent from status {Status}");
        Status = CommandStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Marks the command as acknowledged by device.
    /// </summary>
    public void MarkAcknowledged()
    {
        if (Status != CommandStatus.Sent)
            throw new InvalidOperationException($"Cannot mark as Acknowledged from status {Status}");
        Status = CommandStatus.Acknowledged;
        AcknowledgedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Marks the command as succeeded with reported state.
    /// </summary>
    public void MarkSucceeded(Dictionary<string, object>? reportedState = null)
    {
        if (IsTerminal)
            throw new InvalidOperationException($"Cannot transition from terminal status {Status}");
        Status = CommandStatus.Succeeded;
        CompletedAt = DateTimeOffset.UtcNow;
        ReportedState = reportedState;
        Touch();
    }

    /// <summary>
    /// Marks the command as failed with error.
    /// </summary>
    public void MarkFailed(string errorCode, string errorMessage)
    {
        if (IsTerminal)
            throw new InvalidOperationException($"Cannot transition from terminal status {Status}");
        Status = CommandStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Touch();
    }

    /// <summary>
    /// Marks the command as timed out.
    /// </summary>
    public void MarkTimedOut()
    {
        if (IsTerminal)
            throw new InvalidOperationException($"Cannot transition from terminal status {Status}");
        Status = CommandStatus.TimedOut;
        CompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>
    /// Marks the command as cancelled.
    /// </summary>
    public void MarkCancelled()
    {
        if (IsTerminal)
            throw new InvalidOperationException($"Cannot transition from terminal status {Status}");
        Status = CommandStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }
}