namespace SHIELDON.Domain.Entities;

public class AttemptAnswer
{
    public Guid Id { get; set; }
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public string? TextAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public decimal? PointsAwarded { get; set; }

    public ExamAttempt? Attempt { get; set; }
    public ExamQuestion? Question { get; set; }
    public QuestionOption? SelectedOption { get; set; }
}
