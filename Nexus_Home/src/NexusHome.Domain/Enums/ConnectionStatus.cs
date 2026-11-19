namespace NexusHome.Domain.Enums;

/// <summary>
/// Device connection status.
/// Backend uses ('online', 'offline', 'unknown').
/// NexusLink availability schema defines only 'online', 'offline'.
/// Backend derives 'unknown' after 3 missed heartbeats (90s).
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Device is online and responding.
    /// </summary>
    Online,

    /// <summary>
    /// Device is offline (explicitly reported or Last Will).
    /// </summary>
    Offline,

    /// <summary>
    /// Unknown - derived by backend after 3 missed heartbeats.
    /// Never sent by device.
    /// </summary>
    Unknown
}