namespace SHIELDON.Domain.Enums;

/// <summary>
/// Represents the status of a student's enrollment request for a course.
/// </summary>
public enum CourseEnrollmentStatus
{
    /// <summary>Student submitted request; awaiting Admin/Tutor review.</summary>
    Pending = 0,

    /// <summary>Request approved - student has full course access.</summary>
    Approved = 1,

    /// <summary>Request rejected by Admin/Tutor.</summary>
    Rejected = 2,

    /// <summary>Student voluntarily dropped the course after enrollment.</summary>
    Dropped = 3,

    /// <summary>
    /// Student was removed by an Admin or assigned Tutor.
    /// Does NOT count as a rejection and carries no cooldown.
    /// The student is free to submit a new enrollment request immediately.
    /// </summary>
    KickedOut = 4
}
