namespace SHIELDON.Domain.Enums;

/// <summary>
/// The status of an exam session for a student.
/// </summary>
public enum ExamSessionStatus
{
    Active,
    Submitted,
    ForceSubmitted,
    Expired,
    AutoExpired
}

/// <summary>
/// How the exam session was submitted.
/// </summary>
public enum SubmissionType
{
    Manual,
    AutoExpired,
    ForceSubmitted
}
