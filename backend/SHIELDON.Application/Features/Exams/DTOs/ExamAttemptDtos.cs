using SHIELDON.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SHIELDON.Application.Features.Exams.DTOs;

public record StartExamResponse(
    Guid AttemptId,
    Guid Token,
    int TimeLimitMinutes,
    decimal PassScore,
    DateTime ExpiresAt,
    IReadOnlyList<StudentQuestionDto> Questions,
    IReadOnlyList<SavedAnswerDto> SavedAnswers,
    /// <summary>Course ID — used by frontend for post-submit redirect back to Exams tab.</summary>
    Guid CourseId,
    /// <summary>Result visibility setting — used by frontend to decide redirect destination after submit.</summary>
    string ResultVisibility
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

public record SavedAnswerDto(
    Guid QuestionId,
    Guid? SelectedOptionId,
    string? TextAnswer
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
    bool Passed,
    /// <summary>Exam's result visibility — used by frontend to decide redirect after submit.</summary>
    string ResultVisibility,
    /// <summary>Course ID — used by frontend for post-submit redirect.</summary>
    Guid CourseId
);
