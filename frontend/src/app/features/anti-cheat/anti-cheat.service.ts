import { Injectable, inject, signal, computed, NgZone, OnDestroy } from '@angular/core';
import { Subject, Subscription, interval } from 'rxjs';
import { bufferTime, filter } from 'rxjs/operators';
import { ViolationService, ViolationLogRequest, ViolationType, ViolationSeverity } from '../../core/services/violation.service';
import { environment } from '../../../environments/environment';

export interface ViolationEvent {
  type: ViolationType;
  severity: ViolationSeverity;
  description: string;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class AntiCheatService implements OnDestroy {
  private ngZone = inject(NgZone);
  private violationService = inject(ViolationService);

  // State
  private attemptId: string | null = null;
  private isMonitoring = false;
  
  // Strike System
  private _strikeScore = signal<number>(0);
  public strikeScore = this._strikeScore.asReadonly();
  
  public strikeLevel = computed(() => {
    const score = this._strikeScore();
    if (score >= 3.0) return 3; // Force submit
    if (score >= 2.0) return 2; // Final warning (orange)
    if (score >= 1.0) return 1; // First warning (yellow)
    return 0; // Clean
  });

  // Violation Stream
  private violations$ = new Subject<ViolationEvent>();
  
  // Cooldown Tracking (type -> last trigger time ms)
  private cooldowns = new Map<ViolationType, number>();

  // Unsynced Violations
  private violationBuffer: ViolationLogRequest[] = [];
  private syncSub?: Subscription;

  // Track original dimensions for resize comparison
  private originalWidth = 0;
  private originalHeight = 0;

  // Mouse activity tracking
  private mousePosBuffer: {x: number, y: number, time: number}[] = [];

  // Bindings for event listeners
  private boundHandleVisibilityChange = this.handleVisibilityChange.bind(this);
  private boundHandleBlur = this.handleBlur.bind(this);
  private boundHandleFocus = this.handleFocus.bind(this);
  private boundHandleKeyDown = this.handleKeyDown.bind(this);
  private boundHandleContextMenu = this.handleContextMenu.bind(this);
  private boundHandleResize = this.handleResize.bind(this);
  private boundHandleMouseLeave = this.handleMouseLeave.bind(this);
  private boundHandleMouseMove = this.handleMouseMove.bind(this);
  private boundHandleFullscreenChange = this.handleFullscreenChange.bind(this);
  private boundHandleBeforeUnload = this.handleBeforeUnload.bind(this);

  constructor() {
    // Process violations
    this.violations$.subscribe(v => {
      this.processViolation(v);
    });
  }

  ngOnDestroy(): void {
    this.stopMonitoring();
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  public startMonitoring(attemptId: string): void {
    if (this.isMonitoring) return;
    this.attemptId = attemptId;
    this.isMonitoring = true;
    
    // Reset state
    this._strikeScore.set(0);
    this.cooldowns.clear();
    this.violationBuffer = [];
    this.mousePosBuffer = [];
    this.originalWidth = window.innerWidth;
    this.originalHeight = window.innerHeight;

    // We use ngZone.runOutsideAngular for high-frequency events to avoid change detection thrashing
    this.ngZone.runOutsideAngular(() => {
      document.addEventListener('visibilitychange', this.boundHandleVisibilityChange);
      window.addEventListener('blur', this.boundHandleBlur);
      window.addEventListener('focus', this.boundHandleFocus);
      document.addEventListener('keydown', this.boundHandleKeyDown, { capture: true });
      document.addEventListener('contextmenu', this.boundHandleContextMenu, { capture: true });
      window.addEventListener('resize', this.boundHandleResize);
      document.addEventListener('mouseleave', this.boundHandleMouseLeave);
      document.addEventListener('mousemove', this.boundHandleMouseMove);
      document.addEventListener('fullscreenchange', this.boundHandleFullscreenChange);
      window.addEventListener('beforeunload', this.boundHandleBeforeUnload);
    });

    // Start periodic sync (every 10 seconds)
    this.syncSub = interval(10000).subscribe(() => this.syncViolations());
  }

  public resumeMonitoring(attemptId: string): void {
    // On reconnect, we need to fetch existing violations to restore the strike score
    this.startMonitoring(attemptId);
    
    // Actually, backend needs a way to fetch student's own violations. 
    // Since our API currently only exposes GET /api/attempts/{id}/violations to Tutor/Admin,
    // we can either add a student endpoint or just resume from 0 visually, but backend still has the full log.
    // For now, we will just resume from 0 visually (which is okay, if they force-quit they might lose warning context, 
    // but the backend will still add new strikes on top of old ones if we implement backend tallying, 
    // or we just let it be a fresh 3 strikes per session. Let's keep it simple: fresh session visually).
    
    this.requestFullscreen();
  }

  public stopMonitoring(wasAutoSubmit: boolean = false): void {
    if (!this.isMonitoring) return;
    this.isMonitoring = false;

    // Remove listeners
    document.removeEventListener('visibilitychange', this.boundHandleVisibilityChange);
    window.removeEventListener('blur', this.boundHandleBlur);
    window.removeEventListener('focus', this.boundHandleFocus);
    document.removeEventListener('keydown', this.boundHandleKeyDown, { capture: true });
    document.removeEventListener('contextmenu', this.boundHandleContextMenu, { capture: true });
    window.removeEventListener('resize', this.boundHandleResize);
    document.removeEventListener('mouseleave', this.boundHandleMouseLeave);
    document.removeEventListener('mousemove', this.boundHandleMouseMove);
    document.removeEventListener('fullscreenchange', this.boundHandleFullscreenChange);
    window.removeEventListener('beforeunload', this.boundHandleBeforeUnload);

    this.syncSub?.unsubscribe();

    // Mark last violation as auto-submit trigger if applicable
    if (wasAutoSubmit && this.violationBuffer.length > 0) {
      this.violationBuffer[this.violationBuffer.length - 1].wasAutoSubmit = true;
    }

    // Final sync
    this.syncViolations();
    this.attemptId = null;
  }

  public requestFullscreen(): void {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen().catch(err => {
        console.warn('Fullscreen request blocked by browser:', err);
      });
    }
  }

  // ── Processing ────────────────────────────────────────────────────────────

  private processViolation(event: ViolationEvent): void {
    // Cooldown check
    const now = Date.now();
    const last = this.cooldowns.get(event.type) || 0;
    const cooldownMs = this.getCooldown(event.type);

    if (now - last < cooldownMs) return; // Ignore if in cooldown
    this.cooldowns.set(event.type, now);

    // Accumulate Score
    const weight = this.getSeverityWeight(event.severity);
    
    this.ngZone.run(() => {
      this._strikeScore.update(s => s + weight);
    });

    // Buffer for API
    if (this.attemptId) {
      this.violationBuffer.push({
        attemptId: this.attemptId,
        type: event.type,
        severity: event.severity,
        description: event.description,
        occurredAt: event.timestamp.toISOString(),
        wasAutoSubmit: false
      });
    }

    // Auto re-enter fullscreen if needed
    if (event.type === 'FullScreenExit') {
      setTimeout(() => this.requestFullscreen(), 1500);
    }
  }

  private getSeverityWeight(severity: ViolationSeverity): number {
    switch (severity) {
      case 'Minor': return 0.25;
      case 'Medium': return 0.5;
      case 'Critical': return 1.0;
      default: return 0.25;
    }
  }

  private getCooldown(type: ViolationType): number {
    switch (type) {
      case 'FocusLoss': return 3000;
      case 'TabSwitch': return 5000;
      case 'FullScreenExit': return 8000;
      case 'RestrictedShortcut': return 1000;
      case 'AbnormalMouseActivity': return 30000; // Rate limit this heavily
      case 'WindowResize': return 2000;
      case 'SplitScreen': return 5000;
      default: return 2000;
    }
  }

  // ── Sync ──────────────────────────────────────────────────────────────────

  private syncViolations(): void {
    if (this.violationBuffer.length === 0) return;

    const payload = { violations: [...this.violationBuffer] };
    this.violationBuffer = []; // Clear buffer immediately

    this.violationService.logViolationBatch(payload).subscribe({
      next: () => {},
      error: (err) => {
        // If it fails, put them back to try again later
        console.error('Failed to sync violations', err);
        this.violationBuffer.push(...payload.violations);
      }
    });
  }

  private handleBeforeUnload(e: BeforeUnloadEvent): void {
    if (this.violationBuffer.length > 0 && this.attemptId) {
      // Use sendBeacon for reliable delivery during page unload
      const url = `${environment.apiUrl}/violations/batch`;
      const token = localStorage.getItem('access_token');
      
      const payload = { violations: this.violationBuffer };
      const blob = new Blob([JSON.stringify(payload)], { type: 'application/json' });
      
      // Unfortunately sendBeacon doesn't easily support Auth headers.
      // We will try standard fetch with keepalive.
      if (token) {
        fetch(url, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
          },
          body: JSON.stringify(payload),
          keepalive: true
        }).catch(() => {}); // Fire and forget
      }
    }
    
