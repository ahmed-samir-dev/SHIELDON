using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

public class ReattemptRequest
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ExamId { get; set; }
    public string Justification { get; set; } = string.Empty;
    
    /// <summary>Optional file proof uploaded by the student (screenshot, document, etc.).</summary>
    public string? AttachmentUrl { get; set; }
    
    /// <summary>
    /// True = student never entered the exam and is requesting re-open access.
    /// False = student had an attempt (failed/expired) and is requesting a re-attempt.
    /// </summary>
    public bool IsReopenRequest { get; set; } = false;
    
    /// <summary>Status of the request (e.g. Pending, Approved, Rejected).</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Optional reason provided when the request is rejected.</summary>
    public string? RejectionReason { get; set; }
    
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }

    public User? Student { get; set; }
    public Exam? Exam { get; set; }
    public User? ReviewedBy { get; set; }
}
