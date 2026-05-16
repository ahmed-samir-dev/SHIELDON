export interface CreateCourseRequest {
  title: string;
  courseCode: string;
  description: string | null;
  assignedTutorId: string | null;
  courseFee: number;
}

export interface UpdateCourseRequest {
  title: string;
  description: string | null;
  assignedTutorId: string | null;
  courseFee: number;
  isActive: boolean;
}

export interface EnrollmentRequest {
  courseId: string;
}

export interface ReviewEnrollmentRequest {
  approved: boolean;
  rejectionReason: string | null;
}

export interface BulkReviewEnrollmentRequest {
  enrollmentIds: string[];
  approved: boolean;
  rejectionReason: string | null;
}

export interface CourseQueryParams {
  page?: number;
  pageSize?: number;
  search?: string | null;
  isActive?: boolean | null;
  enrollmentStatus?: string | null;
}

export interface CourseResponse {
  id: string;
  title: string;
  courseCode: string;
  description: string | null;
  assignedTutorId: string | null;
  assignedTutorName: string | null;
  isActive: boolean;
  courseFee: number;
  enrolledStudentCount: number;
  createdAt: string;
}

export interface CourseDetailResponse {
  id: string;
  title: string;
  courseCode: string;
  description: string | null;
  assignedTutorId: string | null;
  assignedTutorName: string | null;
  isActive: boolean;
  courseFee: number;
  enrolledStudentCount: number;
  materialCount: number;
  announcementCount: number;
  assignmentCount: number;
  examCount: number;
  publishedExamCount: number;
  createdAt: string;
}

export interface EnrollmentResponse {
  id: string;
  courseId: string;
  courseTitle: string;
  courseCode: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  studentDisplayId: string | null;
  status: string;
  rejectionCount: number;
  cooldownUntil: string | null;
  rejectionReason: string | null;
  requestedAt: string;
  reviewedAt: string | null;
  reviewedByName: string | null;
}

export interface StudentEnrollmentStatusResponse {
  courseId: string;
  courseTitle: string;
  status: string;
  rejectionCount: number;
  cooldownUntil: string | null;
  rejectionReason: string | null;
  requestedAt: string;
}

export interface UserBasicResponse {
  id: string;
  fullName: string;
  email: string;
}
