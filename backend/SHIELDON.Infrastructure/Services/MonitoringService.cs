using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Monitoring.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements all monitoring operations:
/// - ProcessHeartbeatAsync: updates heartbeat timestamp, logs presence events
/// - GetTimelineAsync: merges violations + presence logs into one chronological feed
/// - GetViolationSummaryAsync, GetTutorDashboardAsync, GetAdminDashboardAsync (historical dashboards)
/// </summary>
public class MonitoringService : IMonitoringService
{
    private readonly AppDbContext _db;

    public MonitoringService(AppDbContext db)
    {
        _db = db;
    }

    // ── Heartbeat / Presence ──────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task ProcessHeartbeatAsync(Guid attemptId, Guid studentId, bool isPageRefresh)
    {
        var attempt = await _db.ExamAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId && a.Status == AttemptStatus.InProgress)
            ?? throw new NotFoundException("Exam attempt", attemptId);

        var now = DateTime.UtcNow;
        var wasDisconnected = attempt.IsDisconnected;

        // Always update the heartbeat timestamp
        attempt.LastHeartbeatAt = now;

        // If the student was previously flagged as disconnected, log a Reconnected event
        if (wasDisconnected)
        {
            attempt.IsDisconnected = false;
            _db.PresenceLogs.Add(new PresenceLog
            {
                AttemptId  = attemptId,
                StudentId  = studentId,
                ExamId     = attempt.ExamId,
                EventType  = PresenceEventType.Reconnected,
                OccurredAt = now
            });
        }

        // If frontend signals a page refresh, always log it (even if not previously disconnected)
        if (isPageRefresh)
        {
            _db.PresenceLogs.Add(new PresenceLog
            {
                AttemptId  = attemptId,
                StudentId  = studentId,
                ExamId     = attempt.ExamId,
                EventType  = PresenceEventType.PageRefreshed,
                OccurredAt = now
            });
        }

