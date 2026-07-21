import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { TranslateService } from '@ngx-translate/core';
import Swal from 'sweetalert2';

@Injectable({
  providedIn: 'root'
})
export class SecuritySignalrService {
  private authService = inject(AuthService);
  private translate = inject(TranslateService);
  private hubConnection: HubConnection | null = null;

  public startConnection(): void {
    if (this.hubConnection) return;

    const token = this.authService.getAccessToken();
    if (!token) return;

    const hubUrl = environment.apiUrl.replace('/api', '') + '/hubs/security';

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.setupSignalREvents();

    this.hubConnection
      .start()
      .then(() => console.log('SecurityHub connected successfully'))
      .catch(err => console.error('Error starting SecurityHub connection:', err));
  }

  public stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
    }
  }

  private setupSignalREvents(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('ForceLogout', (backendMsg?: string) => {
      const alertTitle = this.translate.instant('AUDIT_TRAIL.CONCURRENT_LOGOUT_TITLE') || 'Security Alert';
      const alertText = this.translate.instant('AUDIT_TRAIL.CONCURRENT_LOGOUT_MESSAGE') || backendMsg || 'Your account was logged in from another device. Please log in again.';

      // Trigger a blocking SweetAlert2 overlay modal
      Swal.fire({
        title: alertTitle,
        text: alertText,
        icon: 'warning',
        allowOutsideClick: false,
        allowEscapeKey: false,
        allowEnterKey: false,
        showConfirmButton: false,
        timer: 7000,
        timerProgressBar: true,
        willClose: () => {
          this.authService.logout();
        }
      });
    });
  }
}
