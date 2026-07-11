import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';

// ─────────────────────────────────────────────────────────────────────────────
// SHARED TYPES
// ─────────────────────────────────────────────────────────────────────────────

export interface ViolationTypeStat {
  violationType: string;
  count: number;
}

export interface DailyActivityPoint {
  date: string;
  examCount: number;
  violationCount: number;
}

// ─────────────────────────────────────────────────────────────────────────────
// ATTEMPT TIMELINE
// ─────────────────────────────────────────────────────────────────────────────

export interface AdminDashboardQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  tutorId?: string;
  sortColumn?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface TimelineEntry {
  category: string;
  eventType: string;
  severity: string;
  description: string;
  occurredAt: string;
  wasAutoSubmit: boolean;
}

export interface AttemptTimelineResponse {
  attemptId: string;
  studentName: string;
  studentCode: string;
  studentProfilePictureUrl: string | null;
  examTitle: string;
  courseTitle: string;
  startedAt: string;
  submittedAt: string | null;
  status: string;
  score: number | null;
  totalViolations: number;
  criticalCount: number;
  mediumCount: number;
  minorCount: number;
  events: TimelineEntry[];
}

// ─────────────────────────────────────────────────────────────────────────────
// VIOLATION SUMMARY (for charts)
// ─────────────────────────────────────────────────────────────────────────────

export interface ViolationChartPoint {
  minuteOffset: number;
  criticalCount: number;
  mediumCount: number;
  minorCount: number;
}

export interface ViolationTableRow {
  occurredAt: string;
  type: string;
  severity: string;
  description: string;
  wasAutoSubmit: boolean;
}

export interface ViolationSummaryResponse {
  totalViolations: number;
  criticalCount: number;
  mediumCount: number;
  minorCount: number;
  submissionType: string;
  chartData: ViolationChartPoint[];
  violations: ViolationTableRow[];
}

// ─────────────────────────────────────────────────────────────────────────────
// TUTOR DASHBOARD
// ─────────────────────────────────────────────────────────────────────────────

export interface ExamMonitoringSummary {
  examId: string;
  examTitle: string;
  courseTitle: string;
  totalEnrolled: number;
  inProgressCount: number;
  submittedCount: number;
  forceSubmittedCount: number;
  timeoutCount: number;
  violationLimitCount: number;
  notStartedCount: number;
  totalViolations: number;
  criticalViolations: number;
  averageScore: number | null;
  passedCount: number;
  failedCount: number;
}

export interface SubmissionRow {
  attemptId: string;
  examId: string;
  studentName: string;
  studentCode: string;
  examTitle: string;
  courseTitle: string;
  status: string;
  submittedAt: string | null;
  durationMinutes: number | null;
  score: number | null;
  passed: boolean;
  failed: boolean;
  violationCount: number;
  highestSeverity: string;
  history?: SubmissionRow[];
}

export interface TutorDashboardResponse {
  examSummaries: ExamMonitoringSummary[];
  recentSubmissions: SubmissionRow[];
  totalSubmissions: number;
  page: number;
  pageSize: number;
  violationTypeDistribution: ViolationTypeStat[];
  totalActiveCourses: number;
  totalStudents: number;
  activeExams: number;
  averagePassRate: number;
  completionRate: number;
  totalPassedStudents: number;
  averageTimeMinutes: number;
  courseViolationDetails: CourseViolationDetail[];
}

export interface CourseViolationDetail {
  courseTitle: string;
  violationType: string;
  severity: string;
  count: number;
}

// ─────────────────────────────────────────────────────────────────────────────
// ADMIN DASHBOARD
// ─────────────────────────────────────────────────────────────────────────────

export interface CourseViolationStat {
  courseTitle: string;
  violationCount: number;
  criticalCount: number;
  mediumCount: number;
  minorCount: number;
}

export interface SubmissionOutcomeStat {
  outcome: string;
  count: number;
  percentage: number;
}

export interface CourseSubmissionOutcome {
  courseTitle: string;
  outcome: string;
  count: number;
}

export interface RecentPaymentStat {
  paymentId: string;
  amountUSD: number;
  paidAt: string;
  studentName: string;
}