    // Suggest the browser show a confirmation dialog
    if (this.isMonitoring) {
      e.preventDefault();
      e.returnValue = 'You have an active exam. Leaving this page may cause it to be submitted automatically.';
    }
  }

  // ── Event Handlers ────────────────────────────────────────────────────────

  private handleFullscreenChange(): void {
    if (!document.fullscreenElement && this.isMonitoring) {
      this.violations$.next({
        type: 'FullScreenExit',
        severity: 'Critical',
        description: 'Exited fullscreen mode.',
        timestamp: new Date()
      });
    }
  }

  private handleVisibilityChange(): void {
    if (document.visibilityState === 'hidden' && this.isMonitoring) {
      this.violations$.next({
        type: 'TabSwitch',
        severity: 'Critical',
        description: 'Switched tabs or minimized browser.',
        timestamp: new Date()
      });
    }
  }

  private handleBlur(): void {
    if (!this.isMonitoring) return;
    this.violations$.next({
      type: 'FocusLoss',
      severity: 'Critical',
      description: 'Exam window lost focus.',
      timestamp: new Date()
    });
  }

  private handleFocus(): void {
    // Logged return, no violation
  }

  private handleKeyDown(e: KeyboardEvent): void {
    if (!this.isMonitoring) return;

    let blocked = false;
    let desc = '';

    // Clipboard
    if (e.ctrlKey && (e.key === 'c' || e.key === 'C')) { blocked = true; desc = 'Attempted to Copy'; }
    if (e.ctrlKey && (e.key === 'v' || e.key === 'V')) { blocked = true; desc = 'Attempted to Paste'; }
    if (e.ctrlKey && (e.key === 'x' || e.key === 'X')) { blocked = true; desc = 'Attempted to Cut'; }
    if (e.ctrlKey && (e.key === 'a' || e.key === 'A')) { blocked = true; desc = 'Attempted to Select All'; }
    
    // Dev Tools & View Source
    if (e.key === 'F12') { blocked = true; desc = 'Attempted to open Developer Tools'; }
    if (e.ctrlKey && e.shiftKey && (e.key === 'i' || e.key === 'I')) { blocked = true; desc = 'Attempted to open Developer Tools'; }
    if (e.ctrlKey && e.shiftKey && (e.key === 'j' || e.key === 'J')) { blocked = true; desc = 'Attempted to open Developer Console'; }
    if (e.ctrlKey && (e.key === 'u' || e.key === 'U')) { blocked = true; desc = 'Attempted to View Source'; }
    
    // Print & Search
    if (e.ctrlKey && (e.key === 'p' || e.key === 'P')) { blocked = true; desc = 'Attempted to Print'; }
    if (e.ctrlKey && (e.key === 'f' || e.key === 'F')) { blocked = true; desc = 'Attempted to Find'; }
    if (e.ctrlKey && (e.key === 'g' || e.key === 'G')) { blocked = true; desc = 'Attempted to Find Next'; }
    
    // Esc (Fullscreen exit prevention)
    if (e.key === 'Escape') { blocked = true; desc = 'Attempted to exit fullscreen via Esc'; }

    // Alt+Tab cannot be reliably captured in JS natively, 
    // but we can try to catch Alt key alone if they hold it
    if (e.altKey && e.key === 'Tab') { blocked = true; desc = 'Attempted Alt+Tab'; }

    if (blocked) {
      e.preventDefault();
      e.stopPropagation();
      this.violations$.next({
        type: 'RestrictedShortcut',
        severity: 'Medium',
        description: desc,
        timestamp: new Date()
      });
    }
  }

  private handleContextMenu(e: MouseEvent): void {
    if (!this.isMonitoring) return;
    e.preventDefault();
    this.violations$.next({
      type: 'ClipboardPaste', // Close enough proxy for right click
      severity: 'Medium',
      description: 'Right-click context menu blocked.',
      timestamp: new Date()
    });
  }

  private resizeTimeout: any;
  private handleResize(): void {
    if (!this.isMonitoring) return;
    
    clearTimeout(this.resizeTimeout);
    this.resizeTimeout = setTimeout(() => {
      const w = window.innerWidth;
      
      if (w < window.screen.width * 0.7) {
        this.violations$.next({
          type: 'SplitScreen',
          severity: 'Medium',
          description: `Window resized significantly (${w}px). Possible split screen.`,
          timestamp: new Date()
        });
      } else if (w < this.originalWidth * 0.9) {
        this.violations$.next({
          type: 'WindowResize',
          severity: 'Medium',
          description: `Window resized.`,
          timestamp: new Date()
        });
      }
    }, 500);
  }

  private handleMouseLeave(e: MouseEvent): void {
    if (!this.isMonitoring) return;
    // If mouse left the HTML document completely
    if (e.clientY <= 0 || e.clientX <= 0 || (e.clientX >= window.innerWidth || e.clientY >= window.innerHeight)) {
      this.violations$.next({
        type: 'AbnormalMouseActivity',
        severity: 'Minor',
        description: 'Mouse left the browser window boundary.',
        timestamp: new Date()
      });
    }
  }

  private handleMouseMove(e: MouseEvent): void {
    if (!this.isMonitoring) return;
    const now = Date.now();
    
    // Only sample every 200ms
    if (this.mousePosBuffer.length > 0) {
      const last = this.mousePosBuffer[this.mousePosBuffer.length - 1];
      if (now - last.time < 200) return;
    }
    
    this.mousePosBuffer.push({ x: e.clientX, y: e.clientY, time: now });
    
    // Keep last 15 samples (approx 3 seconds)
    if (this.mousePosBuffer.length > 15) {
      this.mousePosBuffer.shift();
      
      // Analyze velocity
      let totalDist = 0;
      for (let i = 1; i < this.mousePosBuffer.length; i++) {
        const p1 = this.mousePosBuffer[i - 1];
        const p2 = this.mousePosBuffer[i];
        totalDist += Math.sqrt(Math.pow(p2.x - p1.x, 2) + Math.pow(p2.y - p1.y, 2));
      }
      
      const timeDiff = (this.mousePosBuffer[this.mousePosBuffer.length - 1].time - this.mousePosBuffer[0].time) / 1000;
      const velocity = totalDist / timeDiff; // pixels per second
      
      // If erratic mouse movement (> 3000 px/sec average over 3 seconds)
      if (velocity > 3000) {
        this.violations$.next({
          type: 'AbnormalMouseActivity',
          severity: 'Minor',
          description: 'Erratic/abnormal rapid mouse movements detected.',
          timestamp: new Date()
        });
        this.mousePosBuffer = []; // Reset
      }
    }
  }
}
