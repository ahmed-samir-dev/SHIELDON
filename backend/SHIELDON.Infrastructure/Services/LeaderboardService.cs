using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Leaderboard.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements the Live Leaderboard feature.
///
/// Scoring:
///   - TotalScore:        weighted sum of all published grade records
///   - ExamAverage:       average of published exam-type grade records (Score/MaxScore * 100)
///   - AssignmentAverage: average of published assignment-type grade records (Score/MaxScore * 100)
///
/// Dense ranking: tied students share the same rank position.
/// Top-10 positions are returned; if position 10 is tied, all tied students at that position are included.
/// </summary>
public class LeaderboardService : ILeaderboardService
{
    private readonly AppDbContext _db;

    public LeaderboardService(AppDbContext db)
    {
        _db = db;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public async Task<LeaderboardResponse> GetLeaderboardAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var course = await _db.Courses
            .AsNoTracking()
            .Include(c => c.LeaderboardSettings)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Course", courseId);

        var settings = course.LeaderboardSettings ?? new LeaderboardSettings
        {
            CourseId = courseId,
            IsLeaderboardVisible = false,
            ShowStudentOwnRank = false,
            ScoringMetric = LeaderboardCourseMetric.TotalScore
        };

        // If a student requests and both visibility and own rank options are off, return empty response with flags = false
        if (requestingUserRole == "Student" && !settings.IsLeaderboardVisible && !settings.ShowStudentOwnRank)
        {
            return new LeaderboardResponse(
                CourseId: courseId,
                CourseTitle: course.Title,
                ScoringMetric: settings.ScoringMetric.ToString(),
                IsLeaderboardVisible: false,
                ShowStudentOwnRank: false,
                TopEntries: new List<LeaderboardEntryResponse>(),
                StudentOwnRank: null,
                GeneratedAt: DateTime.UtcNow
            );
        }

        // Compute current leaderboard
        var (ranked, snapshotMap) = await ComputeRanksAsync(courseId, settings.ScoringMetric, ct);

        // Top-10 positions (only returned if leaderboard is visible OR if instructor)
        List<LeaderboardEntryResponse> topEntries;
        if (requestingUserRole == "Student" && !settings.IsLeaderboardVisible)
        {
            topEntries = new List<LeaderboardEntryResponse>();
        }
        else
        {
            int cutoff = ranked.Count > 0
                ? ranked.Take(10).Select(r => r.Rank).DefaultIfEmpty(0).Last()
                : 0;
            topEntries = ranked.Where(r => r.Rank <= cutoff).Select(r =>
                ToEntry(r, snapshotMap)).ToList();
        }

        // Student own rank (only if ShowStudentOwnRank is true)
        LeaderboardEntryResponse? ownRank = null;
        if (requestingUserRole == "Student" && settings.ShowStudentOwnRank)
        {
            var own = ranked.FirstOrDefault(r => r.StudentId == requestingUserId);
            if (own != null)
                ownRank = ToEntry(own, snapshotMap);
        }

        return new LeaderboardResponse(
            CourseId: courseId,
            CourseTitle: course.Title,
            ScoringMetric: settings.ScoringMetric.ToString(),
            IsLeaderboardVisible: settings.IsLeaderboardVisible,
            ShowStudentOwnRank: settings.ShowStudentOwnRank,
            TopEntries: topEntries,
            StudentOwnRank: ownRank,
            GeneratedAt: DateTime.UtcNow
        );
    }

    public async Task<LeaderboardSettingsResponse> GetSettingsAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        await ValidateInstructorAccessAsync(courseId, requestingUserId, requestingUserRole, ct);

