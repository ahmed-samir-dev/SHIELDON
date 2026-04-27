using SHIELDON.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SHIELDON.Application.Features.Exams.DTOs;

public record StartExamResponse(
    Guid AttemptId,
    Guid Token,
    int TimeLimitMinutes,
    decimal PassScore,
    DateTime ExpiresAt,
    IReadOnlyList<StudentQuestionDto> Questions
);

public record StudentQuestionDto(
    Guid Id,
    string Text,
    QuestionType Type,
    decimal Points,
    IReadOnlyList<StudentOptionDto> Options
);

public record StudentOptionDto(
    Guid Id,
    string Text
);

public class SaveAnswerRequest
{
    [Required]
    public Guid QuestionId { get; set; }

    public Guid? SelectedOptionId { get; set; }
    
    [MaxLength(2000)]
    public string? TextAnswer { get; set; }
}

public record SubmitExamResponse(
    Guid AttemptId,
    AttemptStatus Status,
    decimal? Score,
    bool Passed
);
