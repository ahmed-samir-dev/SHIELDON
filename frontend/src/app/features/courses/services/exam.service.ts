import { Injectable } from '@angular/core';
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

  constructor(private http: HttpClient) {}

  // ── Course-scoped operations ────────────────────────────────────────────────

  getExams(courseId: string, page: number = 1, pageSize: number = 10, search?: string, status?: string): Observable<ApiResponse<PagedResponse<ExamSummaryResponse>>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);

    return this.http.get<ApiResponse<PagedResponse<ExamSummaryResponse>>>(`${this.baseUrl}/courses/${courseId}/exams`, { params });
  }

  createExam(courseId: string, request: CreateExamRequest): Observable<ApiResponse<ExamSummaryResponse>> {
    return this.http.post<ApiResponse<ExamSummaryResponse>>(`${this.baseUrl}/courses/${courseId}/exams`, request);
  }

  // ── Exam-scoped operations ──────────────────────────────────────────────────

  getExamById(examId: string): Observable<ApiResponse<ExamDetailResponse>> {
    return this.http.get<ApiResponse<ExamDetailResponse>>(`${this.baseUrl}/exams/${examId}`);
  }

  updateExam(examId: string, request: UpdateExamRequest): Observable<ApiResponse<ExamDetailResponse>> {
    return this.http.patch<ApiResponse<ExamDetailResponse>>(`${this.baseUrl}/exams/${examId}`, request);
  }

  deleteExam(examId: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.baseUrl}/exams/${examId}`);
  }

  publishExam(examId: string): Observable<ApiResponse<ExamDetailResponse>> {
    return this.http.patch<ApiResponse<ExamDetailResponse>>(`${this.baseUrl}/exams/${examId}/publish`, {});
  }
}
