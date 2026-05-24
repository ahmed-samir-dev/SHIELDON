using SHIELDON.Application.Features.Exams.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Manages the centralized question bank for a course.
///
/// Business rules:
///   - Only Tutor assigned to the course (or Admin) can modify the bank
///   - Questions are course-scoped - they are NOT tied to a single exam
///   - MCQ: exactly 1 option must be IsCorrect; at least 2 options required
///   - TrueFalse: auto-creates/manages exactly 2 options ("True" / "False")
///   - ShortAnswer: no options; will be manually graded later
///   - IsCorrect is NEVER included in student-facing responses
/// </summary>
public interface IQuestionService
{
    /// <summary>
    /// Add a question to the course question bank.
    /// MCQ: caller must supply at least 2 options with exactly 1 marked IsCorrect.
    /// TrueFalse: caller supplies TrueFalseCorrectAnswer; options auto-created.
    /// ShortAnswer: no options allowed.
    /// </summary>
    Task<QuestionResponse> AddQuestionAsync(
        Guid courseId,
        AddQuestionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all questions in the course bank.
    /// For Admin/Tutor: includes IsCorrect on each option.
    /// For Students: not exposed (bank is Tutor/Admin only).
    /// </summary>
    Task<List<QuestionResponse>> GetQuestionsAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the total count of questions per type in the bank.
    /// Used by the frontend badge and publish validation.
    /// </summary>
    Task<Dictionary<string, int>> GetBankCountsAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>Update a bank question's text, points, or randomization flag.</summary>
    Task<QuestionResponse> UpdateQuestionAsync(
        Guid questionId,
        UpdateQuestionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>Delete a question from the bank (and its options, via cascade).</summary>
    Task DeleteQuestionAsync(
        Guid questionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>Bulk-update the OrderIndex for all questions in the bank.</summary>
    Task ReorderQuestionsAsync(
        Guid courseId,
        ReorderQuestionsRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    // ── Option management (MCQ only) ──────────────────────────────────────────

    /// <summary>Add an option to an MCQ question.</summary>
    Task<OptionResponse> AddOptionAsync(
        Guid questionId,
        AddOptionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>Update an option's text or correct status.</summary>
    Task<OptionResponse> UpdateOptionAsync(
        Guid optionId,
        UpdateOptionRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>Delete an option from an MCQ question (minimum 2 options enforced).</summary>
    Task DeleteOptionAsync(
        Guid optionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    // ── Image management ─────────────────────────────────────────────────────

    /// <summary>
    /// Saves the uploaded image file for a question.
    /// Max 5 MB. Allowed extensions: .jpg, .jpeg, .png, .gif, .webp
    /// Returns the relative URL of the saved image.
    /// </summary>
    Task<string> UploadQuestionImageAsync(
        Guid questionId,
        Stream imageStream,
        string fileName,
        long fileSize,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>Removes the image from a question and deletes the file from storage.</summary>
    Task DeleteQuestionImageAsync(
        Guid questionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);
}
