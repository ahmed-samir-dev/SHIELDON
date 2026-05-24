using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Centralized question bank for a course.
/// Tutors/Admins manage questions here; exams draw from this bank at start time.
///
/// Access control:
///   - Admin: full access to all courses
///   - Tutor: can manage questions for courses assigned to them only
///   - Students: no access (bank is internal)
///
/// Critical security rule: IsCorrect is NEVER returned to non-Admin/Tutor callers.
/// </summary>
[ApiController]
[Route("api/courses/{courseId:guid}/question-bank")]
[Authorize(Roles = "Admin,Tutor")]
public class CourseQuestionBankController : ControllerBase
{
    private readonly IQuestionService _questionService;

    public CourseQuestionBankController(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ── GET /api/courses/{courseId}/question-bank ──────────────────────────────

    /// <summary>List all questions in the course question bank.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<QuestionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuestions(Guid courseId, CancellationToken ct = default)
    {
        var result = await _questionService.GetQuestionsAsync(courseId, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<List<QuestionResponse>>.Ok(result, "Questions retrieved successfully."));
    }

    // ── GET /api/courses/{courseId}/question-bank/counts ───────────────────────

    /// <summary>Returns question count per type in the bank (used for the badge).</summary>
    [HttpGet("counts")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, int>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCounts(Guid courseId, CancellationToken ct = default)
    {
        var result = await _questionService.GetBankCountsAsync(courseId, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<Dictionary<string, int>>.Ok(result, "Bank counts retrieved successfully."));
    }

    // ── POST /api/courses/{courseId}/question-bank ─────────────────────────────

    /// <summary>
    /// Add a question to the course question bank.
    /// MCQ: provide options with exactly 1 IsCorrect.
    /// TrueFalse: provide TrueFalseCorrectAnswer = true|false.
    /// ShortAnswer: no options needed.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<QuestionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddQuestion(
        Guid courseId,
        [FromBody] AddQuestionRequest request,
        CancellationToken ct = default)
    {
        var result = await _questionService.AddQuestionAsync(courseId, request, GetUserId(), GetUserRole(), ct);
        return Created(
            $"/api/courses/{courseId}/question-bank/{result.Id}",
            ApiResponse<QuestionResponse>.Ok(result, "Question added to bank successfully."));
    }

    // ── PATCH /api/courses/{courseId}/question-bank/{questionId} ──────────────

    /// <summary>Update a question's text, points, or options.</summary>
    [HttpPatch("{questionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<QuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuestion(
        Guid courseId,
        Guid questionId,
        [FromBody] UpdateQuestionRequest request,
        CancellationToken ct = default)
    {
        var result = await _questionService.UpdateQuestionAsync(questionId, request, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<QuestionResponse>.Ok(result, "Question updated successfully."));
    }

    // ── DELETE /api/courses/{courseId}/question-bank/{questionId} ─────────────

    /// <summary>Delete a question from the bank.</summary>
    [HttpDelete("{questionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuestion(
        Guid courseId,
        Guid questionId,
        CancellationToken ct = default)
    {
        await _questionService.DeleteQuestionAsync(questionId, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<object>.Ok("Question deleted from bank successfully."));
    }

    // ── PATCH /api/courses/{courseId}/question-bank/reorder ───────────────────

    /// <summary>Bulk-update the display order of all questions in the bank.</summary>
    [HttpPatch("reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReorderQuestions(
        Guid courseId,
        [FromBody] ReorderQuestionsRequest request,
        CancellationToken ct = default)
    {
        await _questionService.ReorderQuestionsAsync(courseId, request, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<object>.Ok("Questions reordered successfully."));
    }

    // ── Option endpoints ───────────────────────────────────────────────────────

    /// <summary>Add an option to an MCQ question.</summary>
    [HttpPost("{questionId:guid}/options")]
    [ProducesResponseType(typeof(ApiResponse<OptionResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddOption(
        Guid courseId,
        Guid questionId,
        [FromBody] AddOptionRequest request,
        CancellationToken ct = default)
    {
        var result = await _questionService.AddOptionAsync(questionId, request, GetUserId(), GetUserRole(), ct);
        return Created(
            $"/api/courses/{courseId}/question-bank/{questionId}/options/{result.Id}",
            ApiResponse<OptionResponse>.Ok(result, "Option added successfully."));
    }

    /// <summary>Update an option's text or mark it as the correct answer.</summary>
    [HttpPatch("{questionId:guid}/options/{optionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOption(
        Guid courseId,
        Guid questionId,
        Guid optionId,
        [FromBody] UpdateOptionRequest request,
        CancellationToken ct = default)
    {
        var result = await _questionService.UpdateOptionAsync(optionId, request, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<OptionResponse>.Ok(result, "Option updated successfully."));
    }

    /// <summary>Delete an option from an MCQ question (minimum 2 options enforced).</summary>
    [HttpDelete("{questionId:guid}/options/{optionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteOption(
        Guid courseId,
        Guid questionId,
        Guid optionId,
        CancellationToken ct = default)
    {
        await _questionService.DeleteOptionAsync(optionId, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<object>.Ok("Option deleted successfully."));
    }

    // ── Image endpoints ────────────────────────────────────────────────────────

    /// <summary>
    /// Upload or replace an image for a question.
    /// Max size: 5 MB. Allowed: .jpg, .jpeg, .png, .gif, .webp
    /// Returns the relative URL of the saved image.
    /// </summary>
    [HttpPost("{questionId:guid}/image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadQuestionImage(
        Guid courseId,
        Guid questionId,
        IFormFile image,
        CancellationToken ct = default)
    {
        await using var stream = image.OpenReadStream();
        var imageUrl = await _questionService.UploadQuestionImageAsync(
            questionId, stream, image.FileName, image.Length, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<string>.Ok(imageUrl, "Question image uploaded successfully."));
    }

    /// <summary>Removes the image from a question.</summary>
    [HttpDelete("{questionId:guid}/image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuestionImage(
        Guid courseId,
        Guid questionId,
        CancellationToken ct = default)
    {
        await _questionService.DeleteQuestionImageAsync(questionId, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<object>.Ok("Question image removed successfully."));
    }
}
