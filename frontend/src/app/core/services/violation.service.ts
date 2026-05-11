import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export type ViolationType = 
  | 'AbnormalMouseActivity' 
  | 'ClipboardCopy' 
  | 'ClipboardPaste' 
  | 'RestrictedShortcut' 
  | 'WindowResize' 
  | 'WindowMinimize' 
  | 'SplitScreen' 
  | 'FullScreenExit' 
  | 'TabSwitch' 
  | 'FocusLoss';

export type ViolationSeverity = 'Minor' | 'Medium' | 'Critical';

export interface ViolationLogRequest {
  attemptId: string;
  type: ViolationType;
  severity: ViolationSeverity;
  description: string;
  occurredAt: string; // ISO string
  wasAutoSubmit: boolean;
}

export interface BatchViolationRequest {
  violations: ViolationLogRequest[];
}

export interface ViolationLogResponse {
  id: string;
  attemptId: string;
  studentId: string;
  studentName: string;
  studentDisplayId: string;
  examId: string;
  examTitle: string;
  type: string;
  severity: string;
  description: string;
  occurredAt: string;
  wasAutoSubmit: boolean;
  createdAt: string;
}

export interface AttemptViolationSummary {
  attemptId: string;
  studentId: string;
  studentName: string;
  studentDisplayId: string;
  totalViolations: number;
  criticalCount: number;
  mediumCount: number;
  minorCount: number;
  strikeScore: number;
  wasForceSubmitted: boolean;
  violations: ViolationLogResponse[];
}

@Injectable({
  providedIn: 'root'
})
export class ViolationService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/violations`;

  /**
   * Log a batch of violations detected by the client-side Anti-Cheat Engine.
   */
  logViolationBatch(request: BatchViolationRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}/batch`, request);
  }

  /**
   * Fetch all violations for a specific attempt (Tutor/Admin).
   */
  getViolationsForAttempt(attemptId: string): Observable<ApiResponse<AttemptViolationSummary>> {
    return this.http.get<ApiResponse<AttemptViolationSummary>>(`${environment.apiUrl}/attempts/${attemptId}/violations`);
  }

  /**
   * Fetch per-attempt violation summaries for an exam (Tutor/Admin).
   */
  getViolationSummaryForExam(examId: string): Observable<ApiResponse<AttemptViolationSummary[]>> {
    return this.http.get<ApiResponse<AttemptViolationSummary[]>>(`${environment.apiUrl}/exams/${examId}/violations`);
  }
}
