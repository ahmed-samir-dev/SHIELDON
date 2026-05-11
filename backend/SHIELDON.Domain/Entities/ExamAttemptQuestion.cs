namespace SHIELDON.Domain.Entities;

/// <summary>
/// A frozen snapshot of one question assigned to a specific exam attempt.
/// Created when the student starts the exam; prevents changes to the bank
/// from affecting an in-progress or completed attempt.
/// </summary>
public class ExamAttemptQuestion
{
    public Guid Id { get; set; }
    public Guid AttemptId { get; set; }

    /// <summary>The bank question that was selected for this attempt.</summary>
    public Guid QuestionId { get; set; }

    /// <summary>Display order within this specific attempt.</summary>
    public int OrderIndex { get; set; }

    // ── Navigation ───────────────────────────────────────────────
    public ExamAttempt? Attempt { get; set; }
    public ExamQuestion? Question { get; set; }
}
