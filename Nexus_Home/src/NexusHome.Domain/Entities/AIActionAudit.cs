namespace NexusHome.Domain.Entities;

using NexusHome.Domain.Enums;

/// <summary>
/// AI action audit trail.
/// Per DATABASE_MVP_SUMMARY.md and v1.3 baseline Section 6.2.10.
/// - FK to users: ON DELETE SET NULL (soft delete nullifies)
/// - FK to devices: ON DELETE RESTRICT
/// - FK to device_commands.command_id: ON DELETE SET NULL
/// - outcome: success, failure, denied, error (backend classification)
/// </summary>
public class AIActionAudit : Entity
{
    /// <summary>
    /// User ID (nullable - SET NULL on user delete).
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Device ID (nullable - RESTRICT on device delete).
    /// </summary>
    public Guid? DeviceId { get; private set; }

    /// <summary>
    /// Correlation ID linking to request.
    /// </summary>
    public Guid? CorrelationId { get; private set; }

    /// <summary>
    /// Device command ID (nullable - SET NULL on command delete).
    /// </summary>
    public Guid? CommandId { get; private set; }

    /// <summary>
    /// Action type (e.g., "climate.set_temperature").
    /// </summary>
    public string ActionType { get; private set; } = string.Empty;

    /// <summary>
    /// Action parameters (JSONB).
    /// </summary>
    public Dictionary<string, object> ActionParameters { get; private set; } = new();

    /// <summary>
    /// Outcome classification.
    /// </summary>
    public AIActionOutcome Outcome { get; private set; }

    /// <summary>
    /// Action timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>
    /// Navigation to user.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Navigation to device.
    /// </summary>
    public Device? Device { get; private set; }

    protected AIActionAudit() { }

    public AIActionAudit(Guid id, string actionType, AIActionOutcome outcome,
        DateTimeOffset timestamp, Guid? userId = null, Guid? deviceId = null,
        Guid? correlationId = null, Guid? commandId = null,
        Dictionary<string, object>? actionParameters = null)
        : base(id)
    {
        ActionType = actionType;
        Outcome = outcome;
        Timestamp = timestamp;
        UserId = userId;
        DeviceId = deviceId;
        CorrelationId = correlationId;
        CommandId = commandId;
        ActionParameters = actionParameters ?? new Dictionary<string, object>();
    }
}