using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Question Bank management.
///
/// Access control:
///   - Admin: full access to all exams
///   - Tutor: can manage questions for exams in their assigned courses only
///   - Student: read-only access to Published exams; IsCorrect is always masked
///
/// Critical security rule: IsCorrect is NEVER returned to students.
/// </summary>
[ApiController]
[Authorize]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;

    public QuestionsController(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ── GET /api/exams/{examId}/questions ──────────────────────────────────

    /// <summary>
    /// List all questions for an exam.
    /// Students only see Published exams. IsCorrect is masked for students.
    /// </summary>
    [HttpGet("api/exams/{examId:guid}/questions")]
    [ProducesResponseType(typeof(ApiResponse<List<QuestionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestions(Guid examId, CancellationToken ct = default)
    {
        var result = await _questionService.GetQuestionsAsync(examId, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<List<QuestionResponse>>.Ok(result, "Questions retrieved successfully."));
    }

    // ── POST /api/exams/{examId}/questions ─────────────────────────────────

    /// <summary>
    /// Add a question to a Draft exam.
    /// MCQ: provide options with exactly 1 IsCorrect.
    /// TrueFalse: provide TrueFalseCorrectAnswer = true|false.
    /// ShortAnswer: no options needed.
    /// </summary>
    [HttpPost("api/exams/{examId:guid}/questions")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<QuestionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddQuestion(
        Guid examId,
        [FromBody] AddQuestionRequest request,
        CancellationToken ct = default)
    {
        var result = await _questionService.AddQuestionAsync(examId, request, GetUserId(), GetUserRole(), ct);
        return Created(
            $"/api/exams/{examId}/questions/{result.Id}",
            ApiResponse<QuestionResponse>.Ok(result, "Question added successfully."));
    }

    // ── PATCH /api/exams/{examId}/questions/{questionId} ───────────────────

    /// <summary>Update a question's text, points, or randomization flag.</summary>
    [HttpPatch("api/exams/{examId:guid}/questions/{questionId:guid}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<QuestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuestion(
        Guid examId,
        Guid questionId,
        [FromBody] UpdateQuestionRequest request,
        CancellationToken ct = default)
    {
        var result = await _questionService.UpdateQuestionAsync(questionId, request, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<QuestionResponse>.Ok(result, "Question updated successfully."));
    }

    // ── DELETE /api/exams/{examId}/questions/{questionId} ──────────────────

    /// <summary>Delete a question from a Draft exam.</summary>
    [HttpDelete("api/exams/{examId:guid}/questions/{questionId:guid}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuestion(
        Guid examId,
        Guid questionId,
        CancellationToken ct = default)
    {
        await _questionService.DeleteQuestionAsync(questionId, GetUserId(), GetUserRole(), ct);
        return NoContent();
    }

    // ── PATCH /api/exams/{examId}/questions/reorder ────────────────────────

    /// <summary>Bulk-update the display order of all questions in a Draft exam.</summary>
    [HttpPatch("api/exams/{examId:guid}/questions/reorder")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderQuestions(
        Guid examId,
        [FromBody] ReorderQuestionsRequest request,
        CancellationToken ct = default)
    {
        await _questionService.ReorderQuestionsAsync(examId, request, GetUserId(), GetUserRole(), ct);
        return NoContent();
    }

    // ── POST /api/questions/{questionId}/options ───────────────────────────

    /// <summary>Add an option to an MCQ question (Draft exam only).</summary>
    [HttpPost("api/questions/{questionId:guid}/options")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<OptionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddOption(
        Guid questionId,
        [FromBody] AddOptionRequest request,
        CancellationToken ct = default)
    {
        var result = await _questionService.AddOptionAsync(questionId, request, GetUserId(), GetUserRole(), ct);
        return Created(
            $"/api/questions/{questionId}/options/{result.Id}",
            ApiResponse<OptionResponse>.Ok(result, "Option added successfully."));
    }

    // ── PATCH /api/questions/{questionId}/options/{optionId} ───────────────

    /// <summary>Update an option's text or mark it as the correct answer.</summary>
    [HttpPatch("api/questions/{questionId:guid}/options/{optionId:guid}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<OptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOption(
        Guid questionId,
        Guid optionId,
        [FromBody] UpdateOptionRequest request,
        CancellationToken ct = default)
    {
        var result = await _questionService.UpdateOptionAsync(optionId, request, GetUserId(), GetUserRole(), ct);
        return Ok(ApiResponse<OptionResponse>.Ok(result, "Option updated successfully."));
    }

    // ── DELETE /api/questions/{questionId}/options/{optionId} ──────────────

    /// <summary>Delete an option from an MCQ question (minimum 2 options enforced).</summary>
    [HttpDelete("api/questions/{questionId:guid}/options/{optionId:guid}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOption(
        Guid questionId,
        Guid optionId,
        CancellationToken ct = default)
    {
        await _questionService.DeleteOptionAsync(optionId, GetUserId(), GetUserRole(), ct);
        return NoContent();
    }
}
