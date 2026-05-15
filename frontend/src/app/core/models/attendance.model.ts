export interface AttendanceCheckDto {
  id: string;
  courseId: string;
  courseName: string;
  tutorName: string;
  title: string;
  createdAt: string;
  isActive: boolean;
  totalPresent: number;
  totalEnrolled: number;
}

export interface AttendanceRecordDto {
  id: string;
  studentId: string;
  studentName: string;
  studentAvatarUrl?: string;
  scannedAt: string;
  isManual: boolean;
}

export interface EnrolledStudentDto {
  id: string;
  fullName: string;
  avatarUrl?: string;
  isPresent: boolean;
  isManual: boolean;
}

export interface AttendanceCheckDetailDto extends AttendanceCheckDto {
  records: AttendanceRecordDto[];
  allEnrolledStudents: EnrolledStudentDto[];
}

export interface StudentAttendanceHistoryDto {
  checkId: string;
  checkTitle: string;
  courseName: string;
  courseId: string;
  scannedAt: string;
  isManual: boolean;
}

export interface StartCheckRequest {
  courseId: string;
  title?: string;
}

export interface ScanRequest {
  secret: string;
}

// ── SignalR Events ──

export interface QrUpdatedDto {
  checkId: string;
  payload: string;
  expiresAt: string;
}

export interface AttendanceMarkedDto {
  checkId: string;
  studentId: string;
  studentName: string;
  studentAvatarUrl?: string;
  scannedAt: string;
  isManual: boolean;
}
