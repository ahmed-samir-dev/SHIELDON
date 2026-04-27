using SHIELDON.Application.Features.Exams.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Manages the Question Bank for each exam.
///
/// Business rules enforced here:
///   - Only Tutor assigned to the course (or Admin) can modify questions
///   - Questions can only be added/edited/deleted on Draft exams
///   - MCQ: exactly 1 option must be IsCorrect; at least 2 options required
///   - TrueFalse: auto-creates/manages exactly 2 options ("True" / "False")
///   - ShortAnswer: no options; will be manually graded later
///   - IsCorrect is NEVER included in student-facing responses
/// </summary>
public interface IQuestionService
{
    /// <summary>
    /// Add a question to a Draft exam.
    /// MCQ: caller must supply at least 2 options with exactly 1 marked IsCorrect.
    /// TrueFalse: caller supplies TrueFalseCorrectAnswer (true/false); options auto-created.
    /// ShortAnswer: no options allowed.
    /// </summary>
    Task<QuestionResponse> AddQuestionAsync(
        Guid examId,
        AddQuestionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all questions for an exam.
    /// For Admin/Tutor: includes IsCorrect on each option.
    /// For Students: IsCorrect is masked to false on every option.
    /// </summary>
    Task<List<QuestionResponse>> GetQuestionsAsync(
        Guid examId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Update a question's text, points, or randomization flag.
    /// Only possible on Draft exams.
    /// </summary>
    Task<QuestionResponse> UpdateQuestionAsync(
        Guid questionId,
        UpdateQuestionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a question (and its options, via cascade).
    /// Only possible on Draft exams.
    /// </summary>
    Task DeleteQuestionAsync(
        Guid questionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Bulk-update the OrderIndex for all questions in an exam.
    /// Caller must supply one entry per question in the exam.
    /// </summary>
    Task ReorderQuestionsAsync(
        Guid examId,
        ReorderQuestionsRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    // ── Option management (MCQ only) ──────────────────────────────────────────

    /// <summary>Add an option to an MCQ question (Draft exam only).</summary>
    Task<OptionResponse> AddOptionAsync(
        Guid questionId,
        AddOptionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>Update an option's text or correct status (Draft exam only).</summary>
    Task<OptionResponse> UpdateOptionAsync(
        Guid optionId,
        UpdateOptionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>Delete an option from an MCQ question (Draft exam only).</summary>
    Task DeleteOptionAsync(
        Guid optionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);
}
