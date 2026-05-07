namespace SHIELDON.Domain.Enums;

/// <summary>
/// Represents all significant events that can occur during an exam session,
/// used to build a full presence timeline for tutor/admin review in Phase 5.
/// </summary>
public enum PresenceEventType
{
    /// <summary>Student successfully started the exam session.</summary>
    ExamStarted,

    /// <summary>Student reloaded the exam page (session was resumed from an existing attempt).</summary>
    PageRefreshed,

    /// <summary>Heartbeat received from the student's browser — session is live.</summary>
    HeartbeatReceived,

    /// <summary>No heartbeat received for 45+ seconds — student appears disconnected.</summary>
    Disconnected,

    /// <summary>Student reconnected after a disconnection.</summary>
    Reconnected,

    /// <summary>Student voluntarily submitted the exam.</summary>
    ExamSubmitted,

    /// <summary>Exam was force-submitted by the Anti-Cheating Engine (3-violation threshold reached).</summary>
    ForceSubmitted,

    /// <summary>Exam was auto-submitted by the server because the time limit expired.</summary>
    AutoExpired,

    /// <summary>Session ended unexpectedly (browser closed, crash) detected via missing heartbeat + no submission.</summary>
    UnexpectedExit,

    /// <summary>A Tutor or Admin manually terminated the student's active exam session.</summary>
    TutorTerminated
}
