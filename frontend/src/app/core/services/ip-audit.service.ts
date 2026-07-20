import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface IpAuditLogDto {
  id: string;
  userId: string;
  userFullName: string;
  userDisplayId?: string;
  eventType: number;
  eventTypeLabel: string;
  ipAddress?: string;
  userAgent?: string;
  examAttemptId?: string;
  isVpnOrProxy: boolean;
  isDuplicateSession: boolean;
  isNetworkChangeDuringExam: boolean;
  occurredAt: string;
}

export interface AuditTrailQueryParams {
  page: number;
  pageSize: number;
  userId?: string;
  eventType?: string;
  isVpnOrProxy?: boolean;
  isDuplicateSession?: boolean;
  isNetworkChangeDuringExam?: boolean;
  fromDate?: string;
  toDate?: string;
}

export interface AuditTrailPagedResult {
  items: IpAuditLogDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class IpAuditService {
  private apiUrl = `${environment.apiUrl}`;

  constructor(private http: HttpClient) {}

  getAuditTrail(filters: AuditTrailQueryParams): Observable<AuditTrailPagedResult> {
    let params = new HttpParams();
    params = params.set('page', filters.page);
    params = params.set('pageSize', filters.pageSize);

    if (filters.userId) params = params.set('userId', filters.userId);
    if (filters.eventType) params = params.set('eventType', filters.eventType);
    if (filters.isVpnOrProxy !== undefined && filters.isVpnOrProxy !== null) {
      params = params.set('isVpnOrProxy', filters.isVpnOrProxy);
    }
    if (filters.isDuplicateSession !== undefined && filters.isDuplicateSession !== null) {
      params = params.set('isDuplicateSession', filters.isDuplicateSession);
    }
    if (filters.isNetworkChangeDuringExam !== undefined && filters.isNetworkChangeDuringExam !== null) {
      params = params.set('isNetworkChangeDuringExam', filters.isNetworkChangeDuringExam);
    }
    if (filters.fromDate) params = params.set('fromDate', filters.fromDate);
    if (filters.toDate) params = params.set('toDate', filters.toDate);

    return this.http.get<ApiResponse<AuditTrailPagedResult>>(`${this.apiUrl}/admin/audit-trail`, { params })
      .pipe(map(r => r.data!));
  }

  getLogsForUser(userId: string): Observable<IpAuditLogDto[]> {
    return this.http.get<ApiResponse<IpAuditLogDto[]>>(`${this.apiUrl}/users/${userId}/ip-logs`)
      .pipe(map(r => r.data!));
  }

  getLogsForAttempt(attemptId: string): Observable<IpAuditLogDto[]> {
    return this.http.get<ApiResponse<IpAuditLogDto[]>>(`${this.apiUrl}/attempts/${attemptId}/ip-logs`)
      .pipe(map(r => r.data!));
  }
}
