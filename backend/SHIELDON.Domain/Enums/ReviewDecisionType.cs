namespace SHIELDON.Domain.Enums;

/// <summary>
/// The outcome of a tutor/admin manual review of a suspicious exam session.
/// </summary>
public enum ReviewDecisionType
{
    /// <summary>Result stands as-is. No penalty applied.</summary>
    Accepted,

    /// <summary>Session is flagged as cheating. Score is zeroed out in the GradeRecord.</summary>
    MarkedAsCheating,

    /// <summary>An additional attempt is granted to the student.</summary>
    ReAttemptGranted
}
