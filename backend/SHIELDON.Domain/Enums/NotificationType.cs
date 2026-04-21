namespace SHIELDON.Domain.Enums;

/// <summary>
/// Categorizes the event that triggered a notification.
/// Used to drive routing and icon selection on the frontend.
/// </summary>
public enum NotificationType
{
    EnrollmentApproved = 10,
    EnrollmentRejected = 11,
    
    NewCourseAnnouncement = 20,
    ImportantCourseAnnouncement = 21,
    
    NewCourseMaterial = 30,
    
    NewCourseAssignment = 40,
    
    ExamScheduled = 50,
    UpcomingExamReminder = 51,
    ExamResultReleased = 52,
    
    CourseUpdate = 90,
    GeneralSystem = 100
}
