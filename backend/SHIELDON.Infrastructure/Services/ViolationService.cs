using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Violations.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements violation persistence for the Anti-Cheating Engine (Phase 4).
///
/// Students report violations in batches via POST /api/violations/batch.
/// Tutors and Admins read those violations via the monitoring dashboard (Phase 5).
///
/// Strike score calculation (used by tutor dashboard for quick triage):
///   Minor = 0.25 | Medium = 0.5 | Critical = 1.0
///   Score ≥ 1.0 = Strike 1 | ≥ 2.0 = Strike 2 | ≥ 3.0 = Force-Submit
/// </summary>
public class ViolationService : IViolationService
{
    private readonly AppDbContext _db;

    public ViolationService(AppDbContext db)
    {
        _db = db;
    }

    // ── Constants ───────────────────────────────────────────────────────────────

    private static decimal GetSeverityWeight(ViolationSeverity severity) => severity switch
    {
        ViolationSeverity.Minor    => 0.25m,
        ViolationSeverity.Medium   => 0.5m,
        ViolationSeverity.Critical => 1.0m,
        _                          => 0.25m
    };

    // ── Student: Log Violation Batch ────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<ApiResponse<string>> LogViolationBatchAsync(
        BatchViolationRequest request,
        Guid studentId,
        CancellationToken ct = default)
    {
        if (request.Violations == null || request.Violations.Count == 0)
            return ApiResponse<string>.Ok("No violations to log.");

        // Validate all attemptIds belong to this student
        var attemptIds = request.Violations.Select(v => v.AttemptId).Distinct().ToList();

        var validAttempts = await _db.ExamAttempts
            .Include(a => a.Exam)
            .Where(a => attemptIds.Contains(a.Id) && a.StudentId == studentId)
            .ToListAsync(ct);

        var validAttemptIds = validAttempts.Select(a => a.Id).ToHashSet();

        var logsToInsert = new List<ViolationLog>();

        foreach (var v in request.Violations)
        {
            // Skip violations for attempts that don't belong to this student
            if (!validAttemptIds.Contains(v.AttemptId)) continue;

            var attempt = validAttempts.First(a => a.Id == v.AttemptId);

            logsToInsert.Add(new ViolationLog
            {
                AttemptId    = v.AttemptId,
                StudentId    = studentId,
                ExamId       = attempt.ExamId,
                CourseId     = attempt.Exam!.CourseId,
                Type         = v.Type,
                Severity     = v.Severity,
                Description  = v.Description.Length > 500 ? v.Description[..500] : v.Description,
                OccurredAt   = v.OccurredAt,
                WasAutoSubmit = v.WasAutoSubmit,
                CreatedAt    = DateTime.UtcNow
            });
        }

        if (logsToInsert.Count > 0)
        {
            _db.ViolationLogs.AddRange(logsToInsert);
            await _db.SaveChangesAsync(ct);
        }

        return ApiResponse<string>.Ok($"{logsToInsert.Count} violation(s) logged successfully.");
    }

    // ── Tutor/Admin: Get Violations for Attempt ──────────────────────────────────

    /// <inheritdoc/>
    public async Task<ApiResponse<AttemptViolationSummary>> GetViolationsForAttemptAsync(
        Guid attemptId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam)
                .ThenInclude(e => e!.Course)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw new NotFoundException("Exam attempt", attemptId);

        AuthorizeForCourse(attempt.Exam!.Course!, requestingUserId, requestingUserRole);

        var violations = await _db.ViolationLogs
            .Include(v => v.Student)
            .Where(v => v.AttemptId == attemptId)
            .OrderBy(v => v.OccurredAt)
            .AsNoTracking()
            .ToListAsync(ct);

        var student = await _db.Users.FindAsync(new object[] { attempt.StudentId }, ct);

        return ApiResponse<AttemptViolationSummary>.Ok(
            BuildSummary(attemptId, student, violations));
    }

    // ── Tutor/Admin: Get Violation Summary for Exam ──────────────────────────────

    /// <inheritdoc/>
    public async Task<ApiResponse<List<AttemptViolationSummary>>> GetViolationSummaryForExamAsync(
        Guid examId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var exam = await _db.Exams
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == examId, ct)
            ?? throw new NotFoundException("Exam", examId);

        AuthorizeForCourse(exam.Course!, requestingUserId, requestingUserRole);

        // Load all violations for all attempts of this exam
        var violations = await _db.ViolationLogs
            .Include(v => v.Student)
            .Where(v => v.ExamId == examId)
            .OrderBy(v => v.OccurredAt)
            .AsNoTracking()
            .ToListAsync(ct);

        // Group by attempt
        var attemptIds = violations.Select(v => v.AttemptId).Distinct().ToList();

        // Also include attempts with zero violations (students who were clean)
        var allAttempts = await _db.ExamAttempts
            .Include(a => a.Student)
            .Where(a => a.ExamId == examId)
            .AsNoTracking()
            .ToListAsync(ct);

        var summaries = allAttempts.Select(attempt =>
        {
            var attemptViolations = violations.Where(v => v.AttemptId == attempt.Id).ToList();
            return BuildSummary(attempt.Id, attempt.Student, attemptViolations);
        })
        .OrderByDescending(s => s.StrikeScore)
        .ToList();

        return ApiResponse<List<AttemptViolationSummary>>.Ok(summaries);
    }

    // ── Private Helpers ──────────────────────────────────────────────────────────

    private static void AuthorizeForCourse(Domain.Entities.Course course, Guid userId, string role)
    {
        if (role == "Tutor" && course.AssignedTutorId != userId)
            throw new ForbiddenException("You can only view violations for courses assigned to you.");
    }

    private static AttemptViolationSummary BuildSummary(
        Guid attemptId,
        User? student,
        List<ViolationLog> violations)
    {
        var critical = violations.Count(v => v.Severity == ViolationSeverity.Critical);
        var medium   = violations.Count(v => v.Severity == ViolationSeverity.Medium);
        var minor    = violations.Count(v => v.Severity == ViolationSeverity.Minor);

        var strikeScore = violations.Sum(v => GetSeverityWeight(v.Severity));
        var wasForceSubmitted = violations.Any(v => v.WasAutoSubmit);

        var studentName      = student != null ? $"{student.FirstName} {student.LastName}" : "Unknown";
        var studentDisplayId = student?.StudentId ?? student?.TutorId ?? "—";

        return new AttemptViolationSummary(
            AttemptId:        attemptId,
            StudentId:        student?.Id ?? Guid.Empty,
            StudentName:      studentName,
            StudentDisplayId: studentDisplayId,
            TotalViolations:  violations.Count,
            CriticalCount:    critical,
            MediumCount:      medium,
            MinorCount:       minor,
            StrikeScore:      Math.Round(strikeScore, 2),
            WasForceSubmitted: wasForceSubmitted,
            Violations:       violations.Select(v => MapToResponse(v, studentName, studentDisplayId)).ToList()
        );
    }

    private static ViolationLogResponse MapToResponse(
        ViolationLog v,
        string studentName,
        string studentDisplayId) => new(
            Id:               v.Id,
            AttemptId:        v.AttemptId,
            StudentId:        v.StudentId,
            StudentName:      studentName,
            StudentDisplayId: studentDisplayId,
            ExamId:           v.ExamId,
            ExamTitle:        v.Exam?.Title ?? string.Empty,
            Type:             v.Type.ToString(),
            Severity:         v.Severity.ToString(),
            Description:      v.Description,
            OccurredAt:       v.OccurredAt,
            WasAutoSubmit:    v.WasAutoSubmit,
            CreatedAt:        v.CreatedAt
        );
}
