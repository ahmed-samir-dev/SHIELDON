using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Records the outcome of a tutor or admin manual review of a suspicious exam session.
///
/// Created when a Tutor/Admin opens the Manual Review page and submits a final decision.
/// The decision can: accept the result, zero it out (cheating), or grant a re-attempt.
/// One session can have at most one ReviewDecision.
/// </summary>
public class ReviewDecision
{
    public Guid Id { get; set; }

    // ── Attempt Context ───────────────────────────────────────────────────────

    /// <summary>FK → ExamAttempt. The attempt being reviewed.</summary>
    public Guid AttemptId { get; set; }

    /// <summary>FK → User (Reviewer). The tutor or admin who submitted this decision.</summary>
    public Guid ReviewerId { get; set; }

    // ── Decision Details ──────────────────────────────────────────────────────

    /// <summary>The final decision: Accepted, MarkedAsCheating, or ReAttemptGranted.</summary>
    public ReviewDecisionType Decision { get; set; }

    /// <summary>Optional free-text notes explaining the rationale behind the decision.</summary>
    public string? Notes { get; set; }

    /// <summary>UTC timestamp of when the decision was submitted.</summary>
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ─────────────────────────────────────────────────

    public ExamAttempt? Attempt { get; set; }
    public User? Reviewer { get; set; }
}
