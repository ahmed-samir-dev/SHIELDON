using System;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Records major profile and security changes (Password Change, Email Change, Profile Update).
/// </summary>
public class UserActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Type of activity: 'EmailChange', 'PasswordChange', 'ProfileUpdate', 'PictureUpload'
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
