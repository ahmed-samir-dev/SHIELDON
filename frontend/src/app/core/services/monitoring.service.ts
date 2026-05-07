import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';

// DTO Interfaces
export interface TimelineEventResponse {
  occurredAt: string;
  category: string;
  eventType: string;
  severity: string;
  description: string;
  wasAutoSubmit: boolean;
}

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

export interface ActiveExamSummary {
  examId: string;
  examTitle: string;
  courseTitle: string;
  inProgressCount: number;
  submittedCount: number;
  forceSubmittedCount: number;
  notStartedCount: number;
}

export interface LiveSessionRow {
  attemptId: string;
  studentId: string;
  studentName: string;
  studentCode: string;
  examTitle: string;
  status: string;
  violationCount: number;
  startedAt: string;
  lastHeartbeatAt: string | null;
  hasReviewDecision: boolean;
}

export interface ViolationTypeStat {
  violationType: string;
  count: number;
}

export interface ViolationTypeDistribution {
  items: ViolationTypeStat[];
}

export interface TutorDashboardResponse {
  activeExams: ActiveExamSummary[];
  liveSessions: LiveSessionRow[];
  violationDistribution: ViolationTypeDistribution;
}

export interface GlobalExamRow {
  examId: string;
  examTitle: string;
  courseTitle: string;
  tutorName: string;
  studentsInProgress: number;
  totalViolations: number;
}

export interface DailyActivityPoint {
  date: string;
  examCount: number;
  violationCount: number;
}

export interface AdminDashboardResponse {
  totalActiveCourses: number;
  totalOngoingExams: number;
  totalEnrolledStudents: number;
  totalViolationsToday: number;
  totalForceSubmittedToday: number;
  activeExamSessions: GlobalExamRow[];
  topViolationTypes: ViolationTypeStat[];
  activityTrend: DailyActivityPoint[];
  suspiciousSubmissionRatePercent: number;
}

export interface ReviewDecisionRequest {
  decision: string; // 'Accepted' | 'MarkedAsCheating' | 'ReAttemptGranted'
  notes?: string;
}

export interface ReviewDecisionResponse {
  decisionId: string;
  decision: string;
  notes?: string;
  reviewedAt: string;
}

export interface TerminateSessionRequest {
  reason?: string;
}

@Injectable({
  providedIn: 'root'
})
export class MonitoringService {
  private http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  // ── Student ────────────────────────────────────────────────────────────────

  logHeartbeat(attemptId: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/api/attempts/${attemptId}/heartbeat`, {});
  }

  // ── Tutor / Admin ──────────────────────────────────────────────────────────

  getTimeline(attemptId: string): Observable<ApiResponse<TimelineEventResponse[]>> {
    return this.http.get<ApiResponse<TimelineEventResponse[]>>(`${this.baseUrl}/api/attempts/${attemptId}/timeline`);
  }

  getViolationSummary(attemptId: string): Observable<ApiResponse<ViolationSummaryResponse>> {
    return this.http.get<ApiResponse<ViolationSummaryResponse>>(`${this.baseUrl}/api/attempts/${attemptId}/violations/summary`);
  }

  submitReviewDecision(attemptId: string, request: ReviewDecisionRequest): Observable<ApiResponse<ReviewDecisionResponse>> {
    return this.http.post<ApiResponse<ReviewDecisionResponse>>(`${this.baseUrl}/api/attempts/${attemptId}/review`, request);
  }

  terminateSession(attemptId: string, request: TerminateSessionRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/api/attempts/${attemptId}/terminate`, request);
  }

  getTutorDashboard(): Observable<ApiResponse<TutorDashboardResponse>> {
    return this.http.get<ApiResponse<TutorDashboardResponse>>(`${this.baseUrl}/api/monitoring/tutor/dashboard`);
  }

  getAdminDashboard(): Observable<ApiResponse<AdminDashboardResponse>> {
    return this.http.get<ApiResponse<AdminDashboardResponse>>(`${this.baseUrl}/api/monitoring/admin/dashboard`);
  }
}
