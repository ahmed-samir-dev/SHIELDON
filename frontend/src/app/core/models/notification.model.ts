export enum NotificationType {
  // Announcements
  AnnouncementCreated = 'AnnouncementCreated',
  AnnouncementUpdated = 'AnnouncementUpdated',
  NewCourseAnnouncement = 'NewCourseAnnouncement',
  ImportantCourseAnnouncement = 'ImportantCourseAnnouncement',

  // Enrollment
  EnrollmentApproved = 'EnrollmentApproved',
  EnrollmentRejected = 'EnrollmentRejected',

  // Materials
  MaterialUploaded = 'MaterialUploaded',
  NewCourseMaterial = 'NewCourseMaterial',

  // Assignments
  NewCourseAssignment = 'NewCourseAssignment',

  // Exams
  ExamScheduled = 'ExamScheduled',
  UpcomingExamReminder = 'UpcomingExamReminder',
  ExamResultReleased = 'ExamResultReleased',
  ExamCreated = 'ExamCreated',
  ExamUpdated = 'ExamUpdated',
  ExamReminder24h = 'ExamReminder24h',
  ExamReminder1h = 'ExamReminder1h',

  // Results
  ResultReleased = 'ResultReleased',

  // System
  CourseUpdate = 'CourseUpdate',
  GeneralSystem = 'GeneralSystem'
}

export interface NotificationResponse {
  id: string;
  title: string;
  message: string;
  actionUrl: string | null;
  type: NotificationType;
  isRead: boolean;
  createdAt: string;
  relatedEntityId: string | null;
}
