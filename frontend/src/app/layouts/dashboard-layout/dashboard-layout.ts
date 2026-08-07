import { Component, inject, AfterViewInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { AiService } from '../../core/services/ai.service';
import { ShepherdService } from '../../core/services/shepherd.service';
import { environment } from '../../../environments/environment';
import { NotificationPanelComponent } from '../../shared/components/notification-panel/notification-panel.component';
import { AiChatPanelComponent } from '../../shared/components/ai-chat-panel/ai-chat-panel';
import { GlobalCallOverlayComponent } from '../../shared/components/global-call-overlay/global-call-overlay';
import { ChatService } from '../../core/services/chat.service';
import { SecuritySignalrService } from '../../core/services/security-signalr.service';
import { ThemeService } from '../../core/services/theme.service';
import { LanguageService } from '../../core/services/language.service';
import { LayoutService } from '../../core/services/layout.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { OtpModalService } from '../../core/services/otp-modal.service';
import Swal from 'sweetalert2';
import { OtpModalComponent } from '../../shared/components/otp-modal/otp-modal';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, NotificationPanelComponent, AiChatPanelComponent, GlobalCallOverlayComponent, TranslateModule, OtpModalComponent],
  templateUrl: './dashboard-layout.html',
  styleUrl: './dashboard-layout.scss',
  providers: [AiService]
})
export class DashboardLayout implements AfterViewInit {
  authService = inject(AuthService);
  shepherdService = inject(ShepherdService);
  chatService = inject(ChatService);
  securitySignalrService = inject(SecuritySignalrService);
  themeService = inject(ThemeService);
  languageService = inject(LanguageService);
  layoutService = inject(LayoutService);
  otpModalService = inject(OtpModalService);
  router = inject(Router);
  translate = inject(TranslateService);

  isMobileMenuOpen = false;
  isSidebarCollapsed = this.layoutService.isSidebarCollapsed;
  apiUrl = environment.apiUrl.replace('/api', '');

  constructor() {
    this.router.events.subscribe(() => {
      if (this.isMobileMenuOpen) {
        this.isMobileMenuOpen = false;
      }
    });
  }

  toggleMobileMenu(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }

  toggleSidebar(): void {
    this.layoutService.toggleSidebar();
  }

  async logout(): Promise<void> {
    const result = await Swal.fire({
      title: this.translate.instant('NAVBAR.CONFIRM_LOGOUT_TITLE'),
      text: this.translate.instant('NAVBAR.CONFIRM_LOGOUT_TEXT'),
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: this.translate.instant('NAVBAR.CONFIRM_LOGOUT_YES'),
      cancelButtonText: this.translate.instant('NAVBAR.CONFIRM_LOGOUT_NO'),
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#6b7280'
    });

    if (result.isConfirmed) {
      this.authService.logout();
    }
  }

  getAvatarUrl(): string {
    const user = this.authService.currentUser();
    if (user?.profilePictureUrl) {
      return `${this.apiUrl}/${user.profilePictureUrl}`;
    }
    return '';
  }

  getInitials(): string {
    const user = this.authService.currentUser();
    if (!user) return '?';
    return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  }

  ngAfterViewInit(): void {
    // Wait a brief moment for the dashboard elements to fully render
    setTimeout(() => {
      const user = this.authService.currentUser();
      // user might be null if guard is still resolving, but guard usually finishes first
      if (user && user.hasCompletedOnboarding === false) {
        this.shepherdService.startTour(user.role as any);
      }
    }, 500);

    // Start global chat connection for incoming messages toast & unread counts
    this.chatService.startConnection();
    // Start global security socket connection for concurrency logout & security checks
    this.securitySignalrService.startConnection();
  }

  ngOnDestroy(): void {
    this.chatService.stopConnection();
    this.securitySignalrService.stopConnection();
  }
}
