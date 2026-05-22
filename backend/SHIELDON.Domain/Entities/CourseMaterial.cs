using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Common;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a file or external link shared by a Tutor within a course.
/// Only enrolled students can access materials.
/// </summary>
public class CourseMaterial : ITranslatable
{
    public Guid Id { get; set; }

    // ── Relationship Keys ────────────────────────────────────────
    public Guid CourseId { get; set; }
    public Guid UploadedByUserId { get; set; }

    // ── Content ──────────────────────────────────────────────────
    /// <summary>Display name shown to students (e.g., "Week 3 Lecture Slides").</summary>
    [Translatable]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description of the material's purpose.</summary>
    [Translatable]
    public string? Description { get; set; }

    public string? Translations { get; set; }

    /// <summary>Whether this material is an uploaded file or an external URL.</summary>
    public MaterialType MaterialType { get; set; }

    // ── File Info (when MaterialType = File) ─────────────────────
    /// <summary>
    /// Server-side relative path to the stored file.
    /// Format: Storage/Uploads/course-materials/{courseId}/{uniqueFileName}
    /// Null when MaterialType = Link.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>Original filename as uploaded by the Tutor (for display purposes).</summary>
    public string? OriginalFileName { get; set; }

    /// <summary>MIME type of the file (e.g., "application/pdf"). Used for secure serving.</summary>
    public string? ContentType { get; set; }

    /// <summary>File size in bytes. Used for display and quota tracking.</summary>
    public long? FileSizeBytes { get; set; }

    // ── Link Info (when MaterialType = Link) ─────────────────────
    /// <summary>External URL (Google Drive, YouTube, etc.). Null when MaterialType = File.</summary>
    public string? ExternalUrl { get; set; }

    // ── Timestamps ──────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ────────────────────────────────────
    public Course? Course { get; set; }
    public User? UploadedByUser { get; set; }
}
