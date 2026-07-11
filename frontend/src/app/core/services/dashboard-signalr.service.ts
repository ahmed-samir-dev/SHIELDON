import { Injectable, inject, NgZone } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import * as signalR from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class DashboardSignalRService {
  private authService = inject(AuthService);
  private zone = inject(NgZone);
  private hubConnection: signalR.HubConnection | null = null;
  private dashboardUpdatedSubject = new Subject<void>();

  public dashboardUpdated$: Observable<void> = this.dashboardUpdatedSubject.asObservable();

  public startConnection(): void {
    if (this.hubConnection && this.hubConnection.state !== signalR.HubConnectionState.Disconnected) {
      return;
    }

    const token = this.authService.getAccessToken();
    if (!token) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/dashboard`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .then(() => console.log('DashboardHub connected'))
      .catch(err => console.error('Error while starting DashboardHub connection: ', err));

    this.hubConnection.on('DashboardUpdated', () => {
      this.zone.run(() => {
        this.dashboardUpdatedSubject.next();
      });
    });
  }

  public stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
    }
  }
}

