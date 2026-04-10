using System;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Records every login attempt (success or failure) for security auditing.
/// </summary>
public class LoginActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? IpAddress { get; set; }
    public bool IsSuccess { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