export interface ExamStatisticsRow {
  examId: string;
  examTitle: string;
  courseTitle: string;
  tutorName: string;
  scheduledAt: string | null;
  totalAttempts: number;
  submittedCount: number;
  forceSubmittedCount: number;
  inProgressCount: number;
  totalViolations: number;
  averageScore: number | null;
  passRate: number | null;
}

export interface AdminDashboardResponse {
  totalActiveCourses: number;
  totalExams: number;
  totalCompletedExams: number;
  totalSubmissions: number;
  totalViolations: number;
  totalStudents: number;
  totalTutors: number;
  activeExamsInProgress: number;
  averagePassRate: number;
  forceSubmissionRate: number;
  totalRevenueUSD: number;
  violationsByCourse: CourseViolationStat[];
  globalSubmissionOutcomes: SubmissionOutcomeStat[];
  recentPayments: RecentPaymentStat[];
  topViolationTypes: ViolationTypeStat[];
  activityTrend: DailyActivityPoint[];

  activeCourseTitles: string[];
  courseViolationDetails: CourseViolationDetail[];
  courseSubmissionOutcomes: CourseSubmissionOutcome[];

  // Exam Statistics Table
  examStatistics: ExamStatisticsRow[];
  examStatisticsTotalCount: number;
  examStatisticsPage: number;
  examStatisticsPageSize: number;
  examStatisticsTotalPages: number;
}

export interface PaymentTrendPoint {
  date: string;
  amountUSD: number;
}

export interface PlatformActivityResponse {
  activityTrend: DailyActivityPoint[];
}

export interface PaymentsTrendResponse {
  paymentsTrend: PaymentTrendPoint[];
}

@Injectable({
  providedIn: 'root'
})
export class MonitoringService {
  private http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  // ── Tutor / Admin ──────────────────────────────────────────────────────────

  getAttemptTimeline(attemptId: string): Observable<ApiResponse<AttemptTimelineResponse>> {
    return this.http.get<ApiResponse<AttemptTimelineResponse>>(`${this.baseUrl}/attempts/${attemptId}/timeline`);
  }

  getViolationSummary(attemptId: string): Observable<ApiResponse<ViolationSummaryResponse>> {
    return this.http.get<ApiResponse<ViolationSummaryResponse>>(`${this.baseUrl}/attempts/${attemptId}/violations/summary`);
  }

  getTutorDashboard(page = 1, pageSize = 10, search = '', status = 'All', examId?: string): Observable<ApiResponse<TutorDashboardResponse>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) params = params.set('search', search);
    if (status && status !== 'All') params = params.set('status', status);
    if (examId) params = params.set('examId', examId);

    return this.http.get<ApiResponse<TutorDashboardResponse>>(`${this.baseUrl}/monitoring/tutor/dashboard`, { params });
  }

  getAdminDashboard(page = 1, pageSize = 10, search?: string, tutorId?: string, sortColumn = 'ScheduledAt', sortDirection = 'desc'): Observable<ApiResponse<AdminDashboardResponse>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString())
      .set('sortColumn', sortColumn)
      .set('sortDirection', sortDirection);

    if (search) params = params.set('search', search);
    if (tutorId) params = params.set('tutorId', tutorId);

    return this.http.get<ApiResponse<AdminDashboardResponse>>(`${this.baseUrl}/monitoring/admin/dashboard`, { params });
  }

  getPlatformActivity(days?: number | null): Observable<ApiResponse<PlatformActivityResponse>> {
    let params = new HttpParams();
    if (days) params = params.set('days', days.toString());
    return this.http.get<ApiResponse<PlatformActivityResponse>>(`${this.baseUrl}/monitoring/admin/platform-activity`, { params });
  }

  getPaymentsTrend(days?: number | null): Observable<ApiResponse<PaymentsTrendResponse>> {
    let params = new HttpParams();
    if (days) params = params.set('days', days.toString());
    return this.http.get<ApiResponse<PaymentsTrendResponse>>(`${this.baseUrl}/monitoring/admin/payments-trend`, { params });
  }
}
