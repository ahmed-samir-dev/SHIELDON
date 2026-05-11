namespace SHIELDON.Application.Features.Courses.DTOs;

// ── Material Responses ──────────────────────────────────────────────────────

/// <summary>
/// Response returned for a single course material in list or detail views.
/// Used by both Admin/Tutor management views and Student download views.
/// </summary>
public record MaterialResponse(
    Guid Id,
    Guid CourseId,
    string Title,
    string? Description,
    string MaterialType,          // "File" or "Link"
    // ── File fields (null when MaterialType = "Link") ──
    string? OriginalFileName,
    string? ContentType,
    long? FileSizeBytes,
    // ── Link field (null when MaterialType = "File") ──
    string? ExternalUrl,
    // ── Meta ──
    Guid UploadedByUserId,
    string UploadedByName,
    DateTime CreatedAt
);
