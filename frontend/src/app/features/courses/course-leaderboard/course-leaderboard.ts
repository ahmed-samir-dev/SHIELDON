import {
  Component, OnInit, OnDestroy, inject, signal, computed, Input,
  ViewChild, ElementRef, Renderer2
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import {
  LucideAngularModule,
  Trophy, Star, RefreshCw, Settings2, EyeOff,
  TrendingUp, TrendingDown, Minus, Sparkles, Crown, Award,
  BarChart3, X, Check, Lock
} from 'lucide-angular';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ToastrService } from 'ngx-toastr';
import confetti from 'canvas-confetti';
import { environment } from '../../../../environments/environment';
import { DOCUMENT } from '@angular/common';

import { LeaderboardService } from '../../../core/services/leaderboard.service';
import { AuthService } from '../../../core/services/auth.service';
import { LeaderboardEntry, LeaderboardResponse, LeaderboardSettings } from '../../../core/models/leaderboard.model';

@Component({
  selector: 'app-course-leaderboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule, TranslateModule],
  templateUrl: './course-leaderboard.html',
  styleUrl: './course-leaderboard.scss'
})
export class CourseLeaderboardComponent implements OnInit, OnDestroy {
  @Input({ required: true }) courseId!: string;

  private lbService = inject(LeaderboardService);
  private authService = inject(AuthService);
  private toastr = inject(ToastrService);
  private translate = inject(TranslateService);
  private fb = inject(FormBuilder);
  private renderer = inject(Renderer2);
  private document = inject(DOCUMENT);

  /** Reference to the backdrop DOM element so we can move it to document.body. */
  @ViewChild('modalBackdrop') modalBackdropRef!: ElementRef<HTMLElement>;

  // ── Icons ──────────────────────────────────────────────────────────────────
  readonly Trophy = Trophy;
  /** Rank-2 icon: Star is used instead of Medal (Medal SVG looks like a #1 medal visually) */
  readonly Star = Star;
  readonly RefreshCw = RefreshCw;
  readonly Settings2 = Settings2;
  readonly EyeOff = EyeOff;
  readonly TrendingUp = TrendingUp;
  readonly TrendingDown = TrendingDown;
  readonly Minus = Minus;
  readonly Sparkles = Sparkles;
  readonly Crown = Crown;
  /** Rank-3 icon */
  readonly Award = Award;
  readonly BarChart3 = BarChart3;
  readonly X = X;
  readonly Check = Check;
  readonly Lock = Lock;

  /** Base URL without /api - used to prefix relative avatar URLs from the backend. */
  private readonly apiBase = environment.apiUrl.replace('/api', '');

  // ── Reactive State ─────────────────────────────────────────────────────────
  isLoading = signal(true);
  leaderboard = signal<LeaderboardResponse | null>(null);
  settings = signal<LeaderboardSettings | null>(null);
  isRefreshing = signal(false);
  showSettingsModal = signal(false);
  isSavingSettings = signal(false);
  confettiFired = signal(false);

  // Derived
  isStudent = computed(() => this.authService.isStudent());
  isInstructor = computed(() => !this.authService.isStudent());
  podiumTop3 = computed(() => {
    const entries = this.leaderboard()?.topEntries ?? [];
    return {
      first: entries[0] ?? null,
      second: entries[1] ?? null,
      third: entries[2] ?? null
    };
  });
  hasData = computed(() => (this.leaderboard()?.topEntries?.length ?? 0) > 0);

  // Settings form
  settingsForm = this.fb.group({
    isLeaderboardVisible: [true],
    showStudentOwnRank: [true],
    scoringMetric: ['TotalScore', Validators.required],
  });

  private signalRSub: Subscription | null = null;

