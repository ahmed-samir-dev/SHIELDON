using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Monitoring.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements all Phase 5 monitoring operations:
/// presence tracking, session timeline, violation summaries,
/// tutor/admin dashboards, manual review decisions, and live termination.
/// </summary>
public class MonitoringService : IMonitoringService
{
    private readonly AppDbContext _db;

    public MonitoringService(AppDbContext db)
    {
        _db = db;
    }

    // ── Heartbeat ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task LogHeartbeatAsync(Guid attemptId, Guid studentId)
    {
        var attempt = await _db.ExamAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId)
            ?? throw new NotFoundException("Exam attempt", attemptId);

        if (attempt.Status != AttemptStatus.InProgress)
            return; // Silently ignore heartbeats for closed attempts

        var now = DateTime.UtcNow;
        var examId = attempt.ExamId;

        // Load ExamId for denormalization if not already loaded
        var exam = await _db.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId)
            ?? throw new NotFoundException("Exam", examId);

        // Update last heartbeat timestamp
        attempt.LastHeartbeatAt = now;

        // Log HeartbeatReceived presence event
        _db.PresenceLogs.Add(new PresenceLog
        {
            AttemptId  = attemptId,
            StudentId  = studentId,
            ExamId     = examId,
            CourseId   = exam.CourseId,
            EventType  = PresenceEventType.HeartbeatReceived,
            OccurredAt = now,
            CreatedAt  = now
        });

        await _db.SaveChangesAsync();
    }

    // ── Session Timeline ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<List<TimelineEventResponse>> GetTimelineAsync(
        Guid attemptId, Guid requesterId, string requesterRole)
    {
        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam).ThenInclude(e => e!.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new NotFoundException("Exam attempt", attemptId);

        AuthorizeForCourse(attempt.Exam!.Course!, requesterId, requesterRole);

        // Load presence events
        var presenceLogs = await _db.PresenceLogs
            .Where(p => p.AttemptId == attemptId)
            .AsNoTracking()
            .ToListAsync();

        // Load violation logs
        var violationLogs = await _db.ViolationLogs
            .Where(v => v.AttemptId == attemptId)
            .AsNoTracking()
            .ToListAsync();

        // Map and merge
        var timeline = new List<TimelineEventResponse>();

        timeline.AddRange(presenceLogs
            .Where(p => p.EventType != PresenceEventType.HeartbeatReceived) // Skip heartbeat noise
            .Select(p => new TimelineEventResponse
            {
                OccurredAt   = p.OccurredAt,
                Category     = "Presence",
                EventType    = p.EventType.ToString(),
                Severity     = "Info",
                Description  = BuildPresenceDescription(p),
                WasAutoSubmit = false
            }));

        timeline.AddRange(violationLogs.Select(v => new TimelineEventResponse
        {
            OccurredAt   = v.OccurredAt,
            Category     = "Violation",
            EventType    = v.Type.ToString(),
            Severity     = v.Severity.ToString(),
            Description  = v.Description,
            WasAutoSubmit = v.WasAutoSubmit
        }));

        return [.. timeline.OrderBy(e => e.OccurredAt)];
    }

    // ── Violation Summary ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<ViolationSummaryResponse> GetViolationSummaryAsync(
        Guid attemptId, Guid requesterId, string requesterRole)
    {
        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam).ThenInclude(e => e!.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new NotFoundException("Exam attempt", attemptId);

        AuthorizeForCourse(attempt.Exam!.Course!, requesterId, requesterRole);

        var violations = await _db.ViolationLogs
            .Where(v => v.AttemptId == attemptId)
            .OrderBy(v => v.OccurredAt)
            .AsNoTracking()
            .ToListAsync();

        var critical = violations.Count(v => v.Severity == ViolationSeverity.Critical);
        var medium   = violations.Count(v => v.Severity == ViolationSeverity.Medium);
        var minor    = violations.Count(v => v.Severity == ViolationSeverity.Minor);

        // Build chart data: group violations by minute offset from attempt start
        var chartData = violations
            .GroupBy(v => (int)(v.OccurredAt - attempt.StartedAt).TotalMinutes)
            .OrderBy(g => g.Key)
            .Select(g => new ViolationChartPoint
            {
                MinuteOffset  = g.Key,
                CriticalCount = g.Count(v => v.Severity == ViolationSeverity.Critical),
                MediumCount   = g.Count(v => v.Severity == ViolationSeverity.Medium),
                MinorCount    = g.Count(v => v.Severity == ViolationSeverity.Minor)
            })
            .ToList();

        // Determine submission type
        var submissionType = attempt.Status switch
        {
            AttemptStatus.ForceSubmitted => "ForceSubmitted",
            AttemptStatus.Submitted      => violations.Any(v => v.WasAutoSubmit)
                                            ? "AutoExpired" : "Manual",
            _                            => "InProgress"
        };

        return new ViolationSummaryResponse
        {
            TotalViolations = violations.Count,
            CriticalCount   = critical,
            MediumCount     = medium,
            MinorCount      = minor,
            SubmissionType  = submissionType,
            ChartData       = chartData,
            Violations      = violations.Select(v => new ViolationTableRow
            {
                OccurredAt    = v.OccurredAt,
                Type          = v.Type.ToString(),
                Severity      = v.Severity.ToString(),
                Description   = v.Description,
                WasAutoSubmit = v.WasAutoSubmit
            }).ToList()
        };
    }

    // ── Tutor Dashboard ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<TutorDashboardResponse> GetTutorDashboardAsync(Guid tutorId)
    {
        // Find all courses assigned to this tutor
        var courseIds = await _db.Courses
            .Where(c => c.AssignedTutorId == tutorId && c.IsActive)
            .Select(c => c.Id)
            .ToListAsync();

        // Active/published exams in those courses
        var exams = await _db.Exams
            .Where(e => courseIds.Contains(e.CourseId) && e.Status == ExamStatus.Published)
            .Include(e => e.Course)
            .AsNoTracking()
            .ToListAsync();

        var examIds = exams.Select(e => e.Id).ToList();

        // All in-progress and recently submitted attempts for these exams
        var attempts = await _db.ExamAttempts
            .Include(a => a.Student)
            .Include(a => a.Exam)
            .Where(a => examIds.Contains(a.ExamId))
            .AsNoTracking()
            .ToListAsync();

        // Violation counts per attempt
        var violationCounts = await _db.ViolationLogs
            .Where(v => examIds.Contains(v.ExamId))
            .GroupBy(v => v.AttemptId)
            .Select(g => new { AttemptId = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToListAsync();

        var violationLookup = violationCounts.ToDictionary(v => v.AttemptId, v => v.Count);

        // Enrolled student counts per exam (for "Not Started" calculation)
        var enrolledCounts = await _db.CourseEnrollments
            .Where(e => courseIds.Contains(e.CourseId) && e.Status == CourseEnrollmentStatus.Approved)
            .GroupBy(e => e.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToListAsync();

        var enrolledLookup = enrolledCounts.ToDictionary(e => e.CourseId, e => e.Count);

        // Build active exam summaries
        var activeExams = exams.Select(exam =>
        {
            var examAttempts = attempts.Where(a => a.ExamId == exam.Id).ToList();
            var enrolled     = enrolledLookup.GetValueOrDefault(exam.CourseId, 0);
            var started      = examAttempts.Count;

            return new ActiveExamSummary
            {
                ExamId             = exam.Id,
                ExamTitle          = exam.Title,
                CourseTitle        = exam.Course?.Title ?? "",
                InProgressCount    = examAttempts.Count(a => a.Status == AttemptStatus.InProgress),
                SubmittedCount     = examAttempts.Count(a => a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.Graded),
                ForceSubmittedCount = examAttempts.Count(a => a.Status == AttemptStatus.ForceSubmitted),
                NotStartedCount    = Math.Max(0, enrolled - started)
            };
        }).ToList();

        // Build live session grid
        var disconnectThreshold = DateTime.UtcNow.AddSeconds(-45);
        var reviewDecisionAttemptIds = await _db.ReviewDecisions
            .Where(r => examIds.Contains(r.Attempt!.ExamId))
            .Select(r => r.AttemptId)
            .ToListAsync();
        var reviewedSet = reviewDecisionAttemptIds.ToHashSet();

        var liveSessions = attempts.Select(a =>
        {
            var isDisconnected = a.Status == AttemptStatus.InProgress
                && a.LastHeartbeatAt.HasValue
                && a.LastHeartbeatAt < disconnectThreshold;

            var displayStatus = a.Status switch
            {
                AttemptStatus.InProgress     => isDisconnected ? "Disconnected" : "InProgress",
                AttemptStatus.Submitted      => "Submitted",
                AttemptStatus.Graded         => "Submitted",
                AttemptStatus.ForceSubmitted  => "ForceSubmitted",
                _                            => "Unknown"
            };

            return new LiveSessionRow
            {
                AttemptId        = a.Id,
                StudentId        = a.StudentId,
                StudentName      = $"{a.Student?.FirstName} {a.Student?.LastName}",
                StudentCode      = a.Student?.StudentId ?? "—",
                ExamTitle        = a.Exam?.Title ?? "",
                Status           = displayStatus,
                ViolationCount   = violationLookup.GetValueOrDefault(a.Id, 0),
                StartedAt        = a.StartedAt,
                LastHeartbeatAt  = a.LastHeartbeatAt,
                HasReviewDecision = reviewedSet.Contains(a.Id)
            };
        })
        .OrderByDescending(s => s.ViolationCount)
        .ToList();

        // Violation type distribution for ECharts doughnut
        var violationTypes = await _db.ViolationLogs
            .Where(v => examIds.Contains(v.ExamId))
            .GroupBy(v => v.Type)
            .Select(g => new ViolationTypeStat
            {
                ViolationType = g.Key.ToString(),
                Count         = g.Count()
            })
            .AsNoTracking()
            .ToListAsync();

        return new TutorDashboardResponse
        {
            ActiveExams          = activeExams,
            LiveSessions         = liveSessions,
            ViolationDistribution = new ViolationTypeDistribution { Items = violationTypes }
        };
    }

    // ── Admin Dashboard ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<AdminDashboardResponse> GetAdminDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var thirtyDaysAgo = now.AddDays(-30);

        // KPIs
        var totalActiveCourses    = await _db.Courses.CountAsync(c => c.IsActive);
        var totalOngoingExams     = await _db.ExamAttempts.CountAsync(a => a.Status == AttemptStatus.InProgress);
        var totalEnrolled         = await _db.CourseEnrollments.CountAsync(e => e.Status == CourseEnrollmentStatus.Approved);
        var totalViolationsToday  = await _db.ViolationLogs.CountAsync(v => v.CreatedAt >= today);
        var totalForceSubmitted   = await _db.ExamAttempts.CountAsync(a => a.Status == AttemptStatus.ForceSubmitted && a.SubmittedAt >= today);

        // Global exam monitor: active exams across all courses
        var activeExamSessions = await _db.ExamAttempts
            .Where(a => a.Status == AttemptStatus.InProgress)
            .Include(a => a.Exam).ThenInclude(e => e!.Course)
            .Include(a => a.Exam).ThenInclude(e => e!.CreatedByUser)
            .AsNoTracking()
            .GroupBy(a => a.ExamId)
            .Select(g => new GlobalExamRow
            {
                ExamId           = g.Key,
                ExamTitle        = g.First().Exam!.Title,
                CourseTitle      = g.First().Exam!.Course!.Title,
                TutorName        = $"{g.First().Exam!.CreatedByUser!.FirstName} {g.First().Exam!.CreatedByUser!.LastName}",
                StudentsInProgress = g.Count()
            })
            .ToListAsync();

        // Violation counts per exam (for global monitor table)
        var examViolationCounts = await _db.ViolationLogs
            .GroupBy(v => v.ExamId)
            .Select(g => new { ExamId = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToListAsync();
        var examViolationLookup = examViolationCounts.ToDictionary(e => e.ExamId, e => e.Count);

        foreach (var row in activeExamSessions)
            row.TotalViolations = examViolationLookup.GetValueOrDefault(row.ExamId, 0);

        // Top violation types for ECharts horizontal bar
        var topViolationTypes = await _db.ViolationLogs
            .GroupBy(v => v.Type)
            .Select(g => new ViolationTypeStat
            {
                ViolationType = g.Key.ToString(),
                Count         = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .Take(10)
            .AsNoTracking()
            .ToListAsync();

        // 30-day activity trend for ECharts line chart
        var activityTrend = await _db.ExamAttempts
            .Where(a => a.StartedAt >= thirtyDaysAgo)
            .GroupBy(a => a.StartedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToListAsync();

        var violationsByDay = await _db.ViolationLogs
            .Where(v => v.CreatedAt >= thirtyDaysAgo)
            .GroupBy(v => v.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToListAsync();

        var violationDayLookup = violationsByDay.ToDictionary(v => v.Date, v => v.Count);

        var dailyPoints = activityTrend.Select(d => new DailyActivityPoint
        {
            Date           = DateOnly.FromDateTime(d.Date),
            ExamCount      = d.Count,
            ViolationCount = violationDayLookup.GetValueOrDefault(d.Date, 0)
        })
        .OrderBy(d => d.Date)
        .ToList();

        // Suspicious submission rate
        var totalSubmitted    = await _db.ExamAttempts.CountAsync(a =>
            a.Status == AttemptStatus.Submitted ||
            a.Status == AttemptStatus.Graded ||
            a.Status == AttemptStatus.ForceSubmitted);
        var forceTotal = await _db.ExamAttempts.CountAsync(a => a.Status == AttemptStatus.ForceSubmitted);
        var suspiciousRate = totalSubmitted > 0
            ? Math.Round((decimal)forceTotal / totalSubmitted * 100, 1)
            : 0m;

        return new AdminDashboardResponse
        {
            TotalActiveCourses              = totalActiveCourses,
            TotalOngoingExams               = totalOngoingExams,
            TotalEnrolledStudents           = totalEnrolled,
            TotalViolationsToday            = totalViolationsToday,
            TotalForceSubmittedToday        = totalForceSubmitted,
            ActiveExamSessions              = activeExamSessions,
            TopViolationTypes               = topViolationTypes,
            ActivityTrend                   = dailyPoints,
            SuspiciousSubmissionRatePercent = suspiciousRate
        };
    }

    // ── Manual Review Decision ───────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<ReviewDecisionResponse> SubmitReviewDecisionAsync(
        Guid attemptId, ReviewDecisionRequest request, Guid reviewerId)
    {
        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam).ThenInclude(e => e!.Course)
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new NotFoundException("Exam attempt", attemptId);

        // Prevent duplicate reviews
        var existing = await _db.ReviewDecisions.FirstOrDefaultAsync(r => r.AttemptId == attemptId);
        if (existing != null)
            throw new InvalidOperationException("A review decision has already been submitted for this attempt.");

        var now = DateTime.UtcNow;

        // Handle decision side-effects
        if (request.Decision == ReviewDecisionType.MarkedAsCheating)
        {
            // Zero out the GradeRecord for this attempt
            var gradeRecord = await _db.GradeRecords
                .FirstOrDefaultAsync(g => g.ExamId == attempt.ExamId && g.StudentId == attempt.StudentId);

            if (gradeRecord != null)
            {
                gradeRecord.Score      = 0;
                gradeRecord.UpdatedAt  = now;
            }
        }
        else if (request.Decision == ReviewDecisionType.ReAttemptGranted)
        {
            // Create an approved re-attempt request
            _db.ReattemptRequests.Add(new ReattemptRequest
            {
                StudentId     = attempt.StudentId,
                ExamId        = attempt.ExamId,
                Justification = $"Re-attempt granted by reviewer as part of manual review decision. Notes: {request.Notes ?? "None"}",
                Status        = "Approved",
                RequestedAt   = now,
                ReviewedAt    = now,
                ReviewedById  = reviewerId
            });

        }

        // Save review decision
        var decision = new ReviewDecision
        {
            AttemptId  = attemptId,
            ReviewerId = reviewerId,
            Decision   = request.Decision,
            Notes      = request.Notes,
            ReviewedAt = now
        };

        _db.ReviewDecisions.Add(decision);
        await _db.SaveChangesAsync();

        return new ReviewDecisionResponse
        {
            DecisionId  = decision.Id,
            Decision    = decision.Decision.ToString(),
            Notes       = decision.Notes,
            ReviewedAt  = decision.ReviewedAt
        };
    }

    // ── Live Termination ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task TerminateSessionAsync(Guid attemptId, Guid terminatorId, string? reason)
    {
        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new NotFoundException("Exam attempt", attemptId);

        if (attempt.Status != AttemptStatus.InProgress)
            throw new InvalidOperationException("Only active (InProgress) sessions can be terminated.");

        var terminator = await _db.Users.FindAsync(terminatorId);
        var terminatorName = terminator != null
            ? $"{terminator.FirstName} {terminator.LastName}"
            : "Unknown";

        var now = DateTime.UtcNow;

        // Force-submit the attempt
        attempt.Status      = AttemptStatus.ForceSubmitted;
        attempt.SubmittedAt = now;

        // Log TutorTerminated presence event
        _db.PresenceLogs.Add(new PresenceLog
        {
            AttemptId  = attemptId,
            StudentId  = attempt.StudentId,
            ExamId     = attempt.ExamId,
            CourseId   = attempt.Exam!.CourseId,
            EventType  = PresenceEventType.TutorTerminated,
            Detail     = string.IsNullOrWhiteSpace(reason)
                         ? $"Session manually terminated by {terminatorName}."
                         : $"Session terminated by {terminatorName}. Reason: {reason}",
            OccurredAt = now,
            CreatedAt  = now
        });

        await _db.SaveChangesAsync();
    }

    // ── Private Helpers ──────────────────────────────────────────────────────────

    private static void AuthorizeForCourse(Domain.Entities.Course course, Guid userId, string role)
    {
        if (role == "Tutor" && course.AssignedTutorId != userId)
            throw new ForbiddenException("You can only view monitoring data for courses assigned to you.");
    }

    private static string BuildPresenceDescription(PresenceLog p) => p.EventType switch
    {
        PresenceEventType.ExamStarted      => "Student started the exam.",
        PresenceEventType.PageRefreshed    => "Student reloaded the exam page — session restored.",
        PresenceEventType.HeartbeatReceived => "Heartbeat received — student is active.",
        PresenceEventType.Disconnected     => "No heartbeat received for 45+ seconds — student appears disconnected.",
        PresenceEventType.Reconnected      => "Student reconnected after disconnection.",
        PresenceEventType.ExamSubmitted    => "Student voluntarily submitted the exam.",
        PresenceEventType.ForceSubmitted   => "Exam was automatically submitted by the anti-cheat engine.",
        PresenceEventType.AutoExpired      => "Exam was auto-submitted because the time limit expired.",
        PresenceEventType.UnexpectedExit   => "Student's session ended unexpectedly (browser crash or close).",
        PresenceEventType.TutorTerminated  => p.Detail ?? "Session was manually terminated by a tutor/admin.",
        _                                  => p.Detail ?? p.EventType.ToString()
    };
}
