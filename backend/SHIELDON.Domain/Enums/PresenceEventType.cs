namespace SHIELDON.Domain.Enums;

/// <summary>
/// Classifies the type of presence/connectivity event logged during an exam attempt.
/// These events are stored in PresenceLog and merged into the attempt timeline.
/// </summary>
public enum PresenceEventType
{
    /// <summary>Student's heartbeat stopped arriving for more than 90 seconds.</summary>
    Disconnected,

    /// <summary>Student's heartbeat resumed after a Disconnected period.</summary>
    Reconnected,

    /// <summary>Student reloaded or navigated back to the exam page mid-session.</summary>
    PageRefreshed
}
