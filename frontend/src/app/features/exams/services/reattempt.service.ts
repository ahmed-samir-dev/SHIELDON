import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface SubmitReattemptRequest {
  justification: string;
}

export interface ReviewReattemptRequest {
  approved: boolean;
  rejectionReason?: string;
}

export interface ReattemptRequestResponse {
  id: string;
  examId: string;
  examTitle: string;
  courseId: string;
  courseTitle: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  studentDisplayId?: string;
  justification: string;
  status: string;
  attemptsMade: number;
  maxAttempts: number;
  requestedAt: string;
  reviewedAt?: string;
  reviewedByName?: string;
  rejectionReason?: string;
}

export interface StudentReattemptStatusResponse {
  id: string;
  examId: string;
  examTitle: string;
  justification: string;
  status: string;
  attemptsMade: number;
  maxAttempts: number;
  requestedAt: string;
  reviewedAt?: string;
  rejectionReason?: string;
}

export interface ReattemptQueryParams {
  page?: number;
  pageSize?: number;
  status?: string | null;
  examId?: string | null;
  courseId?: string | null;
  searchTerm?: string | null;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: Record<string, string[]>;
}

@Injectable({
  providedIn: 'root'
})
export class ReattemptService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  /**
   * Student submits a re-attempt request for an exam they failed.
   */
  submitRequest(examId: string, request: SubmitReattemptRequest): Observable<ApiResponse<StudentReattemptStatusResponse>> {
    return this.http.post<ApiResponse<StudentReattemptStatusResponse>>(`${this.apiUrl}/reattempt-requests?examId=${examId}`, request);
  }

  /**
   * Returns a paginated list of re-attempt requests.
   * Admin: all requests. Tutor: their courses.
   */
  getRequests(params: ReattemptQueryParams): Observable<ApiResponse<PagedResponse<ReattemptRequestResponse>>> {
    let httpParams = new HttpParams();
    
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (params.status && params.status !== 'All') httpParams = httpParams.set('status', params.status);
    if (params.examId) httpParams = httpParams.set('examId', params.examId);
    if (params.courseId) httpParams = httpParams.set('courseId', params.courseId);
    if (params.searchTerm) httpParams = httpParams.set('searchTerm', params.searchTerm);

    return this.http.get<ApiResponse<PagedResponse<ReattemptRequestResponse>>>(`${this.apiUrl}/reattempt-requests`, { params: httpParams });
  }

  /**
   * Student: Returns all re-attempt requests submitted by the currently authenticated student.
   */
  getMyRequests(): Observable<ApiResponse<StudentReattemptStatusResponse[]>> {
    return this.http.get<ApiResponse<StudentReattemptStatusResponse[]>>(`${this.apiUrl}/reattempt-requests/mine`);
  }

  /**
   * Admin/Tutor reviews a pending re-attempt request (approve or reject).
   */
  reviewRequest(requestId: string, request: ReviewReattemptRequest): Observable<ApiResponse<ReattemptRequestResponse>> {
    return this.http.patch<ApiResponse<ReattemptRequestResponse>>(`${this.apiUrl}/reattempt-requests/${requestId}/review`, request);
  }
}
