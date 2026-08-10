using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Courses.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

using SHIELDON.Application.Common;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implements course material upload, listing, download, and deletion.
/// Enforces RBAC: only Admin/assigned Tutor can upload/delete.
/// Only Approved-enrolled students (or Admin/Tutor) can list/download.
/// Business rules from CLAUDE.md §17 (Feature 6) are enforced here.
/// </summary>
public class MaterialService : IMaterialService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly INotificationService _notificationService;
    private readonly IUserActivityLogger _activityLogger;

    // ── Allowed File Types ────────────────────────────────────────────────
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain",
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "video/mp4",
        "video/webm",
        "application/zip",
        "application/x-zip-compressed"
    };

    private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB

    public MaterialService(AppDbContext db, IWebHostEnvironment env, INotificationService notificationService, IUserActivityLogger? activityLogger = null)
    {
        _db = db;
        _env = env;
        _notificationService = notificationService;
        _activityLogger = activityLogger ?? new NullUserActivityLogger();
    }

    // ── Add Material ──────────────────────────────────────────────────────

    public async Task<MaterialResponse> AddMaterialAsync(
        Guid courseId,
        AddMaterialRequest request,
        UploadedFileDto? file,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        // Verify course exists
        var course = await _db.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Course", courseId);

        // Authorization: Admin always allowed; Tutor only if assigned to this course
        if (requestingUserRole == "Tutor" && course.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only upload materials to courses assigned to you.");

        // Parse material type
        if (!Enum.TryParse<MaterialType>(request.MaterialType, ignoreCase: true, out var materialType))
            throw new BusinessRuleException($"Invalid material type '{request.MaterialType}'. Use 'File' or 'Link'.");

        string? filePath = null;
        string? originalFileName = null;
        string? contentType = null;
        long? fileSizeBytes = null;
        string? externalUrl = null;

        if (materialType == MaterialType.File)
        {
            // Validate file was provided
            if (file is null || file.Length == 0)
                throw new BusinessRuleException("A file must be provided when MaterialType is 'File'.");

            // Validate MIME type
            if (!AllowedMimeTypes.Contains(file.ContentType))
                throw new BusinessRuleException(
                    $"File type '{file.ContentType}' is not allowed. Permitted types: PDF, Word, PowerPoint, Excel, plain text, images (JPEG/PNG/GIF/WebP), video (MP4/WebM), ZIP.");

            // Validate file size
            if (file.Length > MaxFileSizeBytes)
                throw new BusinessRuleException($"File size ({file.Length / 1024 / 1024} MB) exceeds the 100 MB limit.");

            // Build storage path: wwwroot/Storage/Uploads/course-materials/{courseId}/
            var storageFolder = Path.Combine(
                _env.WebRootPath,
                "Storage", "Uploads", "course-materials",
                courseId.ToString());

            Directory.CreateDirectory(storageFolder);

            // Generate unique filename to prevent collisions
            var ext = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var absolutePath = Path.Combine(storageFolder, uniqueFileName);

            // Stream to disk
            await using var stream = new FileStream(absolutePath, FileMode.Create);
            await file.Content.CopyToAsync(stream, ct);

            // Store as a web-relative path  (served via static files)
            filePath = Path.Combine("Storage", "Uploads", "course-materials", courseId.ToString(), uniqueFileName)
                           .Replace('\\', '/');

            originalFileName = file.FileName;
            contentType = file.ContentType;
            fileSizeBytes = file.Length;
        }
        else // Link
        {
            if (string.IsNullOrWhiteSpace(request.ExternalUrl))
                throw new BusinessRuleException("ExternalUrl must be provided when MaterialType is 'Link'.");

            externalUrl = request.ExternalUrl.Trim();
        }

        var material = new CourseMaterial
        {
            CourseId = courseId,
            UploadedByUserId = requestingUserId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            MaterialType = materialType,
            FilePath = filePath,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            ExternalUrl = externalUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.CourseMaterials.Add(material);
        await _db.SaveChangesAsync(ct);

        await _activityLogger.LogAsync(
            requestingUserId,
            "CONTENT",
            "MaterialUploaded",
            $"Uploaded material '{material.Title}' for course: {course.Title}",
            entityId: material.Id.ToString(),
            entityType: "CourseMaterial",
            ct: ct);

        // Notify enrolled students
        var enrolledStudentIds = await _db.CourseEnrollments
            .Where(e => e.CourseId == courseId && e.Status == CourseEnrollmentStatus.Approved)
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        foreach (var studentId in enrolledStudentIds)
        {
            await _notificationService.TriggerNotificationAsync(
                studentId,
                "New Course Material",
                $"New material '{material.Title}' was added to '{course.Title}'.",
                $"/courses/{course.Id}?tab=materials",
                NotificationType.NewCourseMaterial,
                course.Id,
                sendEmail: false, // No email spam for materials
                ct);
        }

        return await BuildResponseAsync(material.Id, ct);
    }

    // ── Get Materials ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<MaterialResponse>> GetMaterialsAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        // Verify course exists
        var course = await _db.Courses.FindAsync([courseId], ct)
            ?? throw new NotFoundException("Course", courseId);

        // Students must be Approved-enrolled to see materials
        if (requestingUserRole == "Student")
        {
            var isEnrolled = await _db.CourseEnrollments.AnyAsync(
                e => e.CourseId == courseId
                  && e.StudentId == requestingUserId
                  && e.Status == CourseEnrollmentStatus.Approved,
                ct);

            if (!isEnrolled)
                throw new ForbiddenException("You must be enrolled in this course to view its materials.");
        }

        var materials = await _db.CourseMaterials
            .Include(m => m.UploadedByUser)
            .AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

        return materials.Select(MapToResponse).ToList();
    }

    // ── Download Material ─────────────────────────────────────────────────

    public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadMaterialAsync(
        Guid materialId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var material = await _db.CourseMaterials.FindAsync([materialId], ct)
            ?? throw new NotFoundException("Material", materialId);

        if (material.MaterialType != MaterialType.File || material.FilePath is null)
            throw new BusinessRuleException("This material is a link, not a downloadable file.");

        // Students must be Approved-enrolled
        if (requestingUserRole == "Student")
        {
            var isEnrolled = await _db.CourseEnrollments.AnyAsync(
                e => e.CourseId == material.CourseId
                  && e.StudentId == requestingUserId
                  && e.Status == CourseEnrollmentStatus.Approved,
                ct);

            if (!isEnrolled)
                throw new ForbiddenException("You must be enrolled in this course to download its materials.");
        }

        var absolutePath = Path.Combine(_env.WebRootPath, material.FilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolutePath))
            throw new NotFoundException("Material file", materialId);

        var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, material.ContentType ?? "application/octet-stream", material.OriginalFileName ?? "download");
    }

    // ── Delete Material ───────────────────────────────────────────────────

    public async Task DeleteMaterialAsync(
        Guid materialId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default)
    {
        var material = await _db.CourseMaterials
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == materialId, ct)
            ?? throw new NotFoundException("Material", materialId);

        // Authorization: Admin always allowed; Tutor only if assigned to the course
        if (requestingUserRole == "Tutor" && material.Course?.AssignedTutorId != requestingUserId)
            throw new ForbiddenException("You can only delete materials from courses assigned to you.");

        // Delete physical file if applicable
        if (material.MaterialType == MaterialType.File && material.FilePath is not null)
        {
            var absolutePath = Path.Combine(_env.WebRootPath, material.FilePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
        }

        material.IsDeleted = true;
        material.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _activityLogger.LogAsync(
            requestingUserId,
            "CONTENT",
            "MaterialDeleted",
            $"Deleted material '{material.Title}'",
            entityId: material.Id.ToString(),
            entityType: "CourseMaterial",
            ct: ct);
    }

    // ── Private Helpers ───────────────────────────────────────────────────

    private async Task<MaterialResponse> BuildResponseAsync(Guid materialId, CancellationToken ct)
    {
        var material = await _db.CourseMaterials
            .Include(m => m.UploadedByUser)
            .AsNoTracking()
            .FirstAsync(m => m.Id == materialId, ct);

        return MapToResponse(material);
    }

    private static MaterialResponse MapToResponse(CourseMaterial m)
    {
        var uploaderName = m.UploadedByUser is not null
            ? $"{m.UploadedByUser.FirstName} {m.UploadedByUser.LastName}"
            : "Unknown";

        return new MaterialResponse(
            m.Id,
            m.CourseId,
            m.Title,
            m.Description,
            m.MaterialType.ToString(),
            m.OriginalFileName,
            m.ContentType,
            m.FileSizeBytes,
            m.ExternalUrl,
            m.UploadedByUserId,
            uploaderName,
            m.CreatedAt
        );
    }
}
