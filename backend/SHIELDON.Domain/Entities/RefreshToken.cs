namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a JWT refresh token issued to a user.
/// Supports token rotation: each refresh issues a new token and revokes the old one.
/// Expired and revoked tokens can be purged by a background job.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    // ── Token Data ─────────────────────────────────────────────
    /// <summary>The raw refresh token string. Stored as SHA-256 hash in production.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>UTC datetime when this token expires.</summary>
    public DateTime ExpiresAt { get; set; }

    // ── Lifecycle ──────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC datetime when this token was revoked. Null if still valid.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Optional reason for revocation (e.g. "Replaced", "Logout", "Compromised").</summary>
    public string? RevokedReason { get; set; }

    // ── Relationship ───────────────────────────────────────────
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // ── Computed ───────────────────────────────────────────────
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked;
}
