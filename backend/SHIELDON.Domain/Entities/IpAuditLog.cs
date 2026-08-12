using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Records the IP address and network context for every significant security event:
/// logins, exam starts, exam ends, and violation reports.
///
/// Serves two purposes:
///   1. Forensic audit trail - visible on the Attempt Detail page and Admin Audit Trail.
///   2. Active security enforcement - detects VPN/proxy usage and duplicate sessions.
///
/// All timestamps are stored in UTC.
/// IpAddress supports both IPv4 (max 15 chars) and IPv6 (max 45 chars).
/// </summary>
public class IpAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Who ────────────────────────────────────────────────────────────────
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // ── What ───────────────────────────────────────────────────────────────
    /// <summary>The type of event that triggered this log entry.</summary>
    public IpAuditEventType EventType { get; set; }

    // ── Context ────────────────────────────────────────────────────────────
    /// <summary>
    /// The IP address of the client. Supports IPv4 and full IPv6 notation.
    /// Max 45 chars: "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff" = 39 chars; with zone IDs up to 45.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>Browser User-Agent string for device fingerprinting context.</summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// If the exam attempt context is available (ExamStart, ExamEnd, ViolationReported),
    /// this links the log entry to the specific attempt for the Attempt Detail page.
    /// Null for Login events.
    /// </summary>
    public Guid? ExamAttemptId { get; set; }
    public ExamAttempt? ExamAttempt { get; set; }

    // ── Security Flags ─────────────────────────────────────────────────────
    /// <summary>
    /// True if the IP address matched a known datacenter/VPN CIDR range
    /// from the SecuritySettings:KnownVpnCidrRanges configuration.
    /// Advisory flag - does not automatically penalise the user.
    /// </summary>
    public bool IsVpnOrProxy { get; set; } = false;

    /// <summary>
    /// True if this login occurred within 30 seconds of a prior active login for the same UserId
    /// from a different IP address (Wall 2 duplicate session race condition detection).
    /// Advisory flag for Admin review.
    /// </summary>
    public bool IsDuplicateSession { get; set; } = false;

    /// <summary>
    /// True if the user's IP address changed between ExamStart and this event
    /// during an active exam session. Passive observation - no automatic strike.
    /// </summary>
    public bool IsNetworkChangeDuringExam { get; set; } = false;

    // ── Timestamp ──────────────────────────────────────────────────────────
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
