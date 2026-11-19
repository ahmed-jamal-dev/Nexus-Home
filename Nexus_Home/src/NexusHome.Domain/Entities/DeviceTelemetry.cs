namespace NexusHome.Domain.Entities;

/// <summary>
/// Time-series sensor reading (TimescaleDB hypertable).
/// Per DATABASE_MVP_SUMMARY.md and v1.3 baseline Section 6.2.12.
/// - PK is (Id, Timestamp) to satisfy TimescaleDB requirement.
/// - Partitioned by timestamp (1-day chunks).
/// - Non-unique index on (device_id, timestamp).
/// - Multiple metrics per device at one timestamp allowed.
/// - Deduplication via separate telemetry_dedup_ledger table.
/// - Schema validated: name pattern ^[a-z][a-z0-9_]*$, value=number, unit=string required, maxItems=8.
/// </summary>
public class DeviceTelemetry : Entity
{
    /// <summary>
    /// Device ID.
    /// </summary>
    public Guid DeviceId { get; private set; }

    /// <summary>
    /// Reading timestamp (partitioning column for TimescaleDB).
    /// </summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>
    /// NexusLink messageId for deduplication.
    /// </summary>
    public Guid MessageId { get; private set; }

    /// <summary>
    /// Position in readings array (0-7).
    /// </summary>
    public short ReadingIndex { get; private set; }

    /// <summary>
    /// Metric name (e.g., "temperature", "humidity").
    /// Pattern: ^[a-z][a-z0-9_]*$
    /// </summary>
    public string MetricName { get; private set; } = string.Empty;

    /// <summary>
    /// Metric value (number per telemetry.schema.json).
    /// </summary>
    public double MetricValue { get; private set; }

    /// <summary>
    /// Metric unit (e.g., "celsius", "percent").
    /// Required per telemetry.schema.json.
    /// </summary>
    public string MetricUnit { get; private set; } = string.Empty;

    /// <summary>
    /// Navigation to device.
    /// </summary>
    public Device? Device { get; private set; }

    protected DeviceTelemetry() { }

    public DeviceTelemetry(Guid id, Guid deviceId, DateTimeOffset timestamp,
        Guid messageId, short readingIndex, string metricName, double metricValue, string metricUnit)
        : base(id)
    {
        DeviceId = deviceId;
        Timestamp = timestamp;
        MessageId = messageId;
        ReadingIndex = readingIndex;
        MetricName = metricName;
        MetricValue = metricValue;
        MetricUnit = metricUnit;
    }
}