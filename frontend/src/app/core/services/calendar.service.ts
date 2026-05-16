import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { CalendarEventDto, CreateCustomEventRequest, UpdateCustomEventRequest } from '../models/calendar.model';

@Injectable({
  providedIn: 'root'
})
export class CalendarService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/calendar`;

  getEvents(start: string, end: string): Observable<ApiResponse<CalendarEventDto[]>> {
    const params = new HttpParams()
      .set('start', start)
      .set('end', end);
    return this.http.get<ApiResponse<CalendarEventDto[]>>(`${this.apiUrl}/events`, { params });
  }

  createCustomEvent(request: CreateCustomEventRequest): Observable<ApiResponse<CalendarEventDto>> {
    return this.http.post<ApiResponse<CalendarEventDto>>(`${this.apiUrl}/events/custom`, request);
  }

  updateCustomEvent(eventId: string, request: UpdateCustomEventRequest): Observable<ApiResponse<CalendarEventDto>> {
    return this.http.put<ApiResponse<CalendarEventDto>>(`${this.apiUrl}/events/custom/${eventId}`, request);
  }

  deleteCustomEvent(eventId: string): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.apiUrl}/events/custom/${eventId}`);
  }
}
