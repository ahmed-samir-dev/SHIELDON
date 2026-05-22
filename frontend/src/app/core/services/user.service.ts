import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResponse } from '../models/api-response.model';
import { UserDetailDto, UserFilterParams } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getUsers(filters: UserFilterParams): Observable<PagedResponse<UserDetailDto>> {
    let params = new HttpParams();
    if (filters.page)     params = params.set('page', filters.page);
    if (filters.pageSize) params = params.set('pageSize', filters.pageSize);
    if (filters.search)   params = params.set('search', filters.search);
    if (filters.role)     params = params.set('role', filters.role);
    if (filters.status)   params = params.set('status', filters.status);

    return this.http.get<ApiResponse<PagedResponse<UserDetailDto>>>(this.apiUrl, { params })
      .pipe(map(r => r.data!));
  }

  lockUser(userId: string): Observable<void> {
    return this.http.post<ApiResponse<object>>(`${this.apiUrl}/${userId}/lock`, {})
      .pipe(map(() => void 0));
  }

  unlockUser(id: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/${id}/unlock`, {});
  }
}
