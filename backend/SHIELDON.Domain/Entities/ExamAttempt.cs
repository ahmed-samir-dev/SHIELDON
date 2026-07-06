using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

public class ExamAttempt
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public decimal? Score { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;

    /// <summary>
    /// System-generated notes for this attempt (e.g., fallback randomization warning
    /// when the question bank didn't have enough unseen questions for a retake).
    /// Visible to Tutors/Admins, never to students.
    /// </summary>
    public string? Notes { get; set; }

    // ── Phase 7: Presence Tracking ─────────────────────────────────────────────

    /// <summary>
    /// UTC timestamp of the last received heartbeat from the student's browser.
    /// Updated every 30 seconds while the exam tab is open and active.
    /// Null until the first heartbeat arrives after exam start.
    /// </summary>
    public DateTime? LastHeartbeatAt { get; set; }

    /// <summary>
    /// True when the background monitor has detected a heartbeat timeout (>90s gap).
    /// Reset to false when the next heartbeat arrives (student reconnected).
    /// </summary>
    public bool IsDisconnected { get; set; } = false;

    public Exam? Exam { get; set; }
    public User? Student { get; set; }
    public ExamToken? Token { get; set; }
    public ICollection<AttemptAnswer> Answers { get; set; } = [];
    public ICollection<ExamAttemptQuestion> AttemptQuestions { get; set; } = [];
    public ICollection<PresenceLog> PresenceLogs { get; set; } = [];
}
