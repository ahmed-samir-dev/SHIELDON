using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Records a significant presence/lifecycle event during a student's exam attempt.
///
/// Used to build the Session Timeline in the Tutor/Admin monitoring dashboard (Phase 5).
/// Events are logged from both the frontend (heartbeats, reconnects) and backend
/// (disconnection detection, force-submit, tutor termination).
/// </summary>
public class PresenceLog
{
    public Guid Id { get; set; }

    // ── Attempt Context ───────────────────────────────────────────────────────

    /// <summary>FK → ExamAttempt. The attempt this presence event belongs to.</summary>
    public Guid AttemptId { get; set; }

    /// <summary>FK → User (Student). Denormalized for fast dashboard queries.</summary>
    public Guid StudentId { get; set; }

    /// <summary>FK → Exam. Denormalized for exam-level timeline queries.</summary>
    public Guid ExamId { get; set; }

    /// <summary>FK → Course. Denormalized for course-level monitoring queries.</summary>
    public Guid CourseId { get; set; }

    // ── Event Details ─────────────────────────────────────────────────────────

    /// <summary>The type of presence event (e.g. ExamStarted, Disconnected, TutorTerminated).</summary>
    public PresenceEventType EventType { get; set; }

    /// <summary>
    /// Optional extra context about the event.
    /// Examples: "Reconnected after 32 seconds", "Terminated by tutor: Ahmed Samir"
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>UTC timestamp of when this event occurred.</summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of when this record was persisted to the database.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ─────────────────────────────────────────────────

    public ExamAttempt? Attempt { get; set; }
    public User? Student { get; set; }
    public Exam? Exam { get; set; }
    public Course? Course { get; set; }
}
