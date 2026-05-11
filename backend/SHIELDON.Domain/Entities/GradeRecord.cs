using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

public class GradeRecord
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid? ExamId { get; set; }
    public Guid? AssignmentId { get; set; }
    
    public GradeType Type { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    
    /// <summary>Weight percentage (0.0 - 100.0) set by tutor.</summary>
    public decimal Weight { get; set; }
    
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    
    /// <summary>Tutor override notes.</summary>
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? Student { get; set; }
    public Course? Course { get; set; }
    public Exam? Exam { get; set; }
    public Assignment? Assignment { get; set; }
}
