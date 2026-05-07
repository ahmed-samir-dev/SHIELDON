import { Component, Input, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, AlertCircle, AlertTriangle, Info, CheckCircle, XCircle, Monitor, WifiOff, ShieldAlert, FileText, Clock } from 'lucide-angular';
import { TimelineEventResponse, MonitoringService } from '../../../core/services/monitoring.service';

@Component({
  selector: 'app-session-timeline',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './session-timeline.html',
  styleUrls: ['./session-timeline.scss']
})
export class SessionTimelineComponent implements OnInit {
  @Input() attemptId!: string;
  
  private monitoring = inject(MonitoringService);

  // State
  events = signal<TimelineEventResponse[]>([]);
  loading = signal<boolean>(true);
  error = signal<string>('');
  filter = signal<string>('All'); // All | Normal | Warning | Violation | Critical

  // Icons mapping
  readonly iconMap: Record<string, string> = {
    'ExamStarted': 'CheckCircle',
    'PageRefreshed': 'Monitor',
    'Disconnected': 'WifiOff',
    'Reconnected': 'CheckCircle',
    'HeartbeatReceived': 'Info',
    'ExamSubmitted': 'CheckCircle',
    'ForceSubmitted': 'ShieldAlert',
    'AutoExpired': 'Clock',
    'UnexpectedExit': 'XCircle',
    'TutorTerminated': 'ShieldAlert',
    // Violation types
    'TabSwitch': 'AlertTriangle',
    'BrowserMinimize': 'AlertTriangle',
    'DevToolsOpened': 'ShieldAlert',
    'ClipboardPaste': 'AlertTriangle',
    'RightClick': 'Info',
    'MultipleFaces': 'ShieldAlert',
    'NoFace': 'ShieldAlert',
    'AudioDetected': 'ShieldAlert',
    'UnauthorizedDevice': 'ShieldAlert'
  };

  readonly severityColors: Record<string, string> = {
    'Info': 'text-blue-500 bg-blue-50',
    'Minor': 'text-yellow-600 bg-yellow-50',
    'Medium': 'text-orange-500 bg-orange-50',
    'Critical': 'text-red-600 bg-red-50'
  };

  readonly severityIconMap: Record<string, string> = {
    'Info': 'Info',
    'Minor': 'AlertCircle',
    'Medium': 'AlertTriangle',
    'Critical': 'ShieldAlert'
  };

  filteredEvents = computed(() => {
    const all = this.events();
    const currentFilter = this.filter();
    
    if (currentFilter === 'All') return all;
    if (currentFilter === 'Normal') return all.filter(e => e.severity === 'Info');
    if (currentFilter === 'Warning') return all.filter(e => e.severity === 'Minor' || e.severity === 'Medium');
    if (currentFilter === 'Violation') return all.filter(e => e.category === 'Violation');
    if (currentFilter === 'Critical') return all.filter(e => e.severity === 'Critical');
    
    return all;
  });

  ngOnInit() {
    this.loadTimeline();
  }

  loadTimeline() {
    if (!this.attemptId) return;
    
    this.loading.set(true);
    this.monitoring.getTimeline(this.attemptId).subscribe({
      next: (res) => {
        this.events.set(res.data || []);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load session timeline.');
        this.loading.set(false);
      }
    });
  }

  getIcon(event: TimelineEventResponse): string {
    if (this.iconMap[event.eventType]) {
      return this.iconMap[event.eventType];
    }
    return this.severityIconMap[event.severity] || 'Info';
  }

  getSeverityClasses(severity: string): string {
    return this.severityColors[severity] || this.severityColors['Info'];
  }
}
