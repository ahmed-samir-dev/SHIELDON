using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Grades.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using System.Text;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements the Grade Management Panel feature:
///   - Tutor/Admin: view all grades per course (per-student summary), set weights,
///     override scores, add notes, publish (individual or bulk), export CSV.
///   - Student: view only their own published grades (per course or all courses).
///
/// Weight propagation rule: setting a weight on one GradeRecord propagates
/// the same weight to ALL records for the same exam/assignment in that course.
/// </summary>
public class GradeService : IGradeService
{
    private readonly AppDbContext _db;

    public GradeService(AppDbContext db)
    {
        _db = db;
    }

    // ── Tutor/Admin: Course Grade Summary (paginated per student) ─────────────

    public async Task<PagedResponse<CourseGradeSummaryResponse>> GetCourseGradesAsync(
        Guid courseId,
        GradeQueryParams query,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        // Verify course + RBAC
        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Course", courseId);

        if (requestingUserRole == "Tutor" && course.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only view grades for courses assigned to you.");

        if (requestingUserRole == "Student")
            throw new ForbiddenException("Use the /my-grades endpoint to view your own grades.");

        // Collect enrolled students
        var enrolledQuery = _db.CourseEnrollments
            .Include(e => e.Student)
            .Where(e => e.CourseId == courseId && e.Status == Domain.Enums.CourseEnrollmentStatus.Approved)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            enrolledQuery = enrolledQuery.Where(e =>
                (e.Student!.FirstName + " " + e.Student.LastName).ToLower().Contains(term) ||
                (e.Student.StudentId != null && e.Student.StudentId.ToLower().Contains(term)));
        }

        var totalStudents = await enrolledQuery.CountAsync(ct);

        var enrollments = await enrolledQuery
            .OrderBy(e => e.Student!.LastName)
            .ThenBy(e => e.Student!.FirstName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var studentIds = enrollments.Select(e => e.StudentId).ToList();

        // Load grade records for these students in this course
        var gradeQuery = _db.GradeRecords
            .Include(g => g.Exam)
            .Include(g => g.Assignment)
            .Where(g => g.CourseId == courseId && studentIds.Contains(g.StudentId))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Type))
            gradeQuery = gradeQuery.Where(g => g.Type.ToString() == query.Type);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            bool published = query.Status == "Published";
            gradeQuery = gradeQuery.Where(g => g.IsPublished == published);
        }

        var allGrades = await gradeQuery.AsNoTracking().ToListAsync(ct);

        // Build per-student summaries
        var summaries = enrollments.Select(e =>
        {
            var student = e.Student!;
            var studentGrades = allGrades
                .Where(g => g.StudentId == e.StudentId)
                .Select(MapToGradeItemResponse)
                .ToList();

            decimal totalWeight   = studentGrades.Sum(g => g.Weight);
            decimal? finalScore   = studentGrades.Any() ? studentGrades.Sum(g => g.WeightedScore) : null;

            return new CourseGradeSummaryResponse(
                StudentId:           student.Id,
                StudentName:         $"{student.FirstName} {student.LastName}",
                StudentDisplayId:    student.StudentId,
                StudentEmail:        student.Email,
                Grades:              studentGrades,
                TotalWeightAssigned: totalWeight,
                FinalWeightedScore:  finalScore
            );
        }).ToList();

        return new PagedResponse<CourseGradeSummaryResponse>
        {
            Items      = summaries,
            TotalCount = totalStudents,
            PageNumber = query.Page,
            PageSize   = query.PageSize
        };
    }

    // ── Tutor/Admin: Update Grade (weight/score/notes) ────────────────────────

    public async Task<GradeItemResponse> UpdateGradeAsync(
        Guid gradeId,
        UpdateGradeRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var grade = await _db.GradeRecords
            .Include(g => g.Exam)
            .Include(g => g.Assignment)
            .FirstOrDefaultAsync(g => g.Id == gradeId, ct)
            ?? throw new NotFoundException("Grade record", gradeId);

        // RBAC via course
        var course = await _db.Courses.FindAsync(new object[] { grade.CourseId }, ct)
            ?? throw new NotFoundException("Course", grade.CourseId);

        if (requestingUserRole == "Tutor" && course.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only update grades for courses assigned to you.");

        if (requestingUserRole == "Student")
            throw new ForbiddenException("Students cannot modify grade records.");

        // Apply score override
        if (request.Score.HasValue)
        {
            if (request.Score.Value < 0 || request.Score.Value > grade.MaxScore)
                throw new BusinessRuleException($"Score must be between 0 and {grade.MaxScore}.");
            grade.Score = Math.Round(request.Score.Value, 2);

            if (grade.AssignmentId.HasValue)
            {
                var submission = await _db.AssignmentSubmissions
                    .OrderByDescending(s => s.SubmittedAt)
                    .FirstOrDefaultAsync(s => s.AssignmentId == grade.AssignmentId && s.StudentId == grade.StudentId, ct);
                
                if (submission != null)
                {
                    var assignment = await _db.Assignments.FindAsync(new object[] { grade.AssignmentId }, ct);
                    if (assignment != null && grade.MaxScore > 0)
                    {
                        submission.PointsAwarded = (grade.Score / grade.MaxScore) * assignment.MaxPoints;
                        submission.ReviewedById = requestingUserId;
                        submission.ReviewedAt = DateTime.UtcNow;
                        submission.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }

        // Apply notes
        if (request.Notes is not null)
            grade.Notes = request.Notes.Trim();

        grade.UpdatedAt = DateTime.UtcNow;

        // Weight changes are now handled strictly via Assignment and Exam settings

        await _db.SaveChangesAsync(ct);

        // Reload with navigation for response
        await _db.Entry(grade).Reference(g => g.Exam).LoadAsync(ct);
        await _db.Entry(grade).Reference(g => g.Assignment).LoadAsync(ct);

        return MapToGradeItemResponse(grade);
    }

    // ── Tutor/Admin: Bulk Publish ─────────────────────────────────────────────

    public async Task<string> PublishGradesAsync(
        Guid courseId,
        BulkPublishRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var course = await _db.Courses.FindAsync(new object[] { courseId }, ct)
            ?? throw new NotFoundException("Course", courseId);

        if (requestingUserRole == "Tutor" && course.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only publish grades for courses assigned to you.");

        if (requestingUserRole == "Student")
            throw new ForbiddenException("Students cannot publish grades.");

        IQueryable<Domain.Entities.GradeRecord> query = _db.GradeRecords
            .Where(g => g.CourseId == courseId && !g.IsPublished);

        if (!request.PublishAll && request.GradeIds?.Count > 0)
            query = query.Where(g => request.GradeIds.Contains(g.Id));

        var records = await query.ToListAsync(ct);
        if (!records.Any())
            return "No unpublished grade records found matching the criteria.";

        var now = DateTime.UtcNow;
        foreach (var r in records)
        {
            r.IsPublished  = true;
            r.PublishedAt  = now;
            r.UpdatedAt    = now;
        }

        await _db.SaveChangesAsync(ct);
        return $"{records.Count} grade record(s) published successfully.";
    }

    // ── Tutor/Admin: CSV Export ───────────────────────────────────────────────

    public async Task<(byte[] CsvBytes, string FileName)> ExportGradesCsvAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var course = await _db.Courses.FindAsync(new object[] { courseId }, ct)
            ?? throw new NotFoundException("Course", courseId);

        if (requestingUserRole == "Tutor" && course.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only export grades for courses assigned to you.");

        if (requestingUserRole == "Student")
            throw new ForbiddenException("Students cannot export grade data.");

        var grades = await _db.GradeRecords
            .Include(g => g.Student)
            .Include(g => g.Exam)
            .Include(g => g.Assignment)
            .Where(g => g.CourseId == courseId)
            .OrderBy(g => g.Student!.LastName)
            .ThenBy(g => g.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("StudentName,StudentID,ItemTitle,Type,Score,MaxScore,Weight,WeightedScore,Status,PublishedAt");

        foreach (var g in grades)
        {
            var studentName = g.Student is not null ? $"{g.Student.FirstName} {g.Student.LastName}" : "";
            var studentId   = g.Student?.StudentId ?? "";
            var title       = g.Exam?.Title ?? g.Assignment?.Title ?? "";
            var weighted    = g.MaxScore > 0 ? Math.Round((g.Score / g.MaxScore) * g.Weight, 2) : 0m;
            var status      = g.IsPublished ? "Published" : "Unpublished";
            var published   = g.PublishedAt.HasValue ? g.PublishedAt.Value.ToString("yyyy-MM-dd") : "";

            sb.AppendLine($"\"{studentName}\",\"{studentId}\",\"{title}\",{g.Type},{g.Score},{g.MaxScore},{g.Weight},{weighted},{status},{published}");
        }

        var fileName = $"Grades_{course.CourseCode}_{DateTime.UtcNow:yyyy-MM-dd}.csv";
        return (Encoding.UTF8.GetBytes(sb.ToString()), fileName);
    }

    // ── Student: My Grades (all courses) ─────────────────────────────────────

    public async Task<IReadOnlyList<MyGradeItemResponse>> GetMyGradesAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        var grades = await _db.GradeRecords
            .Include(g => g.Course)
            .Include(g => g.Exam)
            .Include(g => g.Assignment)
            .Where(g => g.StudentId == studentId && g.IsPublished)
            .OrderBy(g => g.Course!.Title)
            .ThenBy(g => g.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return grades.Select(MapToMyGradeItemResponse).ToList();
    }

    // ── Student: My Grades (per course) ──────────────────────────────────────

    public async Task<IReadOnlyList<MyGradeItemResponse>> GetMyGradesForCourseAsync(
        Guid courseId,
        Guid studentId,
        CancellationToken ct = default)
    {
        // Verify enrollment
        var isEnrolled = await _db.CourseEnrollments.AnyAsync(
            e => e.CourseId == courseId &&
                 e.StudentId == studentId &&
                 e.Status == Domain.Enums.CourseEnrollmentStatus.Approved, ct);

        if (!isEnrolled)
            throw new ForbiddenException("You must be enrolled in this course to view your grades.");

        var grades = await _db.GradeRecords
            .Include(g => g.Course)
            .Include(g => g.Exam)
            .Include(g => g.Assignment)
            .Where(g => g.CourseId == courseId && g.StudentId == studentId && g.IsPublished)
            .OrderBy(g => g.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return grades.Select(MapToMyGradeItemResponse).ToList();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private static GradeItemResponse MapToGradeItemResponse(Domain.Entities.GradeRecord g)
    {
        decimal weighted = g.MaxScore > 0 ? Math.Round((g.Score / g.MaxScore) * g.Weight, 2) : 0m;
        return new GradeItemResponse(
            Id:              g.Id,
            StudentId:       g.StudentId,
            StudentName:     g.Student is not null ? $"{g.Student.FirstName} {g.Student.LastName}" : string.Empty,
            StudentDisplayId: g.Student?.StudentId,
            StudentEmail:    g.Student?.Email ?? string.Empty,
            CourseId:        g.CourseId,
            ExamId:          g.ExamId,
            ExamTitle:       g.Exam?.Title,
            AssignmentId:    g.AssignmentId,
            AssignmentTitle: g.Assignment?.Title,
            Type:            g.Type.ToString(),
            Score:           g.Score,
            MaxScore:        g.MaxScore,
            Weight:          g.Weight,
            WeightedScore:   weighted,
            IsPublished:     g.IsPublished,
            PublishedAt:     g.PublishedAt,
            Notes:           g.Notes,
            CreatedAt:       g.CreatedAt,
            UpdatedAt:       g.UpdatedAt
        );
    }

    private static MyGradeItemResponse MapToMyGradeItemResponse(Domain.Entities.GradeRecord g)
    {
        decimal weighted = g.MaxScore > 0 ? Math.Round((g.Score / g.MaxScore) * g.Weight, 2) : 0m;
        return new MyGradeItemResponse(
            Id:              g.Id,
            CourseId:        g.CourseId,
            CourseTitle:     g.Course?.Title ?? string.Empty,
            ExamId:          g.ExamId,
            ExamTitle:       g.Exam?.Title,
            AssignmentId:    g.AssignmentId,
            AssignmentTitle: g.Assignment?.Title,
            Type:            g.Type.ToString(),
            Score:           g.Score,
            MaxScore:        g.MaxScore,
            Weight:          g.Weight,
            WeightedScore:   weighted,
            PublishedAt:     g.PublishedAt
        );
    }
}
