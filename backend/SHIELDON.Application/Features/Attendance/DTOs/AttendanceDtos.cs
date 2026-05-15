namespace SHIELDON.Application.Features.Attendance.DTOs;

// ── Outbound DTOs ─────────────────────────────────────────────────────────────

public class AttendanceCheckDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string TutorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int TotalPresent { get; set; }
    public int TotalEnrolled { get; set; }
}

public class AttendanceCheckDetailDto : AttendanceCheckDto
{
    public List<AttendanceRecordDto> Records { get; set; } = new();
    public List<EnrolledStudentDto> AllEnrolledStudents { get; set; } = new();
}

public class AttendanceRecordDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? StudentAvatarUrl { get; set; }
    public DateTime ScannedAt { get; set; }
    public bool IsManual { get; set; }
}

public class EnrolledStudentDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsPresent { get; set; }
    public bool IsManual { get; set; }
}

public class StudentAttendanceHistoryDto
{
    public Guid CheckId { get; set; }
    public string CheckTitle { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public DateTime ScannedAt { get; set; }
    public bool IsManual { get; set; }
}

// ── Inbound DTOs ─────────────────────────────────────────────────────────────

public class StartCheckRequest
{
    public Guid CourseId { get; set; }
    public string? Title { get; set; }
}

public class ScanRequest
{
    public string Secret { get; set; } = string.Empty;
}

// ── SignalR Push DTOs ─────────────────────────────────────────────────────────

public class QrUpdatedDto
{
    public Guid CheckId { get; set; }
    /// <summary>Format: {checkId}|{secret}</summary>
    public string Payload { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class AttendanceMarkedDto
{
    public Guid CheckId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? StudentAvatarUrl { get; set; }
    public DateTime ScannedAt { get; set; }
    public bool IsManual { get; set; }
}
