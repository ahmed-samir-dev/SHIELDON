import { Component, inject, AfterViewInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { AiService } from '../../core/services/ai.service';
import { ShepherdService } from '../../core/services/shepherd.service';
import { environment } from '../../../environments/environment';
import { NotificationPanelComponent } from '../../shared/components/notification-panel/notification-panel.component';
import { AiChatPanelComponent } from '../../shared/components/ai-chat-panel/ai-chat-panel';
import { ChatService } from '../../core/services/chat.service';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, NotificationPanelComponent, AiChatPanelComponent],
  templateUrl: './dashboard-layout.html',
  styleUrl: './dashboard-layout.scss',
  providers: [AiService]
})
export class DashboardLayout implements AfterViewInit {
  authService = inject(AuthService);
  shepherdService = inject(ShepherdService);
  chatService = inject(ChatService);
  router = inject(Router);

  isMobileMenuOpen = false;
  apiUrl = environment.apiUrl.replace('/api', '');

  toggleMobileMenu(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }

  logout(): void {
    this.authService.logout();
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
  }

  ngOnDestroy(): void {
    this.chatService.stopConnection();
  }
}
