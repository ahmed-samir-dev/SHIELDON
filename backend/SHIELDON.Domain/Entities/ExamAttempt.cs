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

    public Exam? Exam { get; set; }
    public User? Student { get; set; }
    public ExamToken? Token { get; set; }
    public ICollection<AttemptAnswer> Answers { get; set; } = [];
    public ICollection<ExamAttemptQuestion> AttemptQuestions { get; set; } = [];
}