  // ── Lifecycle ───────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadLeaderboard();
    if (this.isInstructor()) {
      this.loadSettings();
    }
    this.connectSignalR();
  }

  ngOnDestroy(): void {
    this.signalRSub?.unsubscribe();
    this.lbService.stopConnection(this.courseId);
  }

  // ── Data Loading ───────────────────────────────────────────────────────────

  loadLeaderboard(): void {
    this.isLoading.set(true);
    this.lbService.getLeaderboard(this.courseId).subscribe({
      next: (data) => {
        this.leaderboard.set(data);
        this.isLoading.set(false);
        this.maybeFireConfetti(data);
      },
      error: (err) => {
        if (err.status === 403 && this.isStudent()) {
          this.leaderboard.set({
            courseId: this.courseId,
            courseTitle: '',
            scoringMetric: 'TotalScore',
            isLeaderboardVisible: false,
            showStudentOwnRank: false,
            topEntries: [],
            studentOwnRank: null,
            generatedAt: new Date().toISOString()
          });
        } else {
          this.toastr.error(this.translate.instant('LEADERBOARD.TOAST_LOAD_ERR'));
        }
        this.isLoading.set(false);
      }
    });
  }

  loadSettings(): void {
    this.lbService.getSettings(this.courseId).subscribe({
      next: (s) => {
        this.settings.set(s);
        this.settingsForm.patchValue({
          isLeaderboardVisible: s.isLeaderboardVisible,
          showStudentOwnRank: s.showStudentOwnRank,
          scoringMetric: s.scoringMetric,
        });
      },
      error: () => {
        // Settings load failure is non-critical - silently ignore
      }
    });
  }

  // ── SignalR ────────────────────────────────────────────────────────────────

  private connectSignalR(): void {
    const token = this.authService.getAccessToken();
    if (!token) return;

    this.lbService.startConnection(this.courseId, token).catch(() => {
      // Connection failure is non-blocking - REST data still works
    });

    this.signalRSub = this.lbService.leaderboardUpdated$.subscribe((payload) => {
      this.leaderboard.set(payload);
      this.maybeFireConfetti(payload);
    });
  }

  // ── Actions ────────────────────────────────────────────────────────────────

  refreshLeaderboard(): void {
    this.isRefreshing.set(true);
    this.lbService.refreshLeaderboard(this.courseId).subscribe({
      next: () => {
        this.toastr.success(this.translate.instant('LEADERBOARD.TOAST_REFRESHED'));
        this.loadLeaderboard();
        this.isRefreshing.set(false);
      },
      error: () => {
        this.toastr.error(this.translate.instant('LEADERBOARD.TOAST_REFRESH_ERR'));
        this.isRefreshing.set(false);
      }
    });
  }

  openSettingsModal(): void {
    this.showSettingsModal.set(true);
    // Move the backdrop to document.body AFTER Angular renders it,
    // then add .modal-attached to smoothly fade in without any flash.
    setTimeout(() => {
      const el = this.modalBackdropRef?.nativeElement;
      if (el) {
        this.renderer.appendChild(this.document.body, el);
        // Double rAF ensures DOM reflow is complete before triggering transition
        requestAnimationFrame(() => {
          this.renderer.addClass(el, 'modal-attached');
        });
      }
    }, 0);
  }

  closeSettingsModal(): void {
    const el = this.modalBackdropRef?.nativeElement;
    if (el) {
      this.renderer.removeClass(el, 'modal-attached');
    }
    // Give animation 150ms to fade out before destroying from Angular DOM
    setTimeout(() => {
      if (el && el.parentElement === this.document.body) {
        this.renderer.removeChild(this.document.body, el);
      }
      this.showSettingsModal.set(false);
    }, 150);
  }

  saveSettings(): void {
    if (this.settingsForm.invalid) return;
    this.isSavingSettings.set(true);
    const formValue = this.settingsForm.value;
    this.lbService.updateSettings(this.courseId, {
      isLeaderboardVisible: formValue.isLeaderboardVisible ?? true,
      showStudentOwnRank: formValue.showStudentOwnRank ?? true,
      scoringMetric: formValue.scoringMetric ?? 'TotalScore',
    }).subscribe({
      next: (updated) => {
        this.settings.set(updated);
        this.isSavingSettings.set(false);
        this.closeSettingsModal();
        this.toastr.success(this.translate.instant('LEADERBOARD.TOAST_SETTINGS_SAVED'));
        this.loadLeaderboard();
      },
      error: () => {
        this.toastr.error(this.translate.instant('LEADERBOARD.TOAST_SETTINGS_ERR'));
        this.isSavingSettings.set(false);
      }
    });
  }

  // ── Helpers ──────────────────────────────────────────────────────────────────

  isTied(entry: LeaderboardEntry, topEntries: LeaderboardEntry[]): boolean {
    if (!entry || !topEntries) return false;
    return topEntries.filter(e => e.rank === entry.rank).length > 1;
  }

  /**
   * Resolves a possibly-relative avatar URL from the backend to an absolute URL.
   * The backend returns paths like "uploads/profiles/xxx.jpg" (no leading slash),
   * so we prepend the API base URL (e.g., http://localhost:5000).
   */
  getAvatarUrl(url: string | null): string | null {
    if (!url) return null;
    if (url.startsWith('http')) return url;
    return `${this.apiBase}/${url}`;
  }

  getAvatarInitials(name: string): string {
    const parts = name.trim().split(' ');
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    return name.substring(0, 2).toUpperCase();
  }

  getMetricLabel(metric: string): string {
    switch (metric) {
      case 'TotalScore': return this.translate.instant('LEADERBOARD.METRIC_TOTAL');
      case 'ExamAverage': return this.translate.instant('LEADERBOARD.METRIC_EXAM');
      case 'AssignmentAverage': return this.translate.instant('LEADERBOARD.METRIC_ASSIGNMENT');
      default: return metric;
    }
  }

  isCurrentUser(studentId: string): boolean {
    return this.authService.currentUser()?.userId === studentId;
  }

  trackByRank(_: number, entry: LeaderboardEntry): string {
    return entry.studentId;
  }

  private maybeFireConfetti(data: LeaderboardResponse): void {
    if (this.confettiFired()) return;
    if (!data.isLeaderboardVisible && this.isStudent()) return;
    if (!data.topEntries?.length) return;
    this.confettiFired.set(true);
    this.fireConfetti();
  }

  private fireConfetti(): void {
    const count = 200;
    const defaults = { origin: { y: 0.7 } };

    const fire = (particleRatio: number, opts: object) => {
      confetti({ ...defaults, ...opts, particleCount: Math.floor(count * particleRatio) });
    };

    fire(0.25, { spread: 26, startVelocity: 55 });
    fire(0.2,  { spread: 60 });
    fire(0.35, { spread: 100, decay: 0.91, scalar: 0.8 });
    fire(0.1,  { spread: 120, startVelocity: 25, decay: 0.92, scalar: 1.2 });
    fire(0.1,  { spread: 120, startVelocity: 45 });
  }
}
