import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';
import { NotificationPanelComponent } from '../../shared/components/notification-panel/notification-panel.component';
import { AiChatPanelComponent } from '../../shared/components/ai-chat-panel/ai-chat-panel';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, NotificationPanelComponent, AiChatPanelComponent],
  templateUrl: './dashboard-layout.html',
  styleUrl: './dashboard-layout.scss'
})
export class DashboardLayout {
  authService = inject(AuthService);
  private router = inject(Router);

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
}
