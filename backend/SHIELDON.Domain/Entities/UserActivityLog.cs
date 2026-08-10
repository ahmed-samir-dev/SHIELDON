using System;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Records major system security, business, and user activity events.
/// </summary>
public class UserActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// Category of activity: 'AUTH', 'COURSE', 'EXAM', 'SECURITY', 'CONTENT', 'GRADE', 'SYSTEM'
    /// </summary>
    public string Category { get; set; } = "SYSTEM";

    /// <summary>
    /// Specific action name (e.g. 'UserLogin', 'CourseCreated', 'ExamAttemptStarted', 'ViolationLogged')
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Legacy event type string retained for backward compatibility.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    
    public string Description { get; set; } = string.Empty;

    public string? EntityId { get; set; }
    public string? EntityType { get; set; }

    public string? MetadataJson { get; set; }
    
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
