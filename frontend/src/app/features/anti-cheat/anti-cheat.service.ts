import { Injectable, inject, signal, computed, NgZone, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject, Subscription, interval } from 'rxjs';
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
  private http = inject(HttpClient);

  // State
  private attemptId: string | null = null;
  private isMonitoring = false;
  private forceSubmitInProgress = false;  // Guard to prevent duplicate force submits
  
  // Strike System
  private _strikeScore = signal<number>(0);
  public strikeScore = this._strikeScore.asReadonly();
  
  public getFormattedStrikeCount(): string {
    return this._strikeScore().toFixed(1).replace('.0', '');
  }
  
  private _strikeTwoAcknowledged = signal<boolean>(false);
  private _strikeOneAcknowledged = signal<boolean>(false);
  public strikeOneAcknowledged = this._strikeOneAcknowledged.asReadonly();
  
  public strikeLevel = computed(() => {
    const score = this._strikeScore();
    if (score >= 3.0) return 3; // Force submit
    if (score >= 2.0 && !this._strikeTwoAcknowledged()) return 2; // Final warning (orange)
    if (score >= 1.0 && !this._strikeOneAcknowledged()) return 1; // First warning (yellow)
    return 0; // Clean
  });

  public dismissStrikeOne(): void {
    this._strikeOneAcknowledged.set(true);
  }

  public dismissStrikeTwo(): void {
    this._strikeTwoAcknowledged.set(true);
  }

  /** Hard reset - call from ExamEngine ngOnInit to guarantee clean state on every visit */
  public resetState(): void {
    this._strikeScore.set(0);
    this._strikeTwoAcknowledged.set(false);
    this._strikeOneAcknowledged.set(false);
    this.forceSubmitInProgress = false;
    this.mousePosBuffer = [];
    this.pendingViolations = [];
    clearTimeout(this.aggregatorTimeout);
    this.aggregatorTimeout = null;
    this.lastGlobalViolationTime = 0;
    // Do NOT stop listeners here - startMonitoring handles that
  }

  // Violation Stream
  private violations$ = new Subject<ViolationEvent>();
  public violationOccurred$ = new Subject<ViolationEvent>();
  
  // Global Cooldown Tracking
  private lastGlobalViolationTime = 0;
  
  // Aggregation window
  private pendingViolations: ViolationEvent[] = [];
  private aggregatorTimeout: any;

  // Unsynced Violations
  private violationBuffer: ViolationLogRequest[] = [];
  private syncSub?: Subscription;
  private heartbeatSub?: Subscription;

  // Track original dimensions for resize comparison
  private originalWidth = 0;
  private originalHeight = 0;

  // Mouse activity tracking
  private mousePosBuffer: {x: number, y: number, time: number}[] = [];
  private monitoringStartTime = 0;

  // Bindings for event listeners
  private boundHandleKeyDown = this.handleKeyDown.bind(this);
  private boundHandleKeyUp = this.handleKeyUp.bind(this);
  private boundHandleContextMenu = this.handleContextMenu.bind(this);
  private boundHandleResize = this.handleResize.bind(this);
  private boundHandleMouseLeave = this.handleMouseLeave.bind(this);
  private boundHandleMouseMove = this.handleMouseMove.bind(this);
  private boundHandleBeforeUnload = this.handleBeforeUnload.bind(this);
  private boundHandleFullscreenChange = this.handleFullscreenChange.bind(this);
  private boundHandleVisibilityChange = this.handleVisibilityChange.bind(this);
  private boundHandleBlur = this.handleBlur.bind(this);
  private boundHandleOnline = this.handleOnline.bind(this);
  private boundHandleOffline = this.handleOffline.bind(this);

  constructor() {
    this.violations$.subscribe(v => {
      this.processViolation(v);
    });
  }

  ngOnDestroy(): void {
    this.stopMonitoring();
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  public startMonitoring(attemptId: string, initialStrikeScore: number = 0): void {
    // Always reset state when starting a new session, even if already monitoring
    if (this.isMonitoring) {
      this.removeAllListeners();
      this.syncSub?.unsubscribe();
      this.heartbeatSub?.unsubscribe();
    }
    
    this.attemptId = attemptId;
    this.isMonitoring = true;
    this.forceSubmitInProgress = false;
    
    // Full state reset for new session
    this._strikeScore.set(initialStrikeScore);
    this._strikeTwoAcknowledged.set(initialStrikeScore >= 2.0);
    this._strikeOneAcknowledged.set(initialStrikeScore >= 1.0);
    this.violationBuffer = [];
    this.mousePosBuffer = [];
    this.pendingViolations = [];
    clearTimeout(this.aggregatorTimeout);
    this.aggregatorTimeout = null;
    this.lastGlobalViolationTime = 0;
    this.monitoringStartTime = Date.now();
    this.originalWidth = window.innerWidth;
    this.originalHeight = window.innerHeight;

    // Attach all listeners inside Angular zone so Angular tracks them
    // BUT use ngZone.runOutsideAngular only for high-frequency events (mouse/resize)
    document.addEventListener('keydown', this.boundHandleKeyDown, { capture: true });
    document.addEventListener('keyup', this.boundHandleKeyUp, { capture: true });
    document.addEventListener('contextmenu', this.boundHandleContextMenu, { capture: true });
    window.addEventListener('beforeunload', this.boundHandleBeforeUnload);
    document.addEventListener('fullscreenchange', this.boundHandleFullscreenChange);
    document.addEventListener('visibilitychange', this.boundHandleVisibilityChange);
    window.addEventListener('blur', this.boundHandleBlur);
    window.addEventListener('online', this.boundHandleOnline);
    window.addEventListener('offline', this.boundHandleOffline);

    this.ngZone.runOutsideAngular(() => {
      window.addEventListener('resize', this.boundHandleResize);
      document.addEventListener('mouseleave', this.boundHandleMouseLeave);
      document.addEventListener('mousemove', this.boundHandleMouseMove);
    });

    // Start periodic sync (every 5 seconds)
    this.syncSub = interval(5000).subscribe(() => this.syncViolations());

    // Start heartbeat (every 30 seconds)
    this.sendHeartbeat(true); // Initial heartbeat as "page refresh" or "start"
    this.heartbeatSub = interval(30000).subscribe(() => {
      // Don't send heartbeat if offline
      if (navigator.onLine) {
        this.sendHeartbeat(false);
      }
    });
  }

  public resumeMonitoring(attemptId: string): void {
    this.startMonitoring(attemptId);
  }

  public stopMonitoring(wasAutoSubmit: boolean = false): void {
    if (!this.isMonitoring) return;
    this.isMonitoring = false;

    // When wasAutoSubmit=true: keep _strikeScore at 3 so the red "Exam Terminated"
    // overlay stays visible until the user dismisses the SweetAlert.
    // When normal stop: reset everything immediately for a clean next session.
    if (!wasAutoSubmit) {
      this._strikeScore.set(0);
    }
    this._strikeTwoAcknowledged.set(false);
    this._strikeOneAcknowledged.set(false);
    this.forceSubmitInProgress = false;
    this.mousePosBuffer = [];
    this.pendingViolations = [];
    clearTimeout(this.aggregatorTimeout);
    this.aggregatorTimeout = null;

    this.removeAllListeners();
    this.syncSub?.unsubscribe();
    this.heartbeatSub?.unsubscribe();

    // Mark last violation as auto-submit trigger if applicable
    // (This is mostly handled automatically in processViolation now)
    if (wasAutoSubmit && this.violationBuffer.length > 0) {
      this.violationBuffer[this.violationBuffer.length - 1].wasAutoSubmit = true;
    }

    // Final sync - use beacon for reliability
    this.syncViolationsBeacon();
    this.attemptId = null;
  }

  private removeAllListeners(): void {
    document.removeEventListener('keydown', this.boundHandleKeyDown, { capture: true });
    document.removeEventListener('keyup', this.boundHandleKeyUp, { capture: true });
    document.removeEventListener('contextmenu', this.boundHandleContextMenu, { capture: true });
    window.removeEventListener('resize', this.boundHandleResize);
    document.removeEventListener('mouseleave', this.boundHandleMouseLeave);
    document.removeEventListener('mousemove', this.boundHandleMouseMove);
    window.removeEventListener('beforeunload', this.boundHandleBeforeUnload);
    document.removeEventListener('fullscreenchange', this.boundHandleFullscreenChange);
    document.removeEventListener('visibilitychange', this.boundHandleVisibilityChange);
    window.removeEventListener('blur', this.boundHandleBlur);
    window.removeEventListener('online', this.boundHandleOnline);
    window.removeEventListener('offline', this.boundHandleOffline);
  }

  private handleFullscreenChange(event: Event): void {
    if (!document.fullscreenElement && this.isMonitoring && !this.forceSubmitInProgress) {
      this.violations$.next({
        type: 'FullScreenExit',
        severity: 'Critical',
        description: 'Student exited Full Screen mode.',
        timestamp: new Date()
      });
    }
  }


  public markForceSubmitInProgress(): void {
    this.forceSubmitInProgress = true;
  }

  public isForceSubmitInProgress(): boolean {
    return this.forceSubmitInProgress;
  }

  // ── Processing ────────────────────────────────────────────────────────────

  private processViolation(event: ViolationEvent): void {
    // If we're inside the 3-second global cooldown, simply ignore
    if (Date.now() - this.lastGlobalViolationTime < 3000) return;

    this.pendingViolations.push(event);

    if (!this.aggregatorTimeout) {
      this.aggregatorTimeout = setTimeout(() => {
        this.flushPendingViolations();
      }, 500); // 500ms aggregation window
    }
  }

  private flushPendingViolations(): void {
    this.aggregatorTimeout = null;
    if (this.pendingViolations.length === 0) return;

    // Pick the most severe violation in the batch
    let maxEvent = this.pendingViolations[0];
    let maxWeight = this.getSeverityWeight(maxEvent.severity);

    for (const ev of this.pendingViolations) {
      const w = this.getSeverityWeight(ev.severity);
      if (w > maxWeight) {
        maxWeight = w;
        maxEvent = ev;
      }
    }

    this.pendingViolations = [];
    
    // Now start the strict 3-second cooldown
    this.lastGlobalViolationTime = Date.now();

    // Accumulate Score inside Angular zone so signals/computed update
    let isTerminating = false;
    this.ngZone.run(() => {
      this._strikeScore.update(s => {
        if (s + maxWeight >= 3.0) isTerminating = true;
        return s + maxWeight;
      });
    });

    // Broadcast valid violation for UI after score is updated
    this.violationOccurred$.next(maxEvent);

    // Buffer for API
    if (this.attemptId) {
      this.violationBuffer.push({
        attemptId: this.attemptId,
        type: maxEvent.type,
        severity: maxEvent.severity,
        description: maxEvent.description,
        occurredAt: maxEvent.timestamp.toISOString(),
        wasAutoSubmit: isTerminating
      });
      
      // For Critical/Medium violations: immediately fire via raw fetch
      if (maxEvent.severity === 'Critical' || maxEvent.severity === 'Medium') {
        this.syncViolationsImmediate();
      }
    }
  }

  private getSeverityWeight(severity: ViolationSeverity): number {
    switch (severity) {
      case 'Minor': return 0.5;
      case 'Medium': return 1.0;
      case 'Critical': return 1.0;
      default: return 1.0;
    }
  }

  // ── Sync ──────────────────────────────────────────────────────────────────

  private syncViolations(): void {
    if (this.violationBuffer.length === 0) return;
    const payload = { violations: [...this.violationBuffer] };
    this.violationBuffer = [];

    this.violationService.logViolationBatch(payload).subscribe({
      next: () => {},
      error: () => {
        console.error('Failed to sync violations, re-queuing...');
        this.violationBuffer.push(...payload.violations);
      }
    });
  }

  // Fire via raw fetch immediately - avoids HttpClient zone delays for Critical events
  private syncViolationsImmediate(): void {
    if (this.violationBuffer.length === 0) return;
    const payload = { violations: [...this.violationBuffer] };
    this.violationBuffer = [];

    const url = `${environment.apiUrl}/violations/batch`;
    const token = localStorage.getItem('shieldon_access_token');
    if (!token) return;

    fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(payload)
    }).catch(() => {
      // On failure, re-queue
      this.violationBuffer.push(...payload.violations);
    });
  }

  // Use sendBeacon for page-unload reliability
  private syncViolationsBeacon(): void {
    if (this.violationBuffer.length === 0) return;
    const url = `${environment.apiUrl}/violations/batch`;
    const token = localStorage.getItem('shieldon_access_token');
    if (!token) return;

    const payload = { violations: [...this.violationBuffer] };
    this.violationBuffer = [];

    fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(payload),
      keepalive: true  // Works even after page unload
    }).catch(() => {});
  }

  private handleBeforeUnload(e: BeforeUnloadEvent): void {
    if (this.isMonitoring) {
      // Attempting to close/refresh tab is a critical violation
      this.violations$.next({
        type: 'TabSwitch',
        severity: 'Critical',
        description: 'Attempted to refresh or close the exam tab.',
        timestamp: new Date()
      });

      e.preventDefault();
      e.returnValue = ''; // Required for Chrome - shows native "Leave site?" dialog
    }

    this.syncViolationsBeacon();
  }

  // ── Event Handlers ────────────────────────────────────────────────────────



  private handleKeyDown(e: KeyboardEvent): void {
    if (!this.isMonitoring) return;

    let blocked = false;
    let desc = '';

    if (e.ctrlKey && (e.key === 'c' || e.key === 'C')) { blocked = true; desc = 'Attempted to Copy'; }
    if (e.ctrlKey && (e.key === 'v' || e.key === 'V')) { blocked = true; desc = 'Attempted to Paste'; }
    if (e.ctrlKey && (e.key === 'x' || e.key === 'X')) { blocked = true; desc = 'Attempted to Cut'; }
    if (e.ctrlKey && (e.key === 'a' || e.key === 'A')) { blocked = true; desc = 'Attempted to Select All'; }
    if (e.key === 'F12') { blocked = true; desc = 'Attempted to open Developer Tools'; }
    if (e.ctrlKey && e.shiftKey && (e.key === 'i' || e.key === 'I')) { blocked = true; desc = 'Attempted to open Developer Tools'; }
    if (e.ctrlKey && e.shiftKey && (e.key === 'j' || e.key === 'J')) { blocked = true; desc = 'Attempted to open Developer Console'; }
    if (e.ctrlKey && (e.key === 'u' || e.key === 'U')) { blocked = true; desc = 'Attempted to View Source'; }
    if (e.ctrlKey && (e.key === 'p' || e.key === 'P')) { blocked = true; desc = 'Attempted to Print'; }
    if (e.ctrlKey && (e.key === 'f' || e.key === 'F')) { blocked = true; desc = 'Attempted to Find'; }
    if (e.ctrlKey && (e.key === 'g' || e.key === 'G')) { blocked = true; desc = 'Attempted to Find Next'; }
    if (e.key === 'Escape') { blocked = true; desc = 'Attempted to exit fullscreen via Esc'; }
    if (e.altKey && e.key === 'Tab') { blocked = true; desc = 'Attempted Alt+Tab'; }
    if (e.key === 'PrintScreen') { blocked = true; desc = 'Attempted to take a Screenshot (PrintScreen)'; }
    if (e.metaKey && e.shiftKey && (e.key === 's' || e.key === 'S')) { blocked = true; desc = 'Attempted to take a Screenshot (Win+Shift+S)'; }
    if (e.metaKey && e.shiftKey && (e.key === '3' || e.key === '4' || e.key === '5')) { blocked = true; desc = 'Attempted to take a Screenshot (Mac shortcut)'; }

    if (blocked) {
      e.preventDefault();
      e.stopPropagation();
      
      if (desc.includes('Screenshot')) {
        navigator.clipboard?.writeText('').catch(() => {});
      }

      this.violations$.next({
        type: 'RestrictedShortcut',
        severity: 'Medium',
        description: desc,
        timestamp: new Date()
      });
    }
  }

  private handleKeyUp(e: KeyboardEvent): void {
    if (!this.isMonitoring) return;
    
    // Windows often fires keyup for PrintScreen instead of keydown
    if (e.key === 'PrintScreen') {
      e.preventDefault();
      e.stopPropagation();
      navigator.clipboard?.writeText('').catch(() => {});
      this.violations$.next({
        type: 'RestrictedShortcut',
        severity: 'Medium',
        description: 'Attempted to take a Screenshot (PrintScreen)',
        timestamp: new Date()
      });
    }
  }

  private handleContextMenu(e: MouseEvent): void {
    if (!this.isMonitoring) return;
    e.preventDefault();
    this.violations$.next({
      type: 'ClipboardPaste',
      severity: 'Medium',
      description: 'Right-click context menu blocked.',
      timestamp: new Date()
    });
  }


  private handleVisibilityChange(): void {
    if (!this.isMonitoring) return;
    if (document.visibilityState === 'hidden') {
      this.violations$.next({
        type: 'TabSwitch',
        severity: 'Medium',
        description: 'Switched to another browser tab or minimized the window.',
        timestamp: new Date()
      });
    }
  }

  private handleBlur(): void {
    if (!this.isMonitoring) return;
    this.violations$.next({
      type: 'FocusLoss',
      severity: 'Minor',
      description: 'Lost focus on the exam window.',
      timestamp: new Date()
    });
  }

  private handleOnline(): void {
    if (!this.isMonitoring) return;
    // Immediately send heartbeat to log "Reconnected" event faster
    this.sendHeartbeat(false);
  }

  private handleOffline(): void {
    if (!this.isMonitoring) return;
    // Could optionally trigger a local warning here
  }

  private sendHeartbeat(isPageRefresh: boolean): void {
    if (!this.attemptId) return;
    this.http.post(`${environment.apiUrl}/exam-attempts/${this.attemptId}/heartbeat`, { isPageRefresh })
      .subscribe({
        error: () => console.warn('Failed to send heartbeat')
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
          description: 'Window resized.',
          timestamp: new Date()
        });
      }
    }, 500);
  }

  private handleMouseLeave(e: MouseEvent): void {
    if (!this.isMonitoring) return;
    if (Date.now() - this.monitoringStartTime < 2000) return; // 2s grace period at start
    
    if (e.clientY <= 0 || e.clientX <= 0 || e.clientX >= window.innerWidth || e.clientY >= window.innerHeight) {
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
    if (Date.now() - this.monitoringStartTime < 2000) return; // 2s grace period at start
    
    const now = Date.now();
    if (this.mousePosBuffer.length > 0) {
      const last = this.mousePosBuffer[this.mousePosBuffer.length - 1];
      if (now - last.time < 200) return;
    }
    this.mousePosBuffer.push({ x: e.clientX, y: e.clientY, time: now });
    if (this.mousePosBuffer.length > 15) {
      this.mousePosBuffer.shift();
      let totalDist = 0;
      for (let i = 1; i < this.mousePosBuffer.length; i++) {
        const p1 = this.mousePosBuffer[i - 1];
        const p2 = this.mousePosBuffer[i];
        totalDist += Math.sqrt(Math.pow(p2.x - p1.x, 2) + Math.pow(p2.y - p1.y, 2));
      }
      const timeDiff = (this.mousePosBuffer[this.mousePosBuffer.length - 1].time - this.mousePosBuffer[0].time) / 1000;
      const velocity = totalDist / timeDiff;
      if (velocity > 3000) {
        this.violations$.next({
          type: 'AbnormalMouseActivity',
          severity: 'Minor',
          description: 'Erratic/abnormal rapid mouse movements detected.',
          timestamp: new Date()
        });
        this.mousePosBuffer = [];
      }
    }
  }
}
