using SHIELDON.Application.Features.Attendance.DTOs;

namespace SHIELDON.Application.Interfaces;

public interface IAttendanceService
{
    /// <summary>Tutor starts a new QR attendance check for a course.</summary>
    Task<AttendanceCheckDto> StartCheckAsync(Guid courseId, Guid tutorId, string? title);

    /// <summary>Tutor ends an active check, making the QR invalid.</summary>
    Task EndCheckAsync(Guid checkId, Guid tutorId);

    /// <summary>
    /// Student submits a scanned QR secret. Validates against the DB and marks attendance.
    /// Returns the created record on success.
    /// </summary>
    Task<AttendanceRecordDto> VerifyAndMarkAsync(Guid studentId, Guid checkId, string secret);

    /// <summary>Tutor manually toggles a student's presence. Returns the new IsPresent state.</summary>
    Task<bool> ManualMarkAsync(Guid checkId, Guid studentId, Guid tutorId);

    /// <summary>Returns full details of a check including all records and enrolled students.</summary>
    Task<AttendanceCheckDetailDto> GetCheckDetailsAsync(Guid checkId);

    /// <summary>Returns all past checks for a course (tutor/admin view).</summary>
    Task<List<AttendanceCheckDto>> GetCourseHistoryAsync(Guid courseId);

    /// <summary>Returns a student's personal attendance history across all enrolled courses.</summary>
    Task<List<StudentAttendanceHistoryDto>> GetStudentHistoryAsync(Guid studentId);

    /// <summary>Admin: Returns all attendance checks across all courses.</summary>
    Task<List<AttendanceCheckDto>> GetAllChecksAsync();

    /// <summary>Tutor: Returns the current QR payload for an active check so the UI can render immediately.</summary>
    Task<QrUpdatedDto> GetCurrentQrPayloadAsync(Guid checkId);
}
