namespace SHIELDON.Domain.Enums;

/// <summary>
/// Represents the type of event captured in the IP audit log.
/// Used to classify what action triggered the network-level audit entry.
/// </summary>
public enum IpAuditEventType
{
    /// <summary>Successful user login.</summary>
    Login = 1,

    /// <summary>Student started an exam attempt.</summary>
    ExamStart = 2,

    /// <summary>Student submitted or the exam auto-submitted.</summary>
    ExamEnd = 3,

    /// <summary>An anti-cheat violation was reported during an attempt.</summary>
    ViolationReported = 4
}
