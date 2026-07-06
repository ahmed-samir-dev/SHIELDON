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
  notStartedCount: number;
  totalViolations: number;
  criticalViolations: number;
  averageScore: number | null;
}

export interface SubmissionRow {
  attemptId: string;
  studentName: string;
  studentCode: string;
  examTitle: string;
  status: string;
  submittedAt: string | null;
  score: number | null;
  violationCount: number;
  highestSeverity: string;
}

export interface TutorDashboardResponse {
  examSummaries: ExamMonitoringSummary[];
  recentSubmissions: SubmissionRow[];
  totalSubmissions: number;
  page: number;
  pageSize: number;
  violationTypeDistribution: ViolationTypeStat[];
}

// ─────────────────────────────────────────────────────────────────────────────
// ADMIN DASHBOARD
// ─────────────────────────────────────────────────────────────────────────────

export interface ExamStatisticsRow {
  examId: string;
  examTitle: string;
  courseTitle: string;
  tutorName: string;
  submittedCount: number;
  forceSubmittedCount: number;
  inProgressCount: number;
  totalViolations: number;
}

export interface AdminDashboardResponse {
  totalActiveCourses: number;
  totalCompletedExams: number;
  totalSubmissions: number;
  totalViolations: number;
  forceSubmissionRate: number;
  examStatistics: ExamStatisticsRow[];
  topViolationTypes: ViolationTypeStat[];
  activityTrend: DailyActivityPoint[];
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

  getAdminDashboard(): Observable<ApiResponse<AdminDashboardResponse>> {
    return this.http.get<ApiResponse<AdminDashboardResponse>>(`${this.baseUrl}/monitoring/admin/dashboard`);
  }
}
