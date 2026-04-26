using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

public class ReattemptRequest
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ExamId { get; set; }
    public string Justification { get; set; } = string.Empty;
    
    /// <summary>Status of the request (e.g. Pending, Approved, Rejected).</summary>
    public string Status { get; set; } = "Pending";
    
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }

    public User? Student { get; set; }
    public Exam? Exam { get; set; }
    public User? ReviewedBy { get; set; }
}
