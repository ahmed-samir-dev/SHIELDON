import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';

// ── Response Interfaces ────────────────────────────────────────────────────

export interface AssignmentSubmissionResponse {
  id: string;
  assignmentId: string;
  studentId: string;
  studentName: string;
  studentDisplayId: string | null;
  originalFileName: string;
  fileExtension: string;
  fileSizeBytes: number;
  submittedAt: string;
  pointsAwarded?: number | null;
  feedback?: string | null;
  reviewedAt?: string | null;
  reviewedByName?: string | null;
  isReviewed?: boolean;
}

export interface AssignmentResponse {
  id: string;
  courseId: string;
  title: string;
  instructions: string | null;
  createdByName: string;
  hasReferenceFile: boolean;
  referenceFileName: string | null;
  referenceFileExtension: string | null;
  referenceFileSizeBytes?: number;
  dueDate?: string;
  weight: number;
  maxPoints: number;
  isPastDue: boolean;
  submissionCount: number;
  mySubmission: AssignmentSubmissionResponse | null;
  createdAt: string;
}

// ── Request Interfaces ─────────────────────────────────────────────────────

export interface CreateAssignmentRequest {
  title: string;
  instructions: string | null;
  dueDate: string | null;
}

export interface UpdateAssignmentRequest {
  title: string;
  instructions: string | null;
  dueDate: string | null;
}

export interface ReviewSubmissionRequest {
  pointsAwarded: number;
  feedback?: string | null;
}

// ── Service ────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class AssignmentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  // ── Assignments ────────────────────────────────────────────────────────

  getAssignments(courseId: string): Observable<ApiResponse<AssignmentResponse[]>> {
    return this.http.get<ApiResponse<AssignmentResponse[]>>(
      `${this.apiUrl}/courses/${courseId}/assignments`
    );
  }

  createAssignment(courseId: string, formData: FormData): Observable<ApiResponse<AssignmentResponse>> {
    return this.http.post<ApiResponse<AssignmentResponse>>(
      `${this.apiUrl}/courses/${courseId}/assignments`, formData
    );
  }

  updateAssignment(courseId: string, assignmentId: string, request: UpdateAssignmentRequest): Observable<ApiResponse<AssignmentResponse>> {
    return this.http.patch<ApiResponse<AssignmentResponse>>(
      `${this.apiUrl}/courses/${courseId}/assignments/${assignmentId}`, request
    );
  }

  deleteAssignment(courseId: string, assignmentId: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(
      `${this.apiUrl}/courses/${courseId}/assignments/${assignmentId}`
    );
  }

  downloadReferenceFile(courseId: string, assignmentId: string, fileName: string): void {
    const url = `${this.apiUrl}/courses/${courseId}/assignments/${assignmentId}/reference`;
    this.http.get(url, { responseType: 'blob' }).subscribe(blob => {
      const link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = fileName;
      link.click();
      URL.revokeObjectURL(link.href);
    });
  }

  // ── Submissions ────────────────────────────────────────────────────────

  submitAssignment(courseId: string, assignmentId: string, formData: FormData): Observable<ApiResponse<AssignmentSubmissionResponse>> {
    return this.http.post<ApiResponse<AssignmentSubmissionResponse>>(
      `${this.apiUrl}/courses/${courseId}/assignments/${assignmentId}/submissions`, formData
    );
  }

  getSubmissions(courseId: string, assignmentId: string): Observable<ApiResponse<AssignmentSubmissionResponse[]>> {
    return this.http.get<ApiResponse<AssignmentSubmissionResponse[]>>(
      `${this.apiUrl}/courses/${courseId}/assignments/${assignmentId}/submissions`
    );
  }

  deleteSubmission(courseId: string, assignmentId: string, submissionId: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(
      `${this.apiUrl}/courses/${courseId}/assignments/${assignmentId}/submissions/${submissionId}`
    );
  }

  downloadSubmission(courseId: string, assignmentId: string, submissionId: string, fileName: string): void {
    const url = `${this.apiUrl}/courses/${courseId}/assignments/${assignmentId}/submissions/${submissionId}/download`;
    this.http.get(url, { responseType: 'blob' }).subscribe(blob => {
      const link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = fileName;
      link.click();
      URL.revokeObjectURL(link.href);
    });
  }

  downloadAllSubmissionsAsZip(courseId: string, assignmentId: string, zipName: string): void {
    const url = `${this.apiUrl}/courses/${courseId}/assignments/${assignmentId}/submissions/download-all`;
    this.http.get(url, { responseType: 'blob', observe: 'response' }).subscribe(response => {
      if (response.status === 204 || !response.body) return;
      const link = document.createElement('a');
      link.href = URL.createObjectURL(response.body);
      link.download = zipName;
      link.click();
      URL.revokeObjectURL(link.href);
    });
  }

  reviewSubmission(courseId: string, assignmentId: string, submissionId: string, request: ReviewSubmissionRequest): Observable<ApiResponse<AssignmentSubmissionResponse>> {
    return this.http.post<ApiResponse<AssignmentSubmissionResponse>>(
      `${this.apiUrl}/courses/${courseId}/assignments/${assignmentId}/submissions/${submissionId}/review`,
      request
    );
  }
}
