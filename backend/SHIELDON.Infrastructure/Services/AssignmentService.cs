using System.IO.Compression;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Courses.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements the full assignment management lifecycle for SHIELDON courses.
///
/// Tutor/Admin creates Assignments (task + optional reference file + optional due date).
/// Students submit AssignmentSubmission files (their answer) against a specific Assignment.
/// Tutor/Admin downloads individual submissions or all submissions as a ZIP archive.
///
/// Storage layout (relative to WebRootPath):
///   Reference files : Storage/Uploads/assignments/{courseId}/reference/{storedFileName}
///   Submissions     : Storage/Uploads/assignments/{courseId}/submissions/{assignmentId}/{storedFileName}
/// </summary>
public class AssignmentService : IAssignmentService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    // ── Allowed file types ─────────────────────────────────────────────────

    /// <summary>Allowed MIME types for Tutor-uploaded reference files (max 50 MB).</summary>
    private static readonly HashSet<string> AllowedReferenceMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "image/jpeg",
        "image/png",
        "application/zip",
        "application/x-zip-compressed"
    };

    /// <summary>Allowed MIME types for student submission files (max 100 MB).</summary>
    private static readonly HashSet<string> AllowedSubmissionMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "image/jpeg",
        "image/png",
        "image/gif",
        "video/mp4",
        "video/quicktime",
        "video/x-msvideo",
        "application/zip",
        "application/x-zip-compressed",
        "application/x-rar-compressed",
        "application/vnd.rar"
    };

    private const long MaxReferenceSizeBytes  = 50L  * 1024 * 1024; // 50 MB
    private const long MaxSubmissionSizeBytes = 100L * 1024 * 1024; // 100 MB

    public AssignmentService(AppDbContext db, IWebHostEnvironment env)
    {
        _db  = db;
        _env = env;
    }

    // ── Create Assignment ──────────────────────────────────────────────────

    public async Task<AssignmentResponse> CreateAssignmentAsync(
        Guid courseId,
        CreateAssignmentRequest request,
        UploadedFileDto? referenceFile,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        // Verify course exists
        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Course", courseId);

        // RBAC: Admin always allowed; Tutor only if assigned to this course
        if (requestingUserRole == "Tutor" && course.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only create assignments for courses assigned to you.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BusinessRuleException("Assignment title cannot be empty.");

        // ── Handle optional reference file ──────────────────────
        string? referenceFileName         = null;
        string? referenceStoredFileName   = null;
        string? referenceFilePath         = null;
        long?   referenceFileSizeBytes    = null;
        string? referenceContentType      = null;

        if (referenceFile is not null && referenceFile.Length > 0)
        {
            if (!AllowedReferenceMimeTypes.Contains(referenceFile.ContentType))
                throw new BusinessRuleException(
                    $"Reference file type '{referenceFile.ContentType}' is not allowed. " +
                    "Permitted: PDF, Word, PowerPoint, Excel, JPEG, PNG, ZIP.");

            if (referenceFile.Length > MaxReferenceSizeBytes)
                throw new BusinessRuleException(
                    $"Reference file ({referenceFile.Length / 1024 / 1024} MB) exceeds the 50 MB limit.");

            // We need the assignment ID for the path, so create entity first, save, then write file.
            // We'll write file after entity creation below.
        }

        // Create entity (without file path yet — we'll update if file was provided)
        var creator = await _db.Users.FindAsync(new object[] { requestingUserId }, ct)
            ?? throw new NotFoundException("User", requestingUserId);

        var assignment = new Assignment
        {
            CourseId         = courseId,
            CreatedByUserId  = requestingUserId,
            Title            = request.Title.Trim(),
            Instructions     = request.Instructions?.Trim(),
            DueDate          = request.DueDate?.ToUniversalTime(),
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        };

        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(ct); // Save to get the Guid Id

        // ── Write reference file to disk now that we have the assignment Id ──
        if (referenceFile is not null && referenceFile.Length > 0)
        {
            var ext          = Path.GetExtension(referenceFile.FileName);
            var storedName   = $"{Guid.NewGuid()}{ext}";
            var folderPath   = Path.Combine(_env.WebRootPath, "Storage", "Uploads", "assignments",
                                            courseId.ToString(), "reference");
            Directory.CreateDirectory(folderPath);

            var absolutePath = Path.Combine(folderPath, storedName);
            await using var fs = new FileStream(absolutePath, FileMode.Create);
            await referenceFile.Content.CopyToAsync(fs, ct);

            referenceFileName       = referenceFile.FileName;
            referenceStoredFileName = storedName;
            referenceFilePath       = Path.Combine("Storage", "Uploads", "assignments",
                                          courseId.ToString(), "reference", storedName)
                                          .Replace('\\', '/');
            referenceFileSizeBytes  = referenceFile.Length;
            referenceContentType    = referenceFile.ContentType;

            // Update entity with file info
            assignment.ReferenceFileName       = referenceFileName;
            assignment.ReferenceStoredFileName = referenceStoredFileName;
            assignment.ReferenceFilePath       = referenceFilePath;
            assignment.ReferenceFileSizeBytes  = referenceFileSizeBytes;
            assignment.ReferenceContentType    = referenceContentType;
            assignment.UpdatedAt               = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
        }

        return MapAssignmentToResponse(assignment, creator, submissionCount: 0, mySubmission: null);
    }

    // ── Get Assignments ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AssignmentResponse>> GetAssignmentsAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        // Verify course exists
        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Course", courseId);

        // Students must be Approved-enrolled
        if (requestingUserRole == "Student")
        {
            var isEnrolled = await _db.CourseEnrollments.AnyAsync(
                e => e.CourseId == courseId
                  && e.StudentId == requestingUserId
                  && e.Status == CourseEnrollmentStatus.Approved,
                ct);

            if (!isEnrolled)
                throw new ForbiddenException("You must be enrolled in this course to view assignments.");
        }

        var assignments = await _db.Assignments
            .Include(a => a.CreatedByUser)
            .Include(a => a.Submissions)
                .ThenInclude(s => s.Student)
            .Where(a => a.CourseId == courseId)
            .OrderByDescending(a => a.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return assignments.Select(a =>
        {
            AssignmentSubmissionResponse? mySubmission = null;
            if (requestingUserRole == "Student")
            {
                var sub = a.Submissions.FirstOrDefault(s => s.StudentId == requestingUserId);
                if (sub is not null) mySubmission = MapSubmissionToResponse(sub);
            }

            return MapAssignmentToResponse(
                a,
                a.CreatedByUser!,
                submissionCount: a.Submissions.Count,
                mySubmission: mySubmission);

        }).ToList();
    }

    // ── Update Assignment ──────────────────────────────────────────────────

    public async Task<AssignmentResponse> UpdateAssignmentAsync(
        Guid assignmentId,
        UpdateAssignmentRequest request,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var assignment = await _db.Assignments
            .Include(a => a.Course)
            .Include(a => a.CreatedByUser)
            .Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new NotFoundException("Assignment", assignmentId);

        // RBAC
        if (requestingUserRole == "Tutor" && assignment.Course!.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only update assignments for courses assigned to you.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BusinessRuleException("Assignment title cannot be empty.");

        assignment.Title        = request.Title.Trim();
        assignment.Instructions = request.Instructions?.Trim();
        assignment.DueDate      = request.DueDate?.ToUniversalTime();
        assignment.UpdatedAt    = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return MapAssignmentToResponse(
            assignment,
            assignment.CreatedByUser!,
            submissionCount: assignment.Submissions.Count,
            mySubmission: null);
    }

    // ── Delete Assignment ──────────────────────────────────────────────────

    public async Task DeleteAssignmentAsync(
        Guid assignmentId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var assignment = await _db.Assignments
            .Include(a => a.Course)
            .Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new NotFoundException("Assignment", assignmentId);

        // RBAC
        if (requestingUserRole == "Tutor" && assignment.Course!.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only delete assignments for courses assigned to you.");

        // Delete reference file from disk
        if (assignment.ReferenceFilePath is not null)
        {
            var refAbsPath = Path.Combine(_env.WebRootPath, assignment.ReferenceFilePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(refAbsPath)) File.Delete(refAbsPath);
        }

        // Delete all submission files from disk
        foreach (var sub in assignment.Submissions)
        {
            var subAbsPath = Path.Combine(_env.WebRootPath, sub.FilePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(subAbsPath)) File.Delete(subAbsPath);
        }

        _db.Assignments.Remove(assignment); // Cascade removes submissions from DB
        await _db.SaveChangesAsync(ct);
    }

    // ── Download Reference File ────────────────────────────────────────────

    public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadReferenceFileAsync(
        Guid assignmentId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var assignment = await _db.Assignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new NotFoundException("Assignment", assignmentId);

        if (assignment.ReferenceFilePath is null)
            throw new NotFoundException("Reference file", assignmentId);

        // Students must be enrolled in the course
        if (requestingUserRole == "Student")
        {
            var isEnrolled = await _db.CourseEnrollments.AnyAsync(
                e => e.CourseId == assignment.CourseId
                  && e.StudentId == requestingUserId
                  && e.Status == CourseEnrollmentStatus.Approved,
                ct);

            if (!isEnrolled)
                throw new ForbiddenException("You must be enrolled in this course to download assignment files.");
        }

        var absolutePath = Path.Combine(_env.WebRootPath, assignment.ReferenceFilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolutePath))
            throw new NotFoundException("Reference file on disk", assignmentId);

        var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, assignment.ReferenceContentType ?? "application/octet-stream", assignment.ReferenceFileName ?? "reference");
    }

    // ── Submit Assignment ──────────────────────────────────────────────────

    public async Task<AssignmentSubmissionResponse> SubmitAssignmentAsync(
        Guid assignmentId,
        Guid studentId,
        UploadedFileDto file,
        CancellationToken ct = default)
    {
        var assignment = await _db.Assignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new NotFoundException("Assignment", assignmentId);

        // Student must be enrolled
        var isEnrolled = await _db.CourseEnrollments.AnyAsync(
            e => e.CourseId == assignment.CourseId
              && e.StudentId == studentId
              && e.Status == CourseEnrollmentStatus.Approved,
            ct);

        if (!isEnrolled)
            throw new ForbiddenException("You must be enrolled in this course to submit assignments.");

        // Deadline guard
        if (assignment.DueDate.HasValue && assignment.DueDate.Value < DateTime.UtcNow)
            throw new BusinessRuleException("Assignment submission deadline has passed.");

        // One-submission-per-student guard
        var alreadySubmitted = await _db.AssignmentSubmissions.AnyAsync(
            s => s.AssignmentId == assignmentId && s.StudentId == studentId, ct);

        if (alreadySubmitted)
            throw new ConflictException("You have already submitted for this assignment. Delete your existing submission first.");

        // Validate file
        if (file.Length == 0)
            throw new BusinessRuleException("Submission file cannot be empty.");

        if (!AllowedSubmissionMimeTypes.Contains(file.ContentType))
            throw new BusinessRuleException(
                $"File type '{file.ContentType}' is not allowed for submissions. " +
                "Permitted: PDF, Word, PowerPoint, Excel, images, video (MP4/MOV/AVI), ZIP, RAR.");

        if (file.Length > MaxSubmissionSizeBytes)
            throw new BusinessRuleException($"File size ({file.Length / 1024 / 1024} MB) exceeds the 100 MB limit.");

        // Write file to disk
        var ext        = Path.GetExtension(file.FileName);
        var storedName = $"{Guid.NewGuid()}{ext}";
        var folderPath = Path.Combine(
            _env.WebRootPath, "Storage", "Uploads", "assignments",
            assignment.CourseId.ToString(), "submissions", assignmentId.ToString());

        Directory.CreateDirectory(folderPath);

        var absolutePath = Path.Combine(folderPath, storedName);
        await using var fs = new FileStream(absolutePath, FileMode.Create);
        await file.Content.CopyToAsync(fs, ct);

        var relPath = Path.Combine(
            "Storage", "Uploads", "assignments",
            assignment.CourseId.ToString(), "submissions", assignmentId.ToString(), storedName)
            .Replace('\\', '/');

        var submission = new AssignmentSubmission
        {
            AssignmentId     = assignmentId,
            StudentId        = studentId,
            OriginalFileName = file.FileName,
            StoredFileName   = storedName,
            FilePath         = relPath,
            FileSizeBytes    = file.Length,
            ContentType      = file.ContentType,
            SubmittedAt      = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        };

        _db.AssignmentSubmissions.Add(submission);
        await _db.SaveChangesAsync(ct);

        // Load student for response
        var student = await _db.Users.FindAsync(new object[] { studentId }, ct);
        submission.Student = student;

        return MapSubmissionToResponse(submission);
    }

    // ── Delete Submission ──────────────────────────────────────────────────

    public async Task DeleteSubmissionAsync(
        Guid submissionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var submission = await _db.AssignmentSubmissions
            .Include(s => s.Assignment)
                .ThenInclude(a => a!.Course)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException("Submission", submissionId);

        if (requestingUserRole == "Student")
        {
            // Students can only delete their OWN submission
            if (submission.StudentId != requestingUserId)
                throw new ForbiddenException("You can only delete your own submission.");

            // Deadline guard for students
            if (submission.Assignment!.DueDate.HasValue &&
                submission.Assignment.DueDate.Value < DateTime.UtcNow)
                throw new BusinessRuleException("Submission deadline has passed. You can no longer delete your submission.");
        }
        else if (requestingUserRole == "Tutor")
        {
            // Tutor must be assigned to the course
            if (submission.Assignment!.Course!.AssignedTutorId != requestingUserId)
                throw new ForbiddenException("You can only manage submissions for courses assigned to you.");
        }
        // Admin: no additional checks needed

        // Delete physical file
        var absolutePath = Path.Combine(_env.WebRootPath, submission.FilePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath)) File.Delete(absolutePath);

        _db.AssignmentSubmissions.Remove(submission);
        await _db.SaveChangesAsync(ct);
    }

    // ── Get Submissions (Tutor/Admin) ──────────────────────────────────────

    public async Task<IReadOnlyList<AssignmentSubmissionResponse>> GetSubmissionsAsync(
        Guid assignmentId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var assignment = await _db.Assignments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new NotFoundException("Assignment", assignmentId);

        // RBAC: Tutor or Admin only
        if (requestingUserRole == "Student")
            throw new ForbiddenException("Students cannot view all submissions for an assignment.");

        if (requestingUserRole == "Tutor" && assignment.Course!.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only view submissions for courses assigned to you.");

        var submissions = await _db.AssignmentSubmissions
            .Include(s => s.Student)
            .Where(s => s.AssignmentId == assignmentId)
            .OrderBy(s => s.SubmittedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return submissions.Select(MapSubmissionToResponse).ToList();
    }

    // ── Download Single Submission ─────────────────────────────────────────

    public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadSubmissionAsync(
        Guid submissionId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var submission = await _db.AssignmentSubmissions
            .Include(s => s.Assignment)
                .ThenInclude(a => a!.Course)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException("Submission", submissionId);

        if (requestingUserRole == "Student" && submission.StudentId != requestingUserId)
            throw new ForbiddenException("You can only download your own submission.");

        if (requestingUserRole == "Tutor" && submission.Assignment!.Course!.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only download submissions for courses assigned to you.");

        var absolutePath = Path.Combine(_env.WebRootPath, submission.FilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolutePath))
            throw new NotFoundException("Submission file on disk", submissionId);

        var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, submission.ContentType, submission.OriginalFileName);
    }

    // ── Download All Submissions as ZIP ────────────────────────────────────

    public async Task<(Stream? ZipStream, string ZipFileName)> DownloadAllSubmissionsAsZipAsync(
        Guid assignmentId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var assignment = await _db.Assignments
            .Include(a => a.Course)
            .Include(a => a.Submissions)
                .ThenInclude(s => s.Student)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new NotFoundException("Assignment", assignmentId);

        // RBAC: Tutor or Admin only
        if (requestingUserRole == "Student")
            throw new ForbiddenException("Students cannot download bulk assignment submissions.");

        if (requestingUserRole == "Tutor" && assignment.Course!.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only download submissions for courses assigned to you.");

        if (!assignment.Submissions.Any())
            return (null, string.Empty); // 204 No Content

        // Build ZIP filename: {CourseCode}_{AssignmentTitle}_{yyyy-MM-dd}.zip
        var courseCode     = assignment.Course!.CourseCode;
        var safeTitle      = SanitizeForFileName(assignment.Title);
        var dateStamp      = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var zipFileName    = $"{courseCode}_{safeTitle}_{dateStamp}.zip";

        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var sub in assignment.Submissions)
            {
                var absolutePath = Path.Combine(_env.WebRootPath, sub.FilePath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(absolutePath)) continue;

                var studentName     = sub.Student is not null
                    ? $"{sub.Student.FirstName}_{sub.Student.LastName}"
                    : "Unknown";
                var studentId       = sub.StudentId.ToString("N")[..8]; // short prefix
                var entryPath       = $"{studentId}_{studentName}/{sub.OriginalFileName}";

                var entry = archive.CreateEntry(entryPath, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await using var fileStream  = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await fileStream.CopyToAsync(entryStream, ct);
            }
        }

        memoryStream.Position = 0;
        return (memoryStream, zipFileName);
    }

    // ── Private Helpers ───────────────────────────────────────────────────

    private static AssignmentResponse MapAssignmentToResponse(
        Assignment a,
        User creator,
        int submissionCount,
        AssignmentSubmissionResponse? mySubmission)
    {
        var now = DateTime.UtcNow;
        return new AssignmentResponse(
            a.Id,
            a.CourseId,
            a.Title,
            a.Instructions,
            $"{creator.FirstName} {creator.LastName}",
            HasReferenceFile:         a.ReferenceFilePath is not null,
            ReferenceFileName:        a.ReferenceFileName,
            ReferenceFileExtension:   a.ReferenceFileName is not null ? Path.GetExtension(a.ReferenceFileName) : null,
            ReferenceFileSizeBytes:   a.ReferenceFileSizeBytes,
            DueDate:                  a.DueDate,
            IsPastDue:                a.DueDate.HasValue && a.DueDate.Value < now,
            SubmissionCount:          submissionCount,
            MySubmission:             mySubmission,
            CreatedAt:                a.CreatedAt
        );
    }

    private static AssignmentSubmissionResponse MapSubmissionToResponse(AssignmentSubmission s)
    {
        var studentName = s.Student is not null
            ? $"{s.Student.FirstName} {s.Student.LastName}"
            : "Unknown";

        // StudentId is the human-readable display ID (e.g., "STU001"), not the GUID
        var studentDisplayId = s.Student?.StudentId;

        return new AssignmentSubmissionResponse(
            s.Id,
            s.AssignmentId,
            s.StudentId,
            studentName,
            studentDisplayId,
            s.OriginalFileName,
            Path.GetExtension(s.OriginalFileName),
            s.FileSizeBytes,
            s.SubmittedAt
        );
    }

    private static string SanitizeForFileName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(input.Select(c => invalid.Contains(c) ? '_' : c))
                     .Replace(' ', '_')
                     .Trim('_');
    }
}
