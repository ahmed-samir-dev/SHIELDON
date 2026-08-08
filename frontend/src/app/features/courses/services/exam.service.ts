import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse, PagedResponse } from '../../../core/models/api-response.model';
import { CreateExamRequest, ExamDetailResponse, ExamSummaryResponse, UpdateExamRequest } from '../../../core/models/exam.model';

@Injectable({
  providedIn: 'root'
})
export class ExamService {
  private readonly baseUrl = environment.apiUrl;
  private http = inject(HttpClient);

  // ── Course-scoped operations ────────────────────────────────────────────────

  getExams(courseId: string, page: number = 1, pageSize: number = 10, search?: string, status?: string): Observable<ApiResponse<PagedResponse<ExamSummaryResponse>>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);

    return this.http.get<ApiResponse<PagedResponse<ExamSummaryResponse>>>(
      `${this.baseUrl}/courses/${courseId}/exams`,
      { params }
    );
  }

  getExamById(idOrCourseId: string, id?: string): Observable<ApiResponse<ExamDetailResponse>> {
    const examId = id || idOrCourseId;
    return this.http.get<ApiResponse<ExamDetailResponse>>(
      `${this.baseUrl}/exams/${examId}`
    );
  }

  createExam(courseId: string, request: CreateExamRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/courses/${courseId}/exams`,
      request
    );
  }

  updateExam(idOrCourseId: string, idOrRequest: string | UpdateExamRequest, req?: UpdateExamRequest): Observable<ApiResponse<void>> {
    let examId: string;
    let request: UpdateExamRequest;

    if (typeof idOrRequest === 'string') {
      examId = idOrRequest;
      request = req!;
    } else {
      examId = idOrCourseId;
      request = idOrRequest;
    }

    return this.http.patch<ApiResponse<void>>(
      `${this.baseUrl}/exams/${examId}`,
      request
    );
  }

  deleteExam(idOrCourseId: string, id?: string): Observable<ApiResponse<void>> {
    const examId = id || idOrCourseId;
    return this.http.delete<ApiResponse<void>>(
      `${this.baseUrl}/exams/${examId}`
    );
  }

  publishExam(idOrCourseId: string, id?: string): Observable<ApiResponse<void>> {
    const examId = id || idOrCourseId;
    return this.http.patch<ApiResponse<void>>(
      `${this.baseUrl}/exams/${examId}/publish`,
      {}
    );
  }
}