        var settings = await GetOrCreateSettingsAsync(courseId, ct);
        return MapSettings(settings);
    }

    public async Task<LeaderboardSettingsResponse> UpdateLeaderboardSettingsAsync(
        Guid courseId,
        UpdateLeaderboardSettingsRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        await ValidateInstructorAccessAsync(courseId, requestingUserId, requestingUserRole, ct);

        var settings = await GetOrCreateSettingsAsync(courseId, ct);

        // Validate metric string
        if (!Enum.TryParse<LeaderboardCourseMetric>(request.ScoringMetric, ignoreCase: true, out var metric))
            throw new ArgumentException($"Invalid ScoringMetric value: '{request.ScoringMetric}'. " +
                "Valid values: TotalScore, ExamAverage, AssignmentAverage");

        settings.IsLeaderboardVisible = request.IsLeaderboardVisible;
        settings.ShowStudentOwnRank   = request.ShowStudentOwnRank;
        settings.ScoringMetric        = metric;
        settings.UpdatedAt            = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // Recompute & snapshot if leaderboard is visible (API layer will broadcast via hub)
        if (settings.IsLeaderboardVisible)
            _ = await ComputeAndBroadcastAsync(courseId, ct);

        return MapSettings(settings);
    }

    public async Task<LeaderboardResponse?> ComputeAndBroadcastAsync(Guid courseId, CancellationToken ct = default)
    {
        var settings = await _db.LeaderboardSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CourseId == courseId, ct);

        if (settings == null || !settings.IsLeaderboardVisible)
            return null;

        var course = await _db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Course", courseId);

        var (ranked, snapshotMap) = await ComputeRanksAsync(courseId, settings.ScoringMetric, ct);

        // Save updated snapshots (upsert)
        await UpsertSnapshotsAsync(courseId, ranked, ct);

        // Build payload for broadcast (API layer will actually push via SignalR)
        int cutoff = ranked.Count > 0
            ? ranked.Take(10).Select(r => r.Rank).DefaultIfEmpty(0).Last()
            : 0;
        var topEntries = ranked.Where(r => r.Rank <= cutoff).Select(r =>
            ToEntry(r, snapshotMap)).ToList();

        return new LeaderboardResponse(
            CourseId: courseId,
            CourseTitle: course.Title,
            ScoringMetric: settings.ScoringMetric.ToString(),
            IsLeaderboardVisible: settings.IsLeaderboardVisible,
            ShowStudentOwnRank: settings.ShowStudentOwnRank,
            TopEntries: topEntries,
            StudentOwnRank: null, // not relevant for broadcast; clients compute own-rank client-side
            GeneratedAt: DateTime.UtcNow
        );
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private record RankedStudent(
        Guid StudentId,
        string FullName,
        string? DisplayId,
        string? AvatarUrl,
        decimal Score,
        int Rank
    );

    /// <summary>
    /// Computes dense-ranked list of enrolled students sorted by descending score.
    /// Also returns the last snapshot map for delta calculation.
    /// </summary>
    private async Task<(List<RankedStudent> Ranked, Dictionary<Guid, int> SnapshotMap)>
        ComputeRanksAsync(Guid courseId, LeaderboardCourseMetric metric, CancellationToken ct)
    {
        // Get all published grade records for the course
        var gradeRecords = await _db.GradeRecords
            .AsNoTracking()
            .Where(g => g.CourseId == courseId && g.IsPublished)
            .ToListAsync(ct);

        // Get enrolled students
        var enrolledStudents = await _db.CourseEnrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .Where(e => e.CourseId == courseId && e.Status == CourseEnrollmentStatus.Approved)
            .Select(e => e.Student!)
            .ToListAsync(ct);

        // Compute score per student based on metric
        var scores = enrolledStudents.Select(student =>
        {
            var records = gradeRecords.Where(g => g.StudentId == student.Id).ToList();
            decimal score = metric switch
            {
                LeaderboardCourseMetric.TotalScore =>
                    records.Sum(g => g.MaxScore > 0 ? (g.Score / g.MaxScore) * g.Weight : 0m),

                LeaderboardCourseMetric.ExamAverage =>
                    records.Any(g => g.Type == GradeType.Exam)
                        ? records.Where(g => g.Type == GradeType.Exam)
                                 .Average(g => g.MaxScore > 0 ? (g.Score / g.MaxScore) * 100m : 0m)
                        : 0m,

                LeaderboardCourseMetric.AssignmentAverage =>
                    records.Any(g => g.Type == GradeType.Assignment)
                        ? records.Where(g => g.Type == GradeType.Assignment)
                                 .Average(g => g.MaxScore > 0 ? (g.Score / g.MaxScore) * 100m : 0m)
                        : 0m,

                _ => 0m
            };

            return (Student: student, Score: Math.Round(score, 4));
        })
        .OrderByDescending(x => x.Score)
        .ToList();

        // Dense ranking: same score → same rank
        var ranked = new List<RankedStudent>();
        int currentRank = 1;
        for (int i = 0; i < scores.Count; i++)
        {
            if (i > 0 && scores[i].Score < scores[i - 1].Score)
                currentRank = i + 1;

            ranked.Add(new RankedStudent(
                StudentId: scores[i].Student.Id,
                FullName: $"{scores[i].Student.FirstName} {scores[i].Student.LastName}",
                DisplayId: scores[i].Student.StudentId,
                AvatarUrl: scores[i].Student.ProfilePictureUrl,
                Score: scores[i].Score,
                Rank: currentRank
            ));
        }

        // Load previous snapshot ranks for delta
        var prevSnapshots = await _db.LeaderboardRankSnapshots
            .AsNoTracking()
            .Where(s => s.CourseId == courseId)
            .ToDictionaryAsync(s => s.StudentId, s => s.RankPosition, ct);

        return (ranked, prevSnapshots);
    }

    /// <summary>Upsert rank snapshots for all ranked students.</summary>
    private async Task UpsertSnapshotsAsync(Guid courseId, List<RankedStudent> ranked, CancellationToken ct)
    {
        var existingSnapshots = await _db.LeaderboardRankSnapshots
            .Where(s => s.CourseId == courseId)
            .ToListAsync(ct);

        var snapshotMap = existingSnapshots.ToDictionary(s => s.StudentId);
        var now = DateTime.UtcNow;

        foreach (var r in ranked)
        {
            if (snapshotMap.TryGetValue(r.StudentId, out var existing))
            {
                existing.RankPosition = r.Rank;
                existing.Score        = r.Score;
                existing.SnapshotAt   = now;
            }
            else
            {
                _db.LeaderboardRankSnapshots.Add(new LeaderboardRankSnapshot
                {
                    Id          = Guid.NewGuid(),
                    CourseId    = courseId,
                    StudentId   = r.StudentId,
                    RankPosition = r.Rank,
                    Score       = r.Score,
                    SnapshotAt  = now
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private static LeaderboardEntryResponse ToEntry(RankedStudent r, Dictionary<Guid, int> snapshotMap)
    {
        int? delta = snapshotMap.TryGetValue(r.StudentId, out var prevRank)
            ? prevRank - r.Rank  // positive = rank improved (went up), negative = fell
            : null;

        return new LeaderboardEntryResponse(
            Rank: r.Rank,
            StudentId: r.StudentId,
            StudentName: r.FullName,
            StudentDisplayId: r.DisplayId,
            AvatarUrl: r.AvatarUrl,
            Score: r.Score,
            RankDelta: delta
        );
    }

    private async Task<LeaderboardSettings> GetOrCreateSettingsAsync(Guid courseId, CancellationToken ct)
    {
        var settings = await _db.LeaderboardSettings
            .FirstOrDefaultAsync(s => s.CourseId == courseId, ct);

        if (settings == null)
        {
            settings = new LeaderboardSettings
            {
                Id        = Guid.NewGuid(),
                CourseId  = courseId,
                IsLeaderboardVisible = false,
                ShowStudentOwnRank   = false,
                ScoringMetric        = LeaderboardCourseMetric.TotalScore,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.LeaderboardSettings.Add(settings);
            await _db.SaveChangesAsync(ct);
        }

        return settings;
    }

    private async Task ValidateInstructorAccessAsync(
        Guid courseId, Guid userId, string role, CancellationToken ct)
    {
        if (role == "Student")
            throw new ForbiddenException("Students cannot manage leaderboard settings.");

        if (role == "Tutor")
        {
            var course = await _db.Courses.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId, ct)
                ?? throw new NotFoundException("Course", courseId);

            if (course.AssignedTutorId != userId)
                throw new ForbiddenException("You can only manage leaderboard settings for courses assigned to you.");
        }
    }

    private static LeaderboardSettingsResponse MapSettings(LeaderboardSettings s) =>
        new(s.Id, s.CourseId, s.IsLeaderboardVisible, s.ShowStudentOwnRank,
            s.ScoringMetric.ToString(), s.UpdatedAt);
}
