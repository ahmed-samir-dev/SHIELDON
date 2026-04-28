using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Defines how many questions of a given type an exam should draw from the course bank.
/// e.g. { QuestionType = MCQ, Count = 5 } means "pick 5 random MCQ questions from the bank".
/// </summary>
public class ExamSelectionRule
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public QuestionType QuestionType { get; set; }

    /// <summary>How many questions of this type to randomly draw from the bank.</summary>
    public int Count { get; set; }

    // ── Navigation ───────────────────────────────────────────────
    public Exam? Exam { get; set; }
}
