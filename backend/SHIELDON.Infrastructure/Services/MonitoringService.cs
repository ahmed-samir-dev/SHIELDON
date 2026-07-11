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

        var totalActiveCourses = courseIds.Count;

        var totalUniqueStudents = 0;
        if (courseIds.Count > 0)
        {
            totalUniqueStudents = await _db.CourseEnrollments
                .AsNoTracking()
                .Where(e => courseIds.Contains(e.CourseId) && e.Status == CourseEnrollmentStatus.Approved)
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();
        }

        if (courseIds.Count == 0)
            return EmptyTutorDashboard(totalActiveCourses, totalUniqueStudents);

        // 2. Published exams in those courses (projection only)
        var exams = await _db.Exams
            .AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId) && e.Status == ExamStatus.Published)
            .Select(e => new { e.Id, e.Title, e.CourseId, CourseTitle = e.Course != null ? e.Course.Title : "" })
            .ToListAsync();

        if (exams.Count == 0)
            return EmptyTutorDashboard(totalActiveCourses, totalUniqueStudents);

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

        var autoSubmittedByExam = await _db.ViolationLogs
            .AsNoTracking()
            .Where(v => examIds.Contains(v.ExamId) && v.WasAutoSubmit)
            .GroupBy(v => v.ExamId)
            .Select(g => new { ExamId = g.Key, Count = g.Select(v => v.AttemptId).Distinct().Count() })
            .ToDictionaryAsync(x => x.ExamId, x => x.Count);

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

        var gradedAttemptsInfo = await _db.ExamAttempts
            .AsNoTracking()
            .Where(a => examIds.Contains(a.ExamId) && a.Status != AttemptStatus.InProgress)
            .Select(a => new { a.ExamId, a.Score, PassScore = a.Exam!.PassScore, a.StartedAt, a.SubmittedAt })
            .ToListAsync();

        var examSummaries = exams.Select(exam =>
        {
            var stats = attemptStats.Where(s => s.ExamId == exam.Id).ToList();
            var inProgress = stats.Where(s => s.Status == AttemptStatus.InProgress).Sum(s => s.Count);
            var submitted = stats.Where(s => s.Status == AttemptStatus.Submitted || s.Status == AttemptStatus.Graded).Sum(s => s.Count);
            var forceSubmitted = stats.Where(s => s.Status == AttemptStatus.ForceSubmitted).Sum(s => s.Count);
            var totalStarted = stats.Sum(s => s.Count);
            
            var violationLimit = autoSubmittedByExam.GetValueOrDefault(exam.Id, 0);
            var timeout = Math.Max(0, forceSubmitted - violationLimit);

            var enrolled = enrolledCounts.GetValueOrDefault(exam.CourseId, 0);
            var violations = examViolations.Where(v => v.ExamId == exam.Id).ToList();
            var gradedForExam = gradedAttemptsInfo.Where(a => a.ExamId == exam.Id).ToList();

            return new ExamMonitoringSummary
            {
                ExamId              = exam.Id,
                ExamTitle           = exam.Title,
                CourseTitle         = exam.CourseTitle,
                TotalEnrolled       = enrolled,
                InProgressCount     = inProgress,
                SubmittedCount      = submitted,
                ForceSubmittedCount = forceSubmitted,
                TimeoutCount        = timeout,
                ViolationLimitCount = violationLimit,
                NotStartedCount     = Math.Max(0, enrolled - totalStarted),
                TotalViolations     = violations.Sum(v => v.Count),
                CriticalViolations  = violations.Where(v => v.Severity == ViolationSeverity.Critical).Sum(v => v.Count),
                AverageScore        = examScores.GetValueOrDefault(exam.Id),
                PassedCount         = gradedForExam.Count(a => a.Score >= a.PassScore),
                FailedCount         = gradedForExam.Count(a => a.Score < a.PassScore)
            };
        }).ToList();

        // 3.5 Calculate Global KPIs (Before filters)
        var activeExams = attemptStats.Where(s => s.Status == AttemptStatus.InProgress).Sum(s => s.Count);
        var expectedSubmissions = exams.Sum(e => enrolledCounts.GetValueOrDefault(e.CourseId, 0));
        
        var uniqueStudentsWithSubmissions = await _db.ExamAttempts
            .AsNoTracking()
            .Where(a => examIds.Contains(a.ExamId) && a.Status != AttemptStatus.InProgress)
            .Select(a => new { a.ExamId, a.StudentId })
            .Distinct()
            .CountAsync();

        var completionRate = expectedSubmissions > 0 ? Math.Round((decimal)uniqueStudentsWithSubmissions / expectedSubmissions * 100, 1) : 0m;

        var totalPassedStudents = gradedAttemptsInfo.Count(a => a.Score >= a.PassScore);
        var averagePassRate = gradedAttemptsInfo.Count > 0
            ? Math.Round((decimal)totalPassedStudents / gradedAttemptsInfo.Count * 100, 1)
            : 0m;

        var completedAttemptsWithTime = gradedAttemptsInfo.Where(a => a.SubmittedAt.HasValue).ToList();
        var averageTimeMinutes = completedAttemptsWithTime.Count > 0
            ? (int)completedAttemptsWithTime.Average(a => (a.SubmittedAt!.Value - a.StartedAt).TotalMinutes)
            : 0;

        // 4. Recent Finished Attempts Query (with optional filters)
        var query = _db.ExamAttempts
            .Include(a => a.Exam!).ThenInclude(e => e.Course)
            .Include(a => a.Student)
            .AsNoTracking()
            .Where(a => examIds.Contains(a.ExamId));

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
                (a.Exam != null && a.Exam.Title.ToLower().Contains(lowerSearch)) ||
                (a.Exam != null && a.Exam.Course != null && a.Exam.Course.Title.ToLower().Contains(lowerSearch))
            );
        }

        var groupedQuery = query
            .GroupBy(a => new { a.StudentId, a.ExamId })
            .Select(g => new {
                StudentId = g.Key.StudentId,
                ExamId = g.Key.ExamId,
                LatestAttemptAt = g.Max(a => a.SubmittedAt ?? a.StartedAt)
            });

        var totalSubmissions = await groupedQuery.CountAsync();

        var paginatedGroups = await groupedQuery
            .OrderByDescending(g => g.LatestAttemptAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var studentIds = paginatedGroups.Select(g => g.StudentId).Distinct().ToList();
        var specificExamIds = paginatedGroups.Select(g => g.ExamId).Distinct().ToList();

        var attemptsToProcess = await query
            .Where(a => studentIds.Contains(a.StudentId) && specificExamIds.Contains(a.ExamId))
            .ToListAsync();

        var relevantPairs = paginatedGroups.Select(g => (g.StudentId, g.ExamId)).ToHashSet();
        attemptsToProcess = attemptsToProcess
            .Where(a => relevantPairs.Contains((a.StudentId, a.ExamId)))
            .ToList();

        var recentAttemptIds = attemptsToProcess.Select(a => a.Id).ToList();

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

        SubmissionRow MapToRow(ExamAttempt a)
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
                ExamId          = a.ExamId,
                ExamTitle       = a.Exam?.Title ?? "-",
                CourseTitle     = a.Exam?.Course?.Title ?? "-",
                Status          = a.Status.ToString(),
                SubmittedAt     = a.SubmittedAt,
                DurationMinutes = a.SubmittedAt.HasValue ? (int)(a.SubmittedAt.Value - a.StartedAt).TotalMinutes : null,
                Score           = a.Score,
                Passed          = a.Score >= a.Exam?.PassScore,
                Failed          = a.Score < a.Exam?.PassScore,
                ViolationCount  = vInfo?.Count ?? 0,
                HighestSeverity = highestSeverity
            };
        }

        var groupedAttempts = attemptsToProcess
            .GroupBy(a => new { a.StudentId, a.ExamId })
            .ToList();

        var recentSubmissions = new List<SubmissionRow>();

        foreach (var group in groupedAttempts)
        {
            var sortedAttempts = group.OrderByDescending(a => a.SubmittedAt ?? a.StartedAt).ToList();
            var latestAttempt = sortedAttempts.First();

            var mappedLatest = MapToRow(latestAttempt);
            mappedLatest.History = sortedAttempts.Select(a => MapToRow(a)).ToList();

            recentSubmissions.Add(mappedLatest);
        }

        recentSubmissions = recentSubmissions
            .OrderByDescending(r => r.SubmittedAt ?? DateTime.MaxValue)
            .ToList();

        // 5. Overall Violation Types Distribution
        var violationTypes = await _db.ViolationLogs
            .AsNoTracking()
            .Where(v => examIds.Contains(v.ExamId))
            .GroupBy(v => v.Type)
            .Select(g => new ViolationTypeStat { ViolationType = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        // 6. Detailed Violations by Course, Type, and Severity (For new Stacked Bar Chart)
        var rawViolations = await _db.ViolationLogs
            .AsNoTracking()
            .Where(v => examIds.Contains(v.ExamId))
            .Select(v => new { v.ExamId, v.Type, v.Severity })
            .ToListAsync();

        var courseViolationDetails = rawViolations
            .Join(exams, v => v.ExamId, e => e.Id, (v, e) => new { e.CourseTitle, v.Type, v.Severity })
            .GroupBy(x => new { x.CourseTitle, x.Type, x.Severity })
            .Select(g => new CourseViolationDetail
            {
                CourseTitle = g.Key.CourseTitle ?? "Other",
                ViolationType = g.Key.Type.ToString(),
                Severity = g.Key.Severity.ToString(),
                Count = g.Count()
            })
            .ToList();

        return new TutorDashboardResponse
        {
            ExamSummaries             = examSummaries,
            RecentSubmissions         = recentSubmissions,
            TotalSubmissions          = totalSubmissions,
            Page                      = page,
            PageSize                  = pageSize,
            ViolationTypeDistribution = violationTypes,
            TotalActiveCourses        = totalActiveCourses,
            TotalStudents             = totalUniqueStudents,
            ActiveExams               = activeExams,
            AveragePassRate           = averagePassRate,
            CompletionRate            = completionRate,
            TotalPassedStudents       = totalPassedStudents,
            AverageTimeMinutes        = averageTimeMinutes,
            CourseViolationDetails    = courseViolationDetails
        };
    }

    private static TutorDashboardResponse EmptyTutorDashboard(int totalActiveCourses = 0, int totalStudents = 0) => new()
    {
        ExamSummaries             = [],
        RecentSubmissions         = [],
        TotalSubmissions          = 0,
        Page                      = 1,
        PageSize                  = 10,
        ViolationTypeDistribution = [],
        CourseViolationDetails    = [],
        TotalActiveCourses        = totalActiveCourses,
        TotalStudents             = totalStudents,
        ActiveExams               = 0,
        AveragePassRate           = 0m,
        CompletionRate            = 0m,
        TotalPassedStudents       = 0,
        AverageTimeMinutes        = 0
    };

    // ── Admin Dashboard ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<AdminDashboardResponse> GetAdminDashboardAsync(ExamStatisticsQueryParams queryParams)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        // ── KPI Row 1 ───────────────────────────────────────────────────────────
        var totalActiveCourses = await _db.Courses.CountAsync(c => c.IsActive);
        var totalExams = await _db.Exams.CountAsync();

        var totalCompletedExams = await _db.ExamAttempts
            .Where(a => a.Status != AttemptStatus.InProgress)
            .Select(a => a.ExamId)
            .Distinct()
            .CountAsync();

        var totalSubmissions      = await _db.ExamAttempts.CountAsync(a => a.Status != AttemptStatus.InProgress);
        var totalForceSubmitted   = await _db.ExamAttempts.CountAsync(a => a.Status == AttemptStatus.ForceSubmitted);
        var totalViolations       = await _db.ViolationLogs.CountAsync();

        // ── KPI Row 2 ───────────────────────────────────────────────────────────
        var totalStudents         = await _db.Users.CountAsync(u => u.Role == Domain.Enums.UserRole.Student);
        var totalTutors           = await _db.Users.CountAsync(u => u.Role == Domain.Enums.UserRole.Tutor);
        var activeExamsInProgress = await _db.ExamAttempts.CountAsync(a => a.Status == AttemptStatus.InProgress);

        var forceSubmissionRate = totalSubmissions > 0
            ? Math.Round((decimal)totalForceSubmitted / totalSubmissions * 100, 1)
            : 0m;

        var gradedAttemptsInfo = await _db.ExamAttempts
            .Where(a => a.Status != AttemptStatus.InProgress)
            .Select(a => new { a.Score, PassScore = a.Exam.PassScore })
            .ToListAsync();
            
        var averagePassRate = gradedAttemptsInfo.Count > 0
            ? Math.Round((decimal)gradedAttemptsInfo.Count(a => a.Score >= a.PassScore) / gradedAttemptsInfo.Count * 100, 1)
            : 0m;

        // ── Violations by Course Chart ───────────────────────────────────────────
        // Join ViolationLogs → Exam → Course to group per course
        var violationsByCourse = await _db.ViolationLogs
            .Join(_db.Exams, v => v.ExamId, e => e.Id, (v, e) => new { v.Severity, e.CourseId })
            .Join(_db.Courses, x => x.CourseId, c => c.Id, (x, c) => new { x.Severity, CourseTitle = c.Title })
            .GroupBy(x => x.CourseTitle)
            .Select(g => new CourseViolationStat
            {
                CourseTitle    = g.Key,
                ViolationCount = g.Count(),
                CriticalCount  = g.Count(x => x.Severity == ViolationSeverity.Critical),
                MediumCount    = g.Count(x => x.Severity == ViolationSeverity.Medium),
                MinorCount     = g.Count(x => x.Severity == ViolationSeverity.Minor)
            })
            .OrderByDescending(s => s.ViolationCount)
            .Take(10)
            .AsNoTracking()
            .ToListAsync();

        // ── Global Submission Outcomes Chart ─────────────────────────────────────
        var allAttempts = await _db.ExamAttempts
            .AsNoTracking()
            .Select(a => new { a.Id, a.Status })
            .ToListAsync();

        var totalAttemptCount = allAttempts.Count;
        var inProgressCount   = allAttempts.Count(a => a.Status == AttemptStatus.InProgress);

        var autoExpiredExamIds = await _db.ViolationLogs
            .Where(v => v.WasAutoSubmit)
            .Select(v => v.AttemptId)
            .Distinct()
            .ToListAsync();

        var autoExpiredCount = autoExpiredExamIds.Count;
        
        var forceSubmitCount = allAttempts.Count(a => 
            a.Status == AttemptStatus.ForceSubmitted && !autoExpiredExamIds.Contains(a.Id));

        var cleanSubmittedCount = allAttempts.Count(a => 
            (a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.Graded) && !autoExpiredExamIds.Contains(a.Id));

        var globalOutcomes = new List<SubmissionOutcomeStat>();
        void AddOutcome(string label, int count)
        {
            globalOutcomes.Add(new SubmissionOutcomeStat
            {
                Outcome    = label,
                Count      = count,
                Percentage = totalAttemptCount > 0 ? Math.Round((decimal)count / totalAttemptCount * 100, 1) : 0m
            });
        }
        AddOutcome("Submitted", cleanSubmittedCount);
        AddOutcome("ForceSubmitted", forceSubmitCount);
        AddOutcome("AutoExpired", autoExpiredCount);
        AddOutcome("InProgress", inProgressCount);

        // ── Course-level Granular Data for Frontend Chart Filtering ──────────────
        var activeCourseTitles = await _db.Courses
            .Where(c => c.IsActive)
            .Select(c => c.Title)
            .Distinct()
            .ToListAsync();

        var courseViolationDetails = await _db.ViolationLogs
            .Join(_db.Exams, v => v.ExamId, e => e.Id, (v, e) => new { v.Type, v.Severity, e.CourseId })
            .Join(_db.Courses, x => x.CourseId, c => c.Id, (x, c) => new { x.Type, x.Severity, CourseTitle = c.Title })
            .GroupBy(x => new { x.CourseTitle, x.Type, x.Severity })
            .Select(g => new CourseViolationDetail
            {
                CourseTitle = g.Key.CourseTitle ?? "Other",
                ViolationType = g.Key.Type.ToString(),
                Severity = g.Key.Severity.ToString(),
                Count = g.Count()
            })
            .AsNoTracking()
            .ToListAsync();

        var courseOutcomesRaw = await _db.ExamAttempts
            .Join(_db.Exams, a => a.ExamId, e => e.Id, (a, e) => new { a.Id, a.Status, e.CourseId })
            .Join(_db.Courses, x => x.CourseId, c => c.Id, (x, c) => new { x.Id, x.Status, CourseTitle = c.Title })
            .AsNoTracking()
            .ToListAsync();

        var courseSubmissionOutcomes = new List<CourseSubmissionOutcome>();
        var groupedOutcomes = courseOutcomesRaw.GroupBy(x => x.CourseTitle);
        foreach (var group in groupedOutcomes)
        {
            var courseTitle = group.Key;
            
            var inProgressC = group.Count(a => a.Status == AttemptStatus.InProgress);
            var autoExpC = group.Count(a => autoExpiredExamIds.Contains(a.Id));
            var forceSubC = group.Count(a => a.Status == AttemptStatus.ForceSubmitted && !autoExpiredExamIds.Contains(a.Id));
            var cleanSubC = group.Count(a => (a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.Graded) && !autoExpiredExamIds.Contains(a.Id));
            
            courseSubmissionOutcomes.Add(new CourseSubmissionOutcome { CourseTitle = courseTitle, Outcome = "Submitted", Count = cleanSubC });
            courseSubmissionOutcomes.Add(new CourseSubmissionOutcome { CourseTitle = courseTitle, Outcome = "ForceSubmitted", Count = forceSubC });
            courseSubmissionOutcomes.Add(new CourseSubmissionOutcome { CourseTitle = courseTitle, Outcome = "AutoExpired", Count = autoExpC });
            courseSubmissionOutcomes.Add(new CourseSubmissionOutcome { CourseTitle = courseTitle, Outcome = "InProgress", Count = inProgressC });
        }

        // ── Top Violation Types Chart ────────────────────────────────────────────
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

        // ── Financial Data (Payment Module) ──────────────────────────────────────
        var totalRevenue = await _db.PaymentRecords
            .Where(p => p.Status == PaymentRecordStatus.Paid)
            .SumAsync(p => p.AmountUSD);

        var recentPayments = await _db.PaymentRecords
            .Include(p => p.Student)
            .Where(p => p.Status == PaymentRecordStatus.Paid && p.PaidAt != null)
            .OrderByDescending(p => p.PaidAt)
            .Take(15)
            .Select(p => new RecentPaymentStat
            {
                PaymentId = p.Id,
                AmountUSD = p.AmountUSD,
                PaidAt = p.PaidAt!.Value,
                StudentName = p.Student != null ? $"{p.Student.FirstName} {p.Student.LastName}" : "Unknown"
            })
            .AsNoTracking()
            .ToListAsync();

        // ── 30-Day Activity Trend ────────────────────────────────────────────────
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

        // ── Exam Statistics Table (server-side: search, sort, paginate) ──────────
        // Step 1: build the base join in-memory using projections
        // We need Exam + Course + CreatedByUser + aggregate stats
        // Load candidate exams with their basic info
        var examIds = await _db.ExamAttempts
            .Select(a => a.ExamId)
            .Distinct()
            .ToListAsync();

        // Also include exams with no attempts (show all published exams)
        var publishedExamIds = await _db.Exams
            .Where(e => e.Status == ExamStatus.Published)
            .Select(e => e.Id)
            .ToListAsync();

        var allExamIds = examIds.Union(publishedExamIds).Distinct().ToList();

        // Load exam info
        var examInfos = await _db.Exams
            .Where(e => allExamIds.Contains(e.Id))
            .Select(e => new
            {
                e.Id,
                e.Title,
                ScheduledAt    = e.ScheduledAt,
                PassScore      = e.PassScore,
                CourseTitle    = e.Course != null ? e.Course.Title : "",
                TutorId        = e.CreatedByUserId,
                TutorFirstName = e.CreatedByUser != null ? e.CreatedByUser.FirstName : "",
                TutorLastName  = e.CreatedByUser != null ? e.CreatedByUser.LastName : ""
            })
            .AsNoTracking()
            .ToListAsync();

        // Load attempt stats per exam
        var attemptStatsByExam = await _db.ExamAttempts
            .Where(a => allExamIds.Contains(a.ExamId))
            .GroupBy(a => a.ExamId)
            .Select(g => new
            {
                ExamId              = g.Key,
                TotalAttempts       = g.Count(),
                SubmittedCount      = g.Count(a => a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.Graded),
                ForceSubmittedCount = g.Count(a => a.Status == AttemptStatus.ForceSubmitted),
                InProgressCount     = g.Count(a => a.Status == AttemptStatus.InProgress),
                AverageScore        = g.Where(a => a.Score.HasValue).Average(a => (decimal?)a.Score)
            })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.ExamId);

        // Load violation counts per exam
        var violationCountsByExam = await _db.ViolationLogs
            .Where(v => allExamIds.Contains(v.ExamId))
            .GroupBy(v => v.ExamId)
            .Select(g => new { ExamId = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.ExamId, x => x.Count);

        // Load pass counts per exam (score >= PassScore)
        var passCounts = await _db.ExamAttempts
            .Where(a => allExamIds.Contains(a.ExamId) && a.Score.HasValue)
            .Join(_db.Exams, a => a.ExamId, e => e.Id, (a, e) => new { a.ExamId, a.Score, e.PassScore })
            .Where(x => x.Score >= x.PassScore)
            .GroupBy(x => x.ExamId)
            .Select(g => new { ExamId = g.Key, PassCount = g.Count() })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.ExamId, x => x.PassCount);

        // Filter by Tutor if requested
        if (queryParams.TutorId.HasValue)
        {
            examInfos = examInfos.Where(e => e.TutorId == queryParams.TutorId.Value).ToList();
        }

        // Build rows in memory
        var allRows = examInfos.Select(exam =>
        {
            var stats      = attemptStatsByExam.GetValueOrDefault(exam.Id);
            var submitted  = stats?.SubmittedCount ?? 0;
            var passCount  = passCounts.GetValueOrDefault(exam.Id, 0);
            var passRate   = submitted > 0 ? (decimal?)Math.Round((decimal)passCount / submitted * 100, 1) : null;

            return new ExamStatisticsRow
            {
                ExamId              = exam.Id,
                ExamTitle           = exam.Title,
                CourseTitle         = exam.CourseTitle,
                TutorName           = $"{exam.TutorFirstName} {exam.TutorLastName}".Trim(),
                ScheduledAt         = exam.ScheduledAt,
                TotalAttempts       = stats?.TotalAttempts ?? 0,
                SubmittedCount      = submitted,
                ForceSubmittedCount = stats?.ForceSubmittedCount ?? 0,
                InProgressCount     = stats?.InProgressCount ?? 0,
                TotalViolations     = violationCountsByExam.GetValueOrDefault(exam.Id, 0),
                AverageScore        = stats?.AverageScore.HasValue == true ? Math.Round(stats.AverageScore!.Value, 1) : null,
                PassRate            = passRate
            };
        }).ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var term = queryParams.Search.Trim().ToLower();
            allRows = allRows.Where(r =>
                r.ExamTitle.ToLower().Contains(term) ||
                r.CourseTitle.ToLower().Contains(term) ||
                r.TutorName.ToLower().Contains(term)
            ).ToList();
        }

        // Apply sort
        var sortCol = (queryParams.SortColumn ?? "ScheduledAt").ToLower();
        var desc    = (queryParams.SortDirection ?? "desc").ToLower() == "desc";

        allRows = sortCol switch
        {
            "examtitle"           => desc ? allRows.OrderByDescending(r => r.ExamTitle).ToList()           : allRows.OrderBy(r => r.ExamTitle).ToList(),
            "coursetitle"         => desc ? allRows.OrderByDescending(r => r.CourseTitle).ToList()         : allRows.OrderBy(r => r.CourseTitle).ToList(),
            "tutorname"           => desc ? allRows.OrderByDescending(r => r.TutorName).ToList()           : allRows.OrderBy(r => r.TutorName).ToList(),
            "totalattempts"       => desc ? allRows.OrderByDescending(r => r.TotalAttempts).ToList()       : allRows.OrderBy(r => r.TotalAttempts).ToList(),
            "totalviolations"     => desc ? allRows.OrderByDescending(r => r.TotalViolations).ToList()     : allRows.OrderBy(r => r.TotalViolations).ToList(),
            "averagescore"        => desc ? allRows.OrderByDescending(r => r.AverageScore).ToList()        : allRows.OrderBy(r => r.AverageScore).ToList(),
            "passrate"            => desc ? allRows.OrderByDescending(r => r.PassRate).ToList()            : allRows.OrderBy(r => r.PassRate).ToList(),
            _                     => desc ? allRows.OrderByDescending(r => r.ScheduledAt).ToList()         : allRows.OrderBy(r => r.ScheduledAt).ToList()
        };

        // Apply pagination
        var examTotalCount = allRows.Count;
        var page           = Math.Max(1, queryParams.Page);
        var size           = Math.Clamp(queryParams.PageSize, 1, 100);
        var pagedRows      = allRows.Skip((page - 1) * size).Take(size).ToList();

        return new AdminDashboardResponse
        {
            // KPI Row 1
            TotalActiveCourses  = totalActiveCourses,
            TotalExams          = totalExams,
            TotalCompletedExams = totalCompletedExams,
            TotalSubmissions    = totalSubmissions,
            TotalViolations     = totalViolations,
            // KPI Row 2
            TotalStudents         = totalStudents,
            TotalTutors           = totalTutors,
            ActiveExamsInProgress = activeExamsInProgress,
            AveragePassRate       = averagePassRate,
            ForceSubmissionRate   = forceSubmissionRate,
            TotalRevenueUSD       = totalRevenue,
            // Charts
            ViolationsByCourse        = violationsByCourse,
            GlobalSubmissionOutcomes  = globalOutcomes,
            RecentPayments            = recentPayments,
            TopViolationTypes         = topViolationTypes,
            ActivityTrend             = dailyPoints,
            // Granular Data
            ActiveCourseTitles        = activeCourseTitles,
            CourseViolationDetails    = courseViolationDetails,
            CourseSubmissionOutcomes  = courseSubmissionOutcomes,
            // Exam Statistics Table
            ExamStatistics          = pagedRows,
            ExamStatisticsTotalCount = examTotalCount,
            ExamStatisticsPage       = page,
            ExamStatisticsPageSize   = size
        };
    }
    public async Task<PlatformActivityResponse> GetPlatformActivityAsync(int? days)
    {
        var queryExams = _db.ExamAttempts.AsQueryable();
        var queryViolations = _db.ViolationLogs.AsQueryable();

        if (days.HasValue)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days.Value);
            queryExams = queryExams.Where(a => a.StartedAt >= cutoff);
            queryViolations = queryViolations.Where(v => v.CreatedAt >= cutoff);
        }

        var activityTrend = await queryExams
            .GroupBy(a => a.StartedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToListAsync();

        var violationsByDay = await queryViolations
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

        return new PlatformActivityResponse { ActivityTrend = dailyPoints };
    }

    public async Task<PaymentsTrendResponse> GetPaymentsTrendAsync(int? days)
    {
        var query = _db.PaymentRecords.Where(p => p.Status == PaymentRecordStatus.Paid && p.PaidAt != null).AsQueryable();

        if (days.HasValue)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days.Value);
            query = query.Where(p => p.PaidAt >= cutoff);
        }

        var payments = await query
            .GroupBy(p => p.PaidAt!.Value.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.AmountUSD) })
            .AsNoTracking()
            .ToListAsync();

        var trendPoints = payments.Select(p => new PaymentTrendPoint
        {
            Date = DateOnly.FromDateTime(p.Date),
            AmountUSD = p.Amount
        })
        .OrderBy(p => p.Date)
        .ToList();

        return new PaymentsTrendResponse { PaymentsTrend = trendPoints };
    }

    private static void AuthorizeForCourse(Domain.Entities.Course course, Guid userId, string role)
    {
        if (role == "Tutor" && course.AssignedTutorId != userId)
            throw new ForbiddenException("You can only view monitoring data for courses assigned to you.");
    }
}