        await _db.SaveChangesAsync();
    }

    // ── Session Timeline ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<AttemptTimelineResponse> GetTimelineAsync(
        Guid attemptId, Guid requesterId, string requesterRole)
    {
        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam).ThenInclude(e => e!.Course)
            .Include(a => a.Student)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new NotFoundException("Exam attempt", attemptId);

        AuthorizeForCourse(attempt.Exam!.Course!, requesterId, requesterRole);

        // Load violation logs
        var violationLogs = await _db.ViolationLogs
            .Where(v => v.AttemptId == attemptId)
            .OrderBy(v => v.OccurredAt)
            .AsNoTracking()
            .ToListAsync();

        // Load presence logs
        var presenceLogs = await _db.PresenceLogs
            .Where(p => p.AttemptId == attemptId)
            .OrderBy(p => p.OccurredAt)
            .AsNoTracking()
            .ToListAsync();

        var critical = violationLogs.Count(v => v.Severity == ViolationSeverity.Critical);
        var medium   = violationLogs.Count(v => v.Severity == ViolationSeverity.Medium);
        var minor    = violationLogs.Count(v => v.Severity == ViolationSeverity.Minor);

        // Merge violations and presence events into a single chronological list
        var violationEntries = violationLogs.Select(v => new TimelineEntry
        {
            Category     = "Violation",
            EventType    = v.Type.ToString(),
            Severity     = v.Severity.ToString(),
            Description  = v.Description,
            OccurredAt   = v.OccurredAt,
            WasAutoSubmit = v.WasAutoSubmit
        });

        var presenceEntries = presenceLogs.Select(p => new TimelineEntry
        {
            Category     = "Presence",
            EventType    = p.EventType.ToString(),
            Severity     = string.Empty,
            Description  = GetPresenceDescription(p.EventType),
            OccurredAt   = p.OccurredAt,
            WasAutoSubmit = false
        });

        var mergedEvents = violationEntries
            .Concat(presenceEntries)
            .OrderBy(e => e.OccurredAt)
            .ToList();

        return new AttemptTimelineResponse
        {
            AttemptId        = attempt.Id,
            StudentName      = attempt.Student != null ? $"{attempt.Student.FirstName} {attempt.Student.LastName}" : "Unknown",
            StudentCode      = attempt.Student?.StudentId ?? "-",
            StudentProfilePictureUrl = attempt.Student?.ProfilePictureUrl,
            ExamTitle        = attempt.Exam.Title,
            CourseTitle      = attempt.Exam.Course!.Title,
            StartedAt        = attempt.StartedAt,
            SubmittedAt      = attempt.SubmittedAt,
            Status           = attempt.Status.ToString(),
            Score            = attempt.Score,
            TotalViolations  = violationLogs.Count,
            CriticalCount    = critical,
            MediumCount      = medium,
            MinorCount       = minor,
            Events           = mergedEvents
        };
    }

    private static string GetPresenceDescription(PresenceEventType eventType) => eventType switch
    {
        PresenceEventType.Disconnected   => "Student went offline and heartbeat stopped.",
        PresenceEventType.Reconnected    => "Student came back online and reconnected.",
        PresenceEventType.PageRefreshed  => "Student refreshed or reloaded the exam page.",
        _                                => eventType.ToString()
    };

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
    public async Task<TutorDashboardResponse> GetTutorDashboardAsync(
        Guid tutorId, int page = 1, int pageSize = 10, string? search = null, string? status = null, Guid? examId = null)
    {
        // 1. Find all active courses assigned to this tutor
        var courseIds = await _db.Courses
            .AsNoTracking()
            .Where(c => c.AssignedTutorId == tutorId && c.IsActive)
            .Select(c => c.Id)
            .ToListAsync();

        if (courseIds.Count == 0)
            return EmptyTutorDashboard();

        // 2. Published exams in those courses (projection only)
        var exams = await _db.Exams
            .AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId) && e.Status == ExamStatus.Published)
            .Select(e => new { e.Id, e.Title, e.CourseId, CourseTitle = e.Course != null ? e.Course.Title : "" })
            .ToListAsync();

        if (exams.Count == 0)
            return EmptyTutorDashboard();

        var examIds = exams.Select(e => e.Id).ToList();

        // 3. Gather Exam Summaries
        var enrolledCounts = await _db.CourseEnrollments
            .AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId) && e.Status == CourseEnrollmentStatus.Approved)
            .GroupBy(e => e.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(e => e.CourseId, e => e.Count);

        var attemptStats = await _db.ExamAttempts
            .AsNoTracking()
            .Where(a => examIds.Contains(a.ExamId))
            .GroupBy(a => new { a.ExamId, a.Status })
            .Select(g => new { g.Key.ExamId, g.Key.Status, Count = g.Count() })
            .ToListAsync();

        var examScores = await _db.ExamAttempts
            .AsNoTracking()
            .Where(a => examIds.Contains(a.ExamId) && a.Score.HasValue)
            .GroupBy(a => a.ExamId)
            .Select(g => new { ExamId = g.Key, AvgScore = g.Average(a => a.Score) })
            .ToDictionaryAsync(e => e.ExamId, e => e.AvgScore);

        var examViolations = await _db.ViolationLogs
            .AsNoTracking()
            .Where(v => examIds.Contains(v.ExamId))
            .GroupBy(v => new { v.ExamId, v.Severity })
            .Select(g => new { g.Key.ExamId, g.Key.Severity, Count = g.Count() })
            .ToListAsync();

        var examSummaries = exams.Select(exam =>
        {
            var stats = attemptStats.Where(s => s.ExamId == exam.Id).ToList();
            var inProgress = stats.Where(s => s.Status == AttemptStatus.InProgress).Sum(s => s.Count);
            var submitted = stats.Where(s => s.Status == AttemptStatus.Submitted || s.Status == AttemptStatus.Graded).Sum(s => s.Count);
            var forceSubmitted = stats.Where(s => s.Status == AttemptStatus.ForceSubmitted).Sum(s => s.Count);
            var totalStarted = stats.Sum(s => s.Count);
            
            var enrolled = enrolledCounts.GetValueOrDefault(exam.CourseId, 0);
            var violations = examViolations.Where(v => v.ExamId == exam.Id).ToList();

            return new ExamMonitoringSummary
            {
                ExamId              = exam.Id,
                ExamTitle           = exam.Title,
                CourseTitle         = exam.CourseTitle,
                TotalEnrolled       = enrolled,
                InProgressCount     = inProgress,
                SubmittedCount      = submitted,
                ForceSubmittedCount = forceSubmitted,
                NotStartedCount     = Math.Max(0, enrolled - totalStarted),
                TotalViolations     = violations.Sum(v => v.Count),
                CriticalViolations  = violations.Where(v => v.Severity == ViolationSeverity.Critical).Sum(v => v.Count),
                AverageScore        = examScores.GetValueOrDefault(exam.Id)
            };
        }).ToList();

        // 4. Gather Submissions (Finished Attempts)
        var query = _db.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.Student)
            .AsNoTracking()
            .Where(a => examIds.Contains(a.ExamId) && a.Status != AttemptStatus.InProgress);

        if (examId.HasValue)
        {
            query = query.Where(a => a.ExamId == examId.Value);
        }

        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            if (Enum.TryParse<AttemptStatus>(status, out var attemptStatus))
            {
                query = query.Where(a => a.Status == attemptStatus);
            }
        }

        if (!string.IsNullOrEmpty(search))
        {
            var lowerSearch = search.ToLower();
            query = query.Where(a => 
                (a.Student != null && a.Student.FirstName.ToLower().Contains(lowerSearch)) ||
                (a.Student != null && a.Student.LastName.ToLower().Contains(lowerSearch)) ||
                (a.Student != null && a.Student.StudentId != null && a.Student.StudentId.ToLower().Contains(lowerSearch)) ||
                (a.Exam != null && a.Exam.Title.ToLower().Contains(lowerSearch))
            );
        }

        var totalSubmissions = await query.CountAsync();

        var recentAttempts = await query
            .OrderByDescending(a => a.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var recentAttemptIds = recentAttempts.Select(a => a.Id).ToList();

        var recentViolations = await _db.ViolationLogs
            .AsNoTracking()
            .Where(v => recentAttemptIds.Contains(v.AttemptId))
            .GroupBy(v => v.AttemptId)
            .Select(g => new { 
                AttemptId = g.Key, 
                Count = g.Count(),
                HasCritical = g.Any(v => v.Severity == ViolationSeverity.Critical),
                HasMedium = g.Any(v => v.Severity == ViolationSeverity.Medium),
                HasMinor = g.Any(v => v.Severity == ViolationSeverity.Minor)
            })
            .ToListAsync();

        var violationMap = recentViolations.ToDictionary(v => v.AttemptId);

        var recentSubmissions = recentAttempts.Select(a =>
        {
            var vInfo = violationMap.GetValueOrDefault(a.Id);
            var highestSeverity = "None";
            if (vInfo != null && vInfo.Count > 0)
            {
                highestSeverity = vInfo.HasCritical ? "Critical" : vInfo.HasMedium ? "Medium" : "Minor";
            }

            return new SubmissionRow
            {
                AttemptId       = a.Id,
                StudentName     = a.Student != null ? $"{a.Student.FirstName} {a.Student.LastName}" : "Unknown",
                StudentCode     = a.Student?.StudentId ?? "-",
                ExamTitle       = a.Exam?.Title ?? "-",
                Status          = a.Status.ToString(),
                SubmittedAt     = a.SubmittedAt,
                Score           = a.Score,
                ViolationCount  = vInfo?.Count ?? 0,
                HighestSeverity = highestSeverity
            };
        }).ToList();

        // 5. Overall Violation Types Distribution
        var violationTypes = await _db.ViolationLogs
            .AsNoTracking()
            .Where(v => examIds.Contains(v.ExamId))
            .GroupBy(v => v.Type)
            .Select(g => new ViolationTypeStat { ViolationType = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        return new TutorDashboardResponse
        {
            ExamSummaries             = examSummaries,
            RecentSubmissions         = recentSubmissions,
            TotalSubmissions          = totalSubmissions,
            Page                      = page,
            PageSize                  = pageSize,
            ViolationTypeDistribution = violationTypes
        };
    }

    private static TutorDashboardResponse EmptyTutorDashboard() => new()
    {
        ExamSummaries             = [],
        RecentSubmissions         = [],
        TotalSubmissions          = 0,
        Page                      = 1,
        PageSize                  = 10,
        ViolationTypeDistribution = []
    };

    // ── Admin Dashboard ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<AdminDashboardResponse> GetAdminDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var totalActiveCourses = await _db.Courses.CountAsync(c => c.IsActive);
        
        var totalCompletedExams = await _db.ExamAttempts
            .Where(a => a.Status != AttemptStatus.InProgress)
            .Select(a => a.ExamId)
            .Distinct()
            .CountAsync();

        var totalSubmissions = await _db.ExamAttempts.CountAsync(a => a.Status != AttemptStatus.InProgress);
        var totalForceSubmitted = await _db.ExamAttempts.CountAsync(a => a.Status == AttemptStatus.ForceSubmitted);
        var totalViolations = await _db.ViolationLogs.CountAsync();
        
        var suspiciousRate = totalSubmissions > 0
            ? Math.Round((decimal)totalForceSubmitted / totalSubmissions * 100, 1)
            : 0m;

        // Exam Statistics Table
        var examStats = await _db.ExamAttempts
            .Include(a => a.Exam).ThenInclude(e => e!.Course)
            .Include(a => a.Exam).ThenInclude(e => e!.CreatedByUser)
            .AsNoTracking()
            .GroupBy(a => a.ExamId)
            .Select(g => new ExamStatisticsRow
            {
                ExamId              = g.Key,
                ExamTitle           = g.First().Exam!.Title,
                CourseTitle         = g.First().Exam!.Course!.Title,
                TutorName           = $"{g.First().Exam!.CreatedByUser!.FirstName} {g.First().Exam!.CreatedByUser!.LastName}",
                SubmittedCount      = g.Count(a => a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.Graded),
                ForceSubmittedCount = g.Count(a => a.Status == AttemptStatus.ForceSubmitted),
                InProgressCount     = g.Count(a => a.Status == AttemptStatus.InProgress)
            })
            .ToListAsync();

        var examViolationCounts = await _db.ViolationLogs
            .GroupBy(v => v.ExamId)
            .Select(g => new { ExamId = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.ExamId, x => x.Count);

        foreach (var row in examStats)
        {
            row.TotalViolations = examViolationCounts.GetValueOrDefault(row.ExamId, 0);
        }

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
            .ToDictionaryAsync(x => x.Date, x => x.Count);

        var dailyPoints = activityTrend.Select(d => new DailyActivityPoint
        {
            Date           = DateOnly.FromDateTime(d.Date),
            ExamCount      = d.Count,
            ViolationCount = violationsByDay.GetValueOrDefault(d.Date, 0)
        })
        .OrderBy(d => d.Date)
        .ToList();

        return new AdminDashboardResponse
        {
            TotalActiveCourses  = totalActiveCourses,
            TotalCompletedExams = totalCompletedExams,
            TotalSubmissions    = totalSubmissions,
            TotalViolations     = totalViolations,
            ForceSubmissionRate = suspiciousRate,
            ExamStatistics      = examStats,
            TopViolationTypes   = topViolationTypes,
            ActivityTrend       = dailyPoints
        };
    }

    private static void AuthorizeForCourse(Domain.Entities.Course course, Guid userId, string role)
    {
        if (role == "Tutor" && course.AssignedTutorId != userId)
            throw new ForbiddenException("You can only view monitoring data for courses assigned to you.");
    }
}
