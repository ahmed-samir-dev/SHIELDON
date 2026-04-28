import { Component, effect, HostListener, signal, ViewChild, ElementRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationResponse, NotificationType } from '../../../core/models/notification.model';
import { animate, state, style, transition, trigger } from '@angular/animations';

@Component({
  selector: 'app-notification-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-panel.component.html',
  styleUrls: ['./notification-panel.component.scss'],
  animations: [
    trigger('panelState', [
      state('closed', style({
        opacity: 0,
        transform: 'translateY(-10px) scale(0.95)',
        visibility: 'hidden'
      })),
      state('open', style({
        opacity: 1,
        transform: 'translateY(0) scale(1)',
        visibility: 'visible'
      })),
      transition('closed => open', animate('200ms cubic-bezier(0.175, 0.885, 0.32, 1.275)')),
      transition('open => closed', animate('150ms cubic-bezier(0.4, 0.0, 0.2, 1)'))
    ])
  ]
})
export class NotificationPanelComponent implements OnInit {
  isOpen = signal<boolean>(false);
  isExpanded = signal<boolean>(false);
  
  @ViewChild('panelContainer') panelContainer!: ElementRef;
  @ViewChild('bellButton') bellButton!: ElementRef;

  constructor(
    public notificationService: NotificationService,
    private router: Router,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit() {
    this.notificationService.fetchUnreadCount();
  }

  togglePanel(event: Event) {
    event.stopPropagation();
    const willOpen = !this.isOpen();
    this.isOpen.set(willOpen);
    if (!willOpen) {
      this.isExpanded.set(false); // Reset when closing
    }
    
    if (willOpen) {
      // Always fetch fresh on open
      this.notificationService.fetchNotifications(1, 20);
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.isOpen()) return;

    const target = event.target as HTMLElement;
    const clickedInsidePanel = this.panelContainer?.nativeElement.contains(target);
    const clickedBell = this.bellButton?.nativeElement.contains(target);

    if (!clickedInsidePanel && !clickedBell) {
      this.isOpen.set(false);
      this.isExpanded.set(false);
    }
  }

  handleNotificationClick(notification: NotificationResponse) {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe();
    }
    
    this.isOpen.set(false);
    this.isExpanded.set(false);
    
    if (notification.actionUrl) {
      this.router.navigateByUrl(notification.actionUrl);
    }
  }

  markAllAsRead(event: Event) {
    event.stopPropagation();
    this.notificationService.markAllAsRead().subscribe();
  }

  clearAll(event: Event) {
    event.stopPropagation();
    this.notificationService.deleteAll().subscribe();
  }

  expandPanel(event: Event) {
    event.stopPropagation();
    this.isExpanded.set(true);
  }

  getSvgForType(type: NotificationType): SafeHtml {
    let svg = '';
    switch (type) {
      case NotificationType.EnrollmentApproved: 
        svg = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>`;
        break;
      case NotificationType.EnrollmentRejected: 
        svg = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>`;
        break;
      case NotificationType.ImportantCourseAnnouncement: 
        svg = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>`;
        break;
      case NotificationType.NewCourseAnnouncement: 
        svg = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 11l18-5v12L3 14v-3z"/><path d="M11.6 16.8a3 3 0 1 1-5.8-1.6"/></svg>`;
        break;
      case NotificationType.NewCourseMaterial: 
        svg = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/></svg>`;
        break;
      case NotificationType.NewCourseAssignment: 
        svg = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"/><rect x="8" y="2" width="8" height="4" rx="1" ry="1"/></svg>`;
        break;
      case NotificationType.ExamScheduled: 
        // Modern descriptive icon: Document with a star/sparkle for a new exam
        svg = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><path d="M12 12.5l1.3 2.5 2.7.4-2 2 .5 2.6-2.5-1.3-2.5 1.3.5-2.6-2-2 2.7-.4z"/></svg>`;
        break;
      default: 
        svg = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/></svg>`;
        break;
    }
    return this.sanitizer.bypassSecurityTrustHtml(svg);
  }

  getColorForType(type: NotificationType): string {
    switch (type) {
      case NotificationType.EnrollmentApproved: return '#10b981'; // Green
      case NotificationType.EnrollmentRejected: return '#ef4444'; // Red
      case NotificationType.ImportantCourseAnnouncement: return '#f59e0b'; // Orange
      case NotificationType.NewCourseAnnouncement: return '#3b82f6'; // Blue
      case NotificationType.NewCourseMaterial: return '#8b5cf6'; // Purple
      case NotificationType.NewCourseAssignment: return '#ec4899'; // Pink
      case NotificationType.ExamScheduled: return '#f59e0b'; // Amber
      default: return '#6b7280'; // Gray
    }
  }
}
