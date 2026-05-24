using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Attendance.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _db;

    public AttendanceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AttendanceCheckDto> StartCheckAsync(Guid courseId, Guid tutorId, string? title)
    {
        // End any currently active check for this course first
        var existing = await _db.AttendanceChecks
            .Where(c => c.CourseId == courseId && c.IsActive)
            .ToListAsync();

        foreach (var old in existing)
            old.IsActive = false;

        var check = new AttendanceCheck
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            TutorId = tutorId,
            Title = string.IsNullOrWhiteSpace(title)
                ? $"Attendance Check - {DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC"
                : title.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            CurrentSecret = Guid.NewGuid().ToString("N"),
            SecretExpiresAt = DateTime.UtcNow.AddSeconds(15)
        };

        _db.AttendanceChecks.Add(check);
        await _db.SaveChangesAsync();

        var course = await _db.Courses.FindAsync(courseId);
        return MapToDto(check, course?.Title ?? "", "");
    }

    public async Task EndCheckAsync(Guid checkId, Guid tutorId)
    {
        var check = await _db.AttendanceChecks
            .FirstOrDefaultAsync(c => c.Id == checkId && c.TutorId == tutorId)
            ?? throw new KeyNotFoundException("Attendance check not found or not owned by this tutor.");

        check.IsActive = false;
        await _db.SaveChangesAsync();
    }

    public async Task<AttendanceRecordDto> VerifyAndMarkAsync(Guid studentId, Guid checkId, string secret)
    {
        var check = await _db.AttendanceChecks
            .FirstOrDefaultAsync(c => c.Id == checkId && c.IsActive)
            ?? throw new InvalidOperationException("Attendance check is not active or does not exist.");

        // Validate the secret is current and not expired
        if (check.CurrentSecret != secret || DateTime.UtcNow > check.SecretExpiresAt)
            throw new InvalidOperationException("QR code has expired. Please scan the latest code.");

        // Check for duplicate scan
        var alreadyMarked = await _db.AttendanceRecords
            .AnyAsync(r => r.AttendanceCheckId == checkId && r.StudentId == studentId);

        if (alreadyMarked)
            throw new InvalidOperationException("You have already scanned attendance for this check.");

        var record = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            AttendanceCheckId = checkId,
            StudentId = studentId,
            ScannedAt = DateTime.UtcNow,
            IsManual = false
        };

        _db.AttendanceRecords.Add(record);
        await _db.SaveChangesAsync();

        var student = await _db.Users.FindAsync(studentId);
        return new AttendanceRecordDto
        {
            Id = record.Id,
            StudentId = studentId,
            StudentName = student?.FullName ?? "",
            StudentAvatarUrl = student?.ProfilePictureUrl,
            ScannedAt = record.ScannedAt,
            IsManual = false
        };
    }

    public async Task<bool> ManualMarkAsync(Guid checkId, Guid studentId, Guid tutorId)
    {
        var check = await _db.AttendanceChecks
            .FirstOrDefaultAsync(c => c.Id == checkId && c.TutorId == tutorId)
            ?? throw new KeyNotFoundException("Attendance check not found or not owned by this tutor.");

        var existing = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.AttendanceCheckId == checkId && r.StudentId == studentId);

        if (existing is not null)
        {
            // Toggle: remove the record (un-mark)
            _db.AttendanceRecords.Remove(existing);
            await _db.SaveChangesAsync();
            return false;
        }

        var record = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            AttendanceCheckId = checkId,
            StudentId = studentId,
            ScannedAt = DateTime.UtcNow,
            IsManual = true
        };

        _db.AttendanceRecords.Add(record);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<AttendanceCheckDetailDto> GetCheckDetailsAsync(Guid checkId)
    {
        var check = await _db.AttendanceChecks
            .Include(c => c.Course)
            .Include(c => c.Tutor)
            .Include(c => c.Records)
                .ThenInclude(r => r.Student)
            .FirstOrDefaultAsync(c => c.Id == checkId)
            ?? throw new KeyNotFoundException("Attendance check not found.");

        // Get all enrolled students in the course
        var enrolledStudents = await _db.CourseEnrollments
            .Where(e => e.CourseId == check.CourseId)
            .Include(e => e.Student)
            .Select(e => e.Student!)
            .ToListAsync();

        var records = check.Records.Select(r => new AttendanceRecordDto
        {
            Id = r.Id,
            StudentId = r.StudentId,
            StudentName = r.Student.FullName,
            StudentAvatarUrl = r.Student.ProfilePictureUrl,
            ScannedAt = r.ScannedAt,
            IsManual = r.IsManual
        }).ToList();

        var markedIds = check.Records.Select(r => r.StudentId).ToHashSet();
        var manualIds = check.Records.Where(r => r.IsManual).Select(r => r.StudentId).ToHashSet();

        var allStudents = enrolledStudents.Select(s => new EnrolledStudentDto
        {
            Id = s.Id,
            FullName = s.FullName,
            AvatarUrl = s.ProfilePictureUrl,
            IsPresent = markedIds.Contains(s.Id),
            IsManual = manualIds.Contains(s.Id)
        }).ToList();

        return new AttendanceCheckDetailDto
        {
            Id = check.Id,
            CourseId = check.CourseId,
            CourseName = check.Course.Title,
            TutorName = check.Tutor.FullName,
            Title = check.Title,
            CreatedAt = check.CreatedAt,
            IsActive = check.IsActive,
            TotalPresent = records.Count,
            TotalEnrolled = enrolledStudents.Count,
            Records = records,
            AllEnrolledStudents = allStudents
        };
    }

    public async Task<List<AttendanceCheckDto>> GetCourseHistoryAsync(Guid courseId)
    {
        var checks = await _db.AttendanceChecks
            .Where(c => c.CourseId == courseId)
            .Include(c => c.Course)
            .Include(c => c.Tutor)
            .Include(c => c.Records)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var enrolledCount = await _db.CourseEnrollments.CountAsync(e => e.CourseId == courseId);

        return checks.Select(c => new AttendanceCheckDto
        {
            Id = c.Id,
            CourseId = c.CourseId,
            CourseName = c.Course.Title,
            TutorName = c.Tutor.FullName,
            Title = c.Title,
            CreatedAt = c.CreatedAt,
            IsActive = c.IsActive,
            TotalPresent = c.Records.Count,
            TotalEnrolled = enrolledCount
        }).ToList();
    }

    public async Task<List<StudentAttendanceHistoryDto>> GetStudentHistoryAsync(Guid studentId)
    {
        return await _db.AttendanceRecords
            .Where(r => r.StudentId == studentId)
            .Include(r => r.AttendanceCheck)
                .ThenInclude(c => c.Course)
            .OrderByDescending(r => r.ScannedAt)
            .Select(r => new StudentAttendanceHistoryDto
            {
                CheckId = r.AttendanceCheckId,
                CheckTitle = r.AttendanceCheck.Title,
                CourseName = r.AttendanceCheck.Course.Title,
                CourseId = r.AttendanceCheck.CourseId,
                ScannedAt = r.ScannedAt,
                IsManual = r.IsManual
            })
            .ToListAsync();
    }

    public async Task<List<AttendanceCheckDto>> GetAllChecksAsync()
    {
        var checks = await _db.AttendanceChecks
            .Include(c => c.Course)
            .Include(c => c.Tutor)
            .Include(c => c.Records)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var courseIds = checks.Select(c => c.CourseId).Distinct().ToList();
        var enrolledCounts = await _db.CourseEnrollments
            .Where(e => courseIds.Contains(e.CourseId))
            .GroupBy(e => e.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count);

        return checks.Select(c => new AttendanceCheckDto
        {
            Id = c.Id,
            CourseId = c.CourseId,
            CourseName = c.Course.Title,
            TutorName = c.Tutor.FullName,
            Title = c.Title,
            CreatedAt = c.CreatedAt,
            IsActive = c.IsActive,
            TotalPresent = c.Records.Count,
            TotalEnrolled = enrolledCounts.GetValueOrDefault(c.CourseId, 0)
        }).ToList();
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    public async Task<QrUpdatedDto> GetCurrentQrPayloadAsync(Guid checkId)
    {
        var check = await _db.AttendanceChecks
            .FirstOrDefaultAsync(c => c.Id == checkId && c.IsActive)
            ?? throw new KeyNotFoundException("Attendance check not found or not active.");

        var payload = $"{check.Id}|{check.CurrentSecret}";
        return new QrUpdatedDto
        {
            CheckId = check.Id,
            Payload = payload,
            ExpiresAt = check.SecretExpiresAt
        };
    }

    public static string GenerateSecret() => Guid.NewGuid().ToString("N");

    private static AttendanceCheckDto MapToDto(AttendanceCheck c, string courseName, string tutorName) =>
        new()
        {
            Id = c.Id,
            CourseId = c.CourseId,
            CourseName = courseName,
            TutorName = tutorName,
            Title = c.Title,
            CreatedAt = c.CreatedAt,
            IsActive = c.IsActive,
            TotalPresent = 0,
            TotalEnrolled = 0
        };
}
