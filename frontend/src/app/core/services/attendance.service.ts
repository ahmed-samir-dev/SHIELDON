import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { 
  AttendanceCheckDto, 
  AttendanceCheckDetailDto, 
  StartCheckRequest, 
  ScanRequest, 
  AttendanceRecordDto, 
  StudentAttendanceHistoryDto,
  QrUpdatedDto,
  AttendanceMarkedDto
} from '../models/attendance.model';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class AttendanceService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  
  private hubConnection: HubConnection | null = null;
  
  // Signals for active session tracking
  readonly activeQrPayload = signal<QrUpdatedDto | null>(null);
  readonly liveRecordUpdates = signal<AttendanceMarkedDto | null>(null);
  readonly isSignalRConnected = signal<boolean>(false);

  // ── REST API Methods ──

  startCheck(request: StartCheckRequest): Observable<ApiResponse<AttendanceCheckDto>> {
    return this.http.post<ApiResponse<AttendanceCheckDto>>(`${environment.apiUrl}/attendance/checks`, request);
  }

  endCheck(checkId: string): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/attendance/checks/${checkId}/end`, {});
  }

  scanQrCode(checkId: string, request: ScanRequest): Observable<ApiResponse<AttendanceRecordDto>> {
    return this.http.post<ApiResponse<AttendanceRecordDto>>(`${environment.apiUrl}/attendance/checks/${checkId}/scan`, request);
  }

  manualMark(checkId: string, studentId: string): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${environment.apiUrl}/attendance/checks/${checkId}/manual/${studentId}`, {});
  }

  getCheckDetails(checkId: string): Observable<ApiResponse<AttendanceCheckDetailDto>> {
    return this.http.get<ApiResponse<AttendanceCheckDetailDto>>(`${environment.apiUrl}/attendance/checks/${checkId}`);
  }

  getCourseHistory(courseId: string): Observable<ApiResponse<AttendanceCheckDto[]>> {
    return this.http.get<ApiResponse<AttendanceCheckDto[]>>(`${environment.apiUrl}/attendance/courses/${courseId}/history`);
  }

  getStudentHistory(): Observable<ApiResponse<StudentAttendanceHistoryDto[]>> {
    return this.http.get<ApiResponse<StudentAttendanceHistoryDto[]>>(`${environment.apiUrl}/attendance/my-history`);
  }

  getAllChecksAdmin(): Observable<ApiResponse<AttendanceCheckDto[]>> {
    return this.http.get<ApiResponse<AttendanceCheckDto[]>>(`${environment.apiUrl}/attendance/all`);
  }

  getCurrentQr(checkId: string): Observable<ApiResponse<QrUpdatedDto>> {
    return this.http.get<ApiResponse<QrUpdatedDto>>(`${environment.apiUrl}/attendance/checks/${checkId}/current-qr`);
  }

  // ── SignalR Methods ──

  startSignalRConnection(): Promise<void> {
    if (this.hubConnection?.state === 'Connected') return Promise.resolve();

    const token = this.authService.getAccessToken();
    if (!token) return Promise.reject('No access token');

    const hubUrl = environment.apiUrl.replace('/api', '') + '/hubs/attendance';

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this.setupSignalREvents();

    this.hubConnection.onreconnecting(() => this.isSignalRConnected.set(false));
    this.hubConnection.onreconnected(() => this.isSignalRConnected.set(true));
    this.hubConnection.onclose(() => this.isSignalRConnected.set(false));

    return this.hubConnection.start().then(() => {
      this.isSignalRConnected.set(true);
      console.log('SignalR Attendance Hub connected');
    });
  }

  stopSignalRConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
      this.isSignalRConnected.set(false);
    }
  }

  joinCheckAsTutor(checkId: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') return Promise.reject('Not connected');
    return this.hubConnection.invoke('JoinCheckAsHost', checkId);
  }

  joinCheckAsStudent(checkId: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') return Promise.reject('Not connected');
    return this.hubConnection.invoke('JoinCheckAsStudent', checkId);
  }

  leaveCheck(checkId: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') return Promise.resolve();
    return this.hubConnection.invoke('LeaveCheck', checkId);
  }

  private setupSignalREvents(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('QrUpdated', (dto: QrUpdatedDto) => {
      this.activeQrPayload.set(dto);
    });

    this.hubConnection.on('AttendanceMarked', (dto: AttendanceMarkedDto) => {
      // We set this signal, and any component listening can react and then clear it if they want
      this.liveRecordUpdates.set(dto);
    });
  }
}
