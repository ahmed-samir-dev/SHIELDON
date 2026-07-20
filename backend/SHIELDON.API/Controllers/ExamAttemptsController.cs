using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Exams.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Enums;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ExamAttemptsController : ControllerBase
{
    private readonly IExamAttemptService _examAttemptService;
    public ExamAttemptsController(
        IExamAttemptService examAttemptService)
    {
        _examAttemptService = examAttemptService;
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? GetUserAgent() => Request.Headers.UserAgent.ToString();

    [HttpPost("exams/{examId}/start")]
    [Authorize(Policy = "RequireStudent")]
    [ProducesResponseType(typeof(ApiResponse<StartExamResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartExam(Guid examId, CancellationToken ct)
    {
        var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _examAttemptService.StartExamAsync(examId, studentId, ct);

        return Ok(response);
    }

    [HttpPatch("exam-attempts/{attemptId}/answers")]
    [Authorize(Policy = "RequireStudent")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SaveAnswer(Guid attemptId, [FromBody] SaveAnswerRequest request, CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue("X-Exam-Token", out var tokenValues))
            return Unauthorized(ApiResponse<object>.Fail("Missing X-Exam-Token header."));

        if (!Guid.TryParse(tokenValues.FirstOrDefault(), out var token))
            return Unauthorized(ApiResponse<object>.Fail("Invalid X-Exam-Token header format."));

        var response = await _examAttemptService.SaveAnswerAsync(attemptId, token, request, ct);
        return Ok(response);
    }

    [HttpPost("exam-attempts/{attemptId}/submit")]
    [Authorize(Policy = "RequireStudent")]
    [ProducesResponseType(typeof(ApiResponse<SubmitExamResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitExam(Guid attemptId, CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue("X-Exam-Token", out var tokenValues))
            return Unauthorized(ApiResponse<object>.Fail("Missing X-Exam-Token header."));

        if (!Guid.TryParse(tokenValues.FirstOrDefault(), out var token))
            return Unauthorized(ApiResponse<object>.Fail("Invalid X-Exam-Token header format."));

        var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _examAttemptService.SubmitExamAsync(attemptId, token, isForceSubmit: false, ct);

        return Ok(response);
    }

    [HttpPost("exam-attempts/{attemptId}/force-submit")]
    [Authorize(Policy = "RequireStudent")]
    [ProducesResponseType(typeof(ApiResponse<SubmitExamResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ForceSubmitExam(Guid attemptId, CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue("X-Exam-Token", out var tokenValues))
            return Unauthorized(ApiResponse<object>.Fail("Missing X-Exam-Token header."));

        if (!Guid.TryParse(tokenValues.FirstOrDefault(), out var token))
            return Unauthorized(ApiResponse<object>.Fail("Invalid X-Exam-Token header format."));

        var response = await _examAttemptService.SubmitExamAsync(attemptId, token, isForceSubmit: true, ct);
        return Ok(response);
    }
}
