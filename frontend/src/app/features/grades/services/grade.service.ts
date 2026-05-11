import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse, PagedResponse } from '../../../core/models/api-response.model';

// ── Request Interfaces ─────────────────────────────────────────────────────

export interface GradeQueryParams {
  page?: number;
  pageSize?: number;
  type?: 'Exam' | 'Assignment' | null;
  status?: 'Published' | 'Unpublished' | null;
  searchTerm?: string | null;
}

export interface UpdateGradeRequest {
  weight?: number | null;
  score?: number | null;
  notes?: string | null;
}

export interface BulkPublishRequest {
  gradeIds?: string[] | null;
  publishAll?: boolean;
}

// ── Response Interfaces ────────────────────────────────────────────────────

export interface GradeItemResponse {
  id: string;
  studentId: string;
  studentName: string;
  studentDisplayId: string | null;
  studentEmail: string;
  courseId: string;
  examId: string | null;
  examTitle: string | null;
  assignmentId: string | null;
  assignmentTitle: string | null;
  type: string;
  score: number;
  maxScore: number;
  weight: number;
  weightedScore: number;
  isPublished: boolean;
  publishedAt: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CourseGradeSummaryResponse {
  studentId: string;
  studentName: string;
  studentDisplayId: string | null;
  studentEmail: string;
  grades: GradeItemResponse[];
  totalWeightAssigned: number;
  finalWeightedScore: number | null;
}

export interface MyGradeItemResponse {
  id: string;
  courseId: string;
  courseTitle: string;
  examId: string | null;
  examTitle: string | null;
  assignmentId: string | null;
  assignmentTitle: string | null;
  type: string;
  score: number;
  maxScore: number;
  weight: number;
  weightedScore: number;
  publishedAt: string | null;
}

// ── Service ────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class GradeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  // ── Tutor/Admin Endpoints ──────────────────────────────────────────────

  getCourseGrades(courseId: string, query?: GradeQueryParams): Observable<ApiResponse<PagedResponse<CourseGradeSummaryResponse>>> {
    let params = new HttpParams();
    if (query) {
      if (query.page) params = params.set('page', query.page);
      if (query.pageSize) params = params.set('pageSize', query.pageSize);
      if (query.type) params = params.set('type', query.type);
      if (query.status) params = params.set('status', query.status);
      if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);
    }

    return this.http.get<ApiResponse<PagedResponse<CourseGradeSummaryResponse>>>(
      `${this.apiUrl}/courses/${courseId}/grades`,
      { params }
    );
  }

  updateGrade(gradeId: string, request: UpdateGradeRequest): Observable<ApiResponse<GradeItemResponse>> {
    return this.http.patch<ApiResponse<GradeItemResponse>>(
      `${this.apiUrl}/grades/${gradeId}`,
      request
    );
  }

  publishGrades(courseId: string, request: BulkPublishRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.apiUrl}/courses/${courseId}/grades/publish`,
      request
    );
  }

  exportGradesCsv(courseId: string): void {
    const url = `${this.apiUrl}/courses/${courseId}/grades/export`;
    this.http.get(url, { responseType: 'blob' }).subscribe(blob => {
      const link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      const today = new Date().toISOString().split('T')[0];
      link.download = `Grades_${courseId}_${today}.csv`;
      link.click();
      URL.revokeObjectURL(link.href);
    });
  }

  // ── Student Endpoints ──────────────────────────────────────────────────

  getMyGradesForCourse(courseId: string): Observable<ApiResponse<MyGradeItemResponse[]>> {
    return this.http.get<ApiResponse<MyGradeItemResponse[]>>(
      `${this.apiUrl}/courses/${courseId}/grades/my`
    );
  }

  getMyGrades(): Observable<ApiResponse<MyGradeItemResponse[]>> {
    return this.http.get<ApiResponse<MyGradeItemResponse[]>>(
      `${this.apiUrl}/my-grades`
    );
  }
}
