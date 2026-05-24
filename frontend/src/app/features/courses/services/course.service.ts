import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse, PagedResponse } from '../../../core/models/api-response.model';
import {
  CreateCourseRequest,
  UpdateCourseRequest,
  CourseQueryParams,
  CourseResponse,
  CourseDetailResponse,
  EnrollmentResponse,
  StudentEnrollmentStatusResponse,
  ReviewEnrollmentRequest,
  BulkReviewEnrollmentRequest,
  UserBasicResponse,
  EnrollmentQueryParams,
  StudentEnrollmentQueryParams
} from '../../../core/models/courses.model';

@Injectable({
  providedIn: 'root'
})
export class CourseService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/courses`;

  // ── Course CRUD ────────────────────────────────────────────────────────

  getCourses(query: CourseQueryParams): Observable<ApiResponse<PagedResponse<CourseResponse>>> {
    let params = new HttpParams()
      .set('page', query.page || 1)
      .set('pageSize', query.pageSize || 10);

    if (query.search) {
      params = params.set('search', query.search);
    }

    if (query.isActive !== undefined && query.isActive !== null) {
      params = params.set('isActive', query.isActive);
    }

    if (query.enrollmentStatus) {
      params = params.set('enrollmentStatus', query.enrollmentStatus);
    }

    return this.http.get<ApiResponse<PagedResponse<CourseResponse>>>(this.baseUrl, { params });
  }

  getCourse(id: string): Observable<ApiResponse<CourseDetailResponse>> {
    return this.http.get<ApiResponse<CourseDetailResponse>>(`${this.baseUrl}/${id}`);
  }

  getTutors(): Observable<ApiResponse<UserBasicResponse[]>> {
    return this.http.get<ApiResponse<UserBasicResponse[]>>(`${environment.apiUrl}/users/tutors`);
  }

  createCourse(request: CreateCourseRequest): Observable<ApiResponse<CourseResponse>> {
    return this.http.post<ApiResponse<CourseResponse>>(this.baseUrl, request);
  }

  updateCourse(id: string, request: UpdateCourseRequest): Observable<ApiResponse<CourseResponse>> {
    return this.http.patch<ApiResponse<CourseResponse>>(`${this.baseUrl}/${id}`, request);
  }

  deleteCourse(id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.baseUrl}/${id}`);
  }

  // ── Enrollment Workflow ───────────────────────────────────────────────

  requestEnrollment(courseId: string): Observable<ApiResponse<StudentEnrollmentStatusResponse>> {
    return this.http.post<ApiResponse<StudentEnrollmentStatusResponse>>(`${this.baseUrl}/${courseId}/enroll`, {});
  }

  getPendingEnrollments(query: EnrollmentQueryParams = {}): Observable<ApiResponse<PagedResponse<EnrollmentResponse>>> {
    let params = new HttpParams();
    if (query.page) params = params.set('page', query.page);
    if (query.pageSize) params = params.set('pageSize', query.pageSize);
    if (query.search) params = params.set('search', query.search);
    if (query.courseId) params = params.set('courseId', query.courseId);

    return this.http.get<ApiResponse<PagedResponse<EnrollmentResponse>>>(`${this.baseUrl}/enrollments/pending`, { params });
  }

  getApprovedEnrollments(query: EnrollmentQueryParams = {}): Observable<ApiResponse<PagedResponse<EnrollmentResponse>>> {
    let httpParams = new HttpParams();
    if (query.page) httpParams = httpParams.set('page', query.page);
    if (query.pageSize) httpParams = httpParams.set('pageSize', query.pageSize);
    if (query.search) httpParams = httpParams.set('search', query.search);
    if (query.courseId) httpParams = httpParams.set('courseId', query.courseId);
    if (query.approvedFrom) httpParams = httpParams.set('approvedFrom', query.approvedFrom);
    if (query.approvedTo) httpParams = httpParams.set('approvedTo', query.approvedTo);

    return this.http.get<ApiResponse<PagedResponse<EnrollmentResponse>>>(`${this.baseUrl}/enrollments/approved`, { params: httpParams });
  }

  reviewEnrollment(enrollmentId: string, request: ReviewEnrollmentRequest): Observable<ApiResponse<EnrollmentResponse>> {
    return this.http.patch<ApiResponse<EnrollmentResponse>>(`${this.baseUrl}/enrollments/${enrollmentId}/review`, request);
  }

  bulkReviewEnrollments(request: BulkReviewEnrollmentRequest): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/enrollments/bulk-review`, request);
  }

  getMyEnrollments(query: StudentEnrollmentQueryParams = {}): Observable<ApiResponse<PagedResponse<StudentEnrollmentStatusResponse>>> {
    let params = new HttpParams();
    if (query.page) params = params.set('page', query.page);
    if (query.pageSize) params = params.set('pageSize', query.pageSize);
    if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);
    if (query.requestedFrom) params = params.set('requestedFrom', query.requestedFrom);
    if (query.requestedTo) params = params.set('requestedTo', query.requestedTo);
    if (query.status) params = params.set('status', query.status);

    return this.http.get<ApiResponse<PagedResponse<StudentEnrollmentStatusResponse>>>(`${this.baseUrl}/enrollments/my`, { params });
  }
}
