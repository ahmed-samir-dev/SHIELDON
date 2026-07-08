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
    public async Task<AdminDashboardResponse> GetAdminDashboardAsync(ExamStatisticsQueryParams queryParams)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        // ── KPI Row 1 ───────────────────────────────────────────────────────────
        var totalActiveCourses = await _db.Courses.CountAsync(c => c.IsActive);

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
            .Select(a => new { a.Status })
            .ToListAsync();

        var totalAttemptCount = allAttempts.Count;
        var submittedCount    = allAttempts.Count(a => a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.Graded);
        var forceSubmitCount  = allAttempts.Count(a => a.Status == AttemptStatus.ForceSubmitted);
        var inProgressCount   = allAttempts.Count(a => a.Status == AttemptStatus.InProgress);
        // AutoExpired: submitted attempts that were force-submitted by the WasAutoSubmit flag
        // We approximate: Submitted attempts that have at least one WasAutoSubmit violation
        var autoExpiredExamIds = await _db.ViolationLogs
            .Where(v => v.WasAutoSubmit)
            .Select(v => v.AttemptId)
            .Distinct()
            .ToListAsync();
        var autoExpiredCount  = allAttempts.Count(a => a.Status == AttemptStatus.Submitted &&
                                                       autoExpiredExamIds.Contains(Guid.Empty)); // placeholder — refined below

        // More accurate: count Submitted attempts that have a WasAutoSubmit violation log
        autoExpiredCount = await _db.ExamAttempts
            .Where(a => a.Status == AttemptStatus.Submitted)
            .Where(a => _db.ViolationLogs.Any(v => v.AttemptId == a.Id && v.WasAutoSubmit))
            .CountAsync();

        var cleanSubmittedCount = submittedCount - autoExpiredCount;

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
            TotalCompletedExams = totalCompletedExams,
            TotalSubmissions    = totalSubmissions,
            TotalViolations     = totalViolations,
            // KPI Row 2
            TotalStudents         = totalStudents,
            TotalTutors           = totalTutors,
            ActiveExamsInProgress = activeExamsInProgress,
            ForceSubmissionRate   = forceSubmissionRate,
            TotalRevenueUSD       = totalRevenue,
            // Charts
            ViolationsByCourse        = violationsByCourse,
            GlobalSubmissionOutcomes  = globalOutcomes,
            RecentPayments            = recentPayments,
            TopViolationTypes         = topViolationTypes,
            ActivityTrend             = dailyPoints,
            // Exam Statistics Table
            ExamStatistics          = pagedRows,
            ExamStatisticsTotalCount = examTotalCount,
            ExamStatisticsPage       = page,
            ExamStatisticsPageSize   = size
        };
    }

    private static void AuthorizeForCourse(Domain.Entities.Course course, Guid userId, string role)
    {
        if (role == "Tutor" && course.AssignedTutorId != userId)
            throw new ForbiddenException("You can only view monitoring data for courses assigned to you.");
    }
}
