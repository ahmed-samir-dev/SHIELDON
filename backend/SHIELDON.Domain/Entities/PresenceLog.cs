using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Records a significant connectivity/presence state change for a student during
/// an exam attempt. One row is written per state transition, NOT per heartbeat.
///
/// Significant events:
///   - Disconnected  (heartbeat missing for >90 s)
///   - Reconnected   (heartbeat resumed after Disconnected)
///   - PageRefreshed (frontend explicitly signals a page reload / session resume)
///
/// Denormalized ExamId is stored to simplify timeline JOIN queries.
/// </summary>
public class PresenceLog
{
    public Guid Id { get; set; }

    /// <summary>The attempt this presence event belongs to.</summary>
    public Guid AttemptId { get; set; }

    /// <summary>The student who was (or was not) present.</summary>
    public Guid StudentId { get; set; }

    /// <summary>Denormalized exam reference for efficient querying.</summary>
    public Guid ExamId { get; set; }

    /// <summary>The type of presence event.</summary>
    public PresenceEventType EventType { get; set; }

    /// <summary>UTC timestamp when the event occurred.</summary>
    public DateTime OccurredAt { get; set; }

    // Navigation properties
    public ExamAttempt? Attempt { get; set; }
    public User? Student { get; set; }
    public Exam? Exam { get; set; }
}
