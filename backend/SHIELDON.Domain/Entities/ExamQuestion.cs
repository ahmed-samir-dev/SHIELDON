using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

public class ExamQuestion
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public decimal Points { get; set; }
    public int OrderIndex { get; set; }
    public bool IsRandomized { get; set; } = true;

    public Exam? Exam { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = [];
}
