import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';

export interface AnnouncementResponse {
  id: string;
  courseId: string;
  title: string;
  content: string;
  priority: 'Normal' | 'Important';
  createdByUserId: string;
  createdByName: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAnnouncementRequest {
  title: string;
  content: string;
  priority: string;
}

@Injectable({
  providedIn: 'root'
})
export class AnnouncementService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getAnnouncements(courseId: string): Observable<ApiResponse<AnnouncementResponse[]>> {
    return this.http.get<ApiResponse<AnnouncementResponse[]>>(
      `${this.apiUrl}/courses/${courseId}/announcements`
    );
  }

  createAnnouncement(courseId: string, request: CreateAnnouncementRequest): Observable<ApiResponse<AnnouncementResponse>> {
    return this.http.post<ApiResponse<AnnouncementResponse>>(
      `${this.apiUrl}/courses/${courseId}/announcements`, request
    );
  }

  deleteAnnouncement(courseId: string, announcementId: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(
      `${this.apiUrl}/courses/${courseId}/announcements/${announcementId}`
    );
  }
}
