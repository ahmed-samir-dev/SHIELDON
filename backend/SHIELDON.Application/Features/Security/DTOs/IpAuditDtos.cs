using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Security.DTOs;

/// <summary>
/// Data transfer object representing a single IP audit log entry.
/// Returned by IIpAuditService for display in the Attempt Detail page and Admin Audit Trail.
/// IP data is never returned to students — all endpoints are Admin/Tutor only.
/// </summary>
public class IpAuditLogDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string? UserDisplayId { get; set; }  // StudentId, TutorId, or AdminId
    public IpAuditEventType EventType { get; set; }
    public string EventTypeLabel { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? ExamAttemptId { get; set; }
    public bool IsVpnOrProxy { get; set; }
    public bool IsDuplicateSession { get; set; }
    public bool IsNetworkChangeDuringExam { get; set; }
    public DateTime OccurredAt { get; set; }
}

/// <summary>
/// Query parameters for the paginated Admin Audit Trail endpoint.
/// All filters are optional — omitting them returns all logs.
/// </summary>
public class AuditTrailQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Filter by a specific user ID.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Filter by event type (e.g. "Login", "ExamStart").</summary>
    public IpAuditEventType? EventType { get; set; }

    /// <summary>Filter to only VPN/proxy flagged entries.</summary>
    public bool? IsVpnOrProxy { get; set; }

    /// <summary>Filter to only duplicate session flagged entries.</summary>
    public bool? IsDuplicateSession { get; set; }

    /// <summary>Filter to only network-change-during-exam entries.</summary>
    public bool? IsNetworkChangeDuringExam { get; set; }

    /// <summary>Filter entries from this date (UTC, inclusive).</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Filter entries up to this date (UTC, inclusive).</summary>
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Paginated response for the Admin Audit Trail endpoint.
/// </summary>
public class AuditTrailPagedResult
{
    public List<IpAuditLogDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
