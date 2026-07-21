import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import {
  LeaderboardResponse,
  LeaderboardSettings,
  UpdateLeaderboardSettingsRequest,
} from '../models/leaderboard.model';

@Injectable({ providedIn: 'root' })
export class LeaderboardService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;
  private hubBaseUrl = environment.apiUrl.replace('/api', '');

  private hubConnection: HubConnection | null = null;

  /** Emits the latest leaderboard snapshot pushed by the server via SignalR. */
  private _leaderboardUpdated = new Subject<LeaderboardResponse>();
  readonly leaderboardUpdated$ = this._leaderboardUpdated.asObservable();

  // ── REST API ───────────────────────────────────────────────────────────────

  /** GET /api/courses/{courseId}/leaderboard */
  getLeaderboard(courseId: string): Observable<LeaderboardResponse> {
    return this.http.get<LeaderboardResponse>(`${this.baseUrl}/courses/${courseId}/leaderboard`);
  }

  /** GET /api/courses/{courseId}/leaderboard/settings (Tutor/Admin only) */
  getSettings(courseId: string): Observable<LeaderboardSettings> {
    return this.http.get<LeaderboardSettings>(`${this.baseUrl}/courses/${courseId}/leaderboard/settings`);
  }

  /** PUT /api/courses/{courseId}/leaderboard/settings (Tutor/Admin only) */
  updateSettings(courseId: string, request: UpdateLeaderboardSettingsRequest): Observable<LeaderboardSettings> {
    return this.http.put<LeaderboardSettings>(`${this.baseUrl}/courses/${courseId}/leaderboard/settings`, request);
  }

  /** POST /api/courses/{courseId}/leaderboard/refresh (Tutor/Admin only) */
  refreshLeaderboard(courseId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/courses/${courseId}/leaderboard/refresh`, {});
  }

  // ── SignalR ────────────────────────────────────────────────────────────────

  /** Start SignalR connection to LeaderboardHub and join the course group. */
  async startConnection(courseId: string, accessToken: string): Promise<void> {
    if (this.hubConnection) return; // Already connected

    const hubUrl = `${this.hubBaseUrl}/hubs/leaderboard`;

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => accessToken })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.hubConnection.on('LeaderboardUpdated', (payload: LeaderboardResponse) => {
      this._leaderboardUpdated.next(payload);
    });

    await this.hubConnection.start();
    await this.joinCourseLeaderboard(courseId);
  }

  /** Join the SignalR room for a specific course leaderboard. */
  private async joinCourseLeaderboard(courseId: string): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.hubConnection.invoke('JoinCourseLeaderboard', courseId);
    }
  }

  /** Leave the SignalR room and stop the connection. */
  async stopConnection(courseId: string): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('LeaveCourseLeaderboard', courseId);
      } catch {
        // Ignore errors on leave — connection may already be closing
      }
      await this.hubConnection.stop();
    }
    this.hubConnection = null;
  }
}
