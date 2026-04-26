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

    public Exam? Exam { get; set; }
    public User? Student { get; set; }
    public ExamToken? Token { get; set; }
    public ICollection<AttemptAnswer> Answers { get; set; } = [];
}
