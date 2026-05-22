using SHIELDON.Domain.Common;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a custom event created by an Admin or Tutor.
/// Nullable CourseId means it's a global system event.
/// </summary>
public class CustomEvent : ITranslatable
{
    public Guid Id { get; set; }
    
    [Translatable]
    public string Title { get; set; } = string.Empty;
    
    [Translatable]
    public string? Description { get; set; }

    public string? Translations { get; set; }
    
    public DateTime EventDate { get; set; }
    public DateTime? EventEndDate { get; set; }
    
    // ── Nullable CourseId for Global events ───────────────────────
    public Guid? CourseId { get; set; }
    public Course? Course { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
