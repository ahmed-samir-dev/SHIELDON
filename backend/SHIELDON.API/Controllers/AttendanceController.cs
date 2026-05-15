using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SHIELDON.API.Hubs;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Attendance.DTOs;
using SHIELDON.Application.Interfaces;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IHubContext<AttendanceHub> _hubContext;

    public AttendanceController(IAttendanceService attendanceService, IHubContext<AttendanceHub> hubContext)
    {
        _attendanceService = attendanceService;
        _hubContext = hubContext;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException());

    // ── Tutor / Admin Endpoints ───────────────────────────────────────────────

    [HttpPost("checks")]
    [Authorize(Roles = "Tutor,Admin")]
    public async Task<IActionResult> StartCheck([FromBody] StartCheckRequest request)
    {
        try
        {
            var tutorId = GetCurrentUserId();
            var result = await _attendanceService.StartCheckAsync(request.CourseId, tutorId, request.Title);
            return Ok(ApiResponse<AttendanceCheckDto>.Ok(result, "Attendance check started."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPut("checks/{id:guid}/end")]
    [Authorize(Roles = "Tutor,Admin")]
    public async Task<IActionResult> EndCheck(Guid id)
    {
        try
        {
            var tutorId = GetCurrentUserId();
            await _attendanceService.EndCheckAsync(id, tutorId);
            return Ok(ApiResponse<object>.Ok(null, "Attendance check ended."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("checks/{id:guid}/manual/{studentId:guid}")]
    [Authorize(Roles = "Tutor,Admin")]
    public async Task<IActionResult> ManualMark(Guid id, Guid studentId)
    {
        try
        {
            var tutorId = GetCurrentUserId();
            var isNowPresent = await _attendanceService.ManualMarkAsync(id, studentId, tutorId);

            // Push real-time update to all watchers of this check
            if (isNowPresent)
            {
                var details = await _attendanceService.GetCheckDetailsAsync(id);
                var record = details.Records.FirstOrDefault(r => r.StudentId == studentId);
                if (record is not null)
                {
                    var dto = new AttendanceMarkedDto
                    {
                        CheckId = id,
                        StudentId = studentId,
                        StudentName = record.StudentName,
                        StudentAvatarUrl = record.StudentAvatarUrl,
                        ScannedAt = record.ScannedAt,
                        IsManual = true
                    };
                    await _hubContext.Clients.Group($"attendance-check-{id}").SendAsync("AttendanceMarked", dto);
                    await _hubContext.Clients.Group($"attendance-tutor-{id}").SendAsync("AttendanceMarked", dto);
                }
            }

            return Ok(ApiResponse<bool>.Ok(isNowPresent, isNowPresent ? "Marked as present." : "Removed from present list."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("checks/{id:guid}")]
    [Authorize(Roles = "Tutor,Admin")]
    public async Task<IActionResult> GetCheckDetails(Guid id)
    {
        try
        {
            var result = await _attendanceService.GetCheckDetailsAsync(id);
            return Ok(ApiResponse<AttendanceCheckDetailDto>.Ok(result));
        }
        catch (Exception ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("checks/{id:guid}/current-qr")]
    [Authorize(Roles = "Tutor,Admin")]
    public async Task<IActionResult> GetCurrentQr(Guid id)
    {
        try
        {
            var result = await _attendanceService.GetCurrentQrPayloadAsync(id);
            return Ok(ApiResponse<QrUpdatedDto>.Ok(result));
        }
        catch (Exception ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("courses/{courseId:guid}/history")]
    [Authorize(Roles = "Tutor,Admin")]
    public async Task<IActionResult> GetCourseHistory(Guid courseId)
    {
        var result = await _attendanceService.GetCourseHistoryAsync(courseId);
        return Ok(ApiResponse<List<AttendanceCheckDto>>.Ok(result));
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllChecks()
    {
        var result = await _attendanceService.GetAllChecksAsync();
        return Ok(ApiResponse<List<AttendanceCheckDto>>.Ok(result));
    }

    // ── Student Endpoints ─────────────────────────────────────────────────────

    [HttpPost("checks/{id:guid}/scan")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Scan(Guid id, [FromBody] ScanRequest request)
    {
        try
        {
            var studentId = GetCurrentUserId();
            var record = await _attendanceService.VerifyAndMarkAsync(studentId, id, request.Secret);

            // Push real-time event to tutor and broadcast group
            var dto = new AttendanceMarkedDto
            {
                CheckId = id,
                StudentId = studentId,
                StudentName = record.StudentName,
                StudentAvatarUrl = record.StudentAvatarUrl,
                ScannedAt = record.ScannedAt,
                IsManual = false
            };
            await _hubContext.Clients.Group($"attendance-tutor-{id}").SendAsync("AttendanceMarked", dto);
            await _hubContext.Clients.Group($"attendance-check-{id}").SendAsync("AttendanceMarked", dto);

            return Ok(ApiResponse<AttendanceRecordDto>.Ok(record, "Attendance marked successfully!"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("my-history")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyHistory()
    {
        var studentId = GetCurrentUserId();
        var result = await _attendanceService.GetStudentHistoryAsync(studentId);
        return Ok(ApiResponse<List<StudentAttendanceHistoryDto>>.Ok(result));
    }
}
