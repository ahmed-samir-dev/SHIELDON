import { Component, ElementRef, OnInit, ViewChild, computed, effect, inject, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { AttendanceService } from '../../../core/services/attendance.service';
import { AttendanceCheckDetailDto, EnrolledStudentDto } from '../../../core/models/attendance.model';
import QRCode from 'qrcode';
import { environment } from '../../../../environments/environment';
import { RouterModule } from '@angular/router';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-attendance-tutor',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './attendance-tutor.html',
  styleUrls: ['./attendance-tutor.scss']
})
export class AttendanceTutorComponent implements OnInit, OnDestroy {
  @ViewChild('qrCanvas') qrCanvas!: ElementRef<HTMLCanvasElement>;

  private route = inject(ActivatedRoute);
  private attendanceService = inject(AttendanceService);

  courseId = signal<string>('');
  activeCheck = signal<AttendanceCheckDetailDto | null>(null);
  history = signal<any[]>([]);

  // Real-time tracking
  students = signal<EnrolledStudentDto[]>([]);
  totalEnrolled = computed(() => this.students().length);
  totalPresent = computed(() => this.students().filter(s => s.isPresent).length);

  isLoading = signal<boolean>(false);
  isQrLoading = signal<boolean>(true);
  qrFlash = signal<boolean>(false); // drives CSS flash animation on each QR refresh
  errorMsg = signal<string>('');

  private qrPollInterval: ReturnType<typeof setInterval> | null = null;

  constructor() {
    // Listen to real-time student markings via SignalR
    effect(() => {
      const marking = this.attendanceService.liveRecordUpdates();
      if (marking && this.activeCheck() &&
          marking.checkId.toLowerCase() === this.activeCheck()?.id.toLowerCase()) {
        this.students.update(list => list.map(s => {
          if (s.id.toLowerCase() === marking.studentId.toLowerCase()) {
            return { ...s, isPresent: true, isManual: marking.isManual };
          }
          return s;
        }));
      }
    });
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.courseId.set(id);
        this.loadExistingCheck();
      }
    });
  }

  ngOnDestroy() {
    this.stopQrPolling();
    const check = this.activeCheck();
    if (check?.isActive) {
      this.attendanceService.leaveCheck(check.id);
    }
  }

  // ── Private helpers ──────────────────────────────────────────────────────

  private loadExistingCheck() {
    this.isLoading.set(true);
    this.attendanceService.getCourseHistory(this.courseId()).subscribe({
      next: (res) => {
        const active = res.data.find(c => c.isActive);
        // Store ended checks in history
        this.history.set(res.data.filter(c => !c.isActive));
        
        if (active) {
          this.loadCheckDetails(active.id);
        } else {
          this.isLoading.set(false);
        }
      },
      error: () => this.isLoading.set(false)
    });
  }

  private loadCheckDetails(checkId: string) {
    this.attendanceService.getCheckDetails(checkId).subscribe({
      next: async (res) => {
        this.activeCheck.set(res.data);
        this.students.set(res.data.allEnrolledStudents);
        this.isQrLoading.set(true);

        // SignalR: only used for live student-marking events
        await this.attendanceService.startSignalRConnection();
        await this.attendanceService.joinCheckAsTutor(checkId);

        // Render QR immediately via REST — no waiting for the 5s timer
        this.fetchAndRenderQr(checkId);

        // Poll every 5s to stay in sync with the backend's secret rotation
        this.startQrPolling(checkId);

        this.isLoading.set(false);
      },
      error: () => {
        this.errorMsg.set('Failed to load check details');
        this.isLoading.set(false);
      }
    });
  }

  private fetchAndRenderQr(checkId: string) {
    this.attendanceService.getCurrentQr(checkId).subscribe({
      next: (qrRes) => {
        const { payload, expiresAt } = qrRes.data;
        setTimeout(() => this.renderQrCode(payload), 50);

        // Schedule the NEXT fetch to fire 300ms AFTER the backend rotates.
        // expiresAt is the exact moment the current secret expires and a new one
        // will be written to the DB by AttendanceRotationService.
        this.scheduleNextQrFetch(checkId, expiresAt);
      },
      error: () => {
        console.warn('Could not fetch QR payload — retrying in 2s');
        setTimeout(() => {
          if (this.activeCheck()?.isActive) this.fetchAndRenderQr(checkId);
        }, 2000);
      }
    });
  }

  private scheduleNextQrFetch(checkId: string, expiresAtUtc: string) {
    this.stopQrPolling();

    // Calculate ms until expiry, then add 300ms grace period so the DB is updated
    const expiresMs = new Date(expiresAtUtc).getTime() - Date.now();
    const delay = Math.max(expiresMs + 300, 500); // at least 500ms

    // Use a one-shot timeout then kick off the next cycle
    this.qrPollInterval = setTimeout(() => {
      this.qrPollInterval = null;
      if (this.activeCheck()?.isActive) this.fetchAndRenderQr(checkId);
    }, delay) as unknown as ReturnType<typeof setInterval>;
  }

  private startQrPolling(checkId: string) {
    // Initial fetch is triggered by loadCheckDetails; this is just a safety fallback
    // in case scheduleNextQrFetch never fires (e.g., network error path)
  }

  private stopQrPolling() {
    if (this.qrPollInterval !== null) {
      clearTimeout(this.qrPollInterval as unknown as ReturnType<typeof setTimeout>);
      clearInterval(this.qrPollInterval);
      this.qrPollInterval = null;
    }
  }

  private renderQrCode(payload: string) {
    if (!this.qrCanvas?.nativeElement) return;
    QRCode.toCanvas(this.qrCanvas.nativeElement, payload, {
      width: 350,
      margin: 2,
      color: { dark: '#0f172a', light: '#ffffff' }
    }, (error) => {
      if (error) {
        console.error('QR Generate Error:', error);
      } else {
        this.isQrLoading.set(false);
        // Briefly flash the canvas to make rotation visually obvious
        this.qrFlash.set(true);
        setTimeout(() => this.qrFlash.set(false), 400);
      }
    });
  }

  // ── Public actions ───────────────────────────────────────────────────────

  startCheck() {
    this.isLoading.set(true);
    this.attendanceService.startCheck({ courseId: this.courseId() }).subscribe({
      next: (res) => this.loadCheckDetails(res.data.id),
      error: (err) => {
        this.errorMsg.set(err.error?.message || 'Failed to start check');
        this.isLoading.set(false);
      }
    });
  }

  endCheck() {
    const check = this.activeCheck();
    if (!check) return;

    Swal.fire({
      title: 'End Attendance Session?',
      text: 'Are you sure you want to end this attendance check? The QR will become invalid.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes, End Session',
      cancelButtonText: 'Cancel',
      confirmButtonColor: '#e11d48'
    }).then((result) => {
      if (result.isConfirmed) {
        this.isLoading.set(true);
        this.stopQrPolling();
        this.attendanceService.endCheck(check.id).subscribe({
          next: () => {
            this.activeCheck.set(null);
            this.students.set([]);
            this.isQrLoading.set(true);
            this.attendanceService.leaveCheck(check.id);
            this.loadExistingCheck(); // Load history after ending
            this.isLoading.set(false);
          },
          error: () => {
            this.errorMsg.set('Failed to end check');
            this.isLoading.set(false);
          }
        });
      }
    });
  }

  toggleManualPresence(student: EnrolledStudentDto) {
    const check = this.activeCheck();
    if (!check) return;

    const previousState = student.isPresent;
    this.students.update(list => list.map(s =>
      s.id === student.id ? { ...s, isPresent: !previousState, isManual: !previousState } : s
    ));

    this.attendanceService.manualMark(check.id, student.id).subscribe({
      error: () => {
        this.students.update(list => list.map(s =>
          s.id === student.id ? { ...s, isPresent: previousState, isManual: student.isManual } : s
        ));
        alert('Failed to update student presence manually.');
      }
    });
  }

  getAvatarUrl(url: string | null | undefined): string {
    if (!url) return '';
    if (url.startsWith('http')) return url;
    const apiUrl = environment.apiUrl.replace('/api', '');
    return `${apiUrl}/${url}`;
  }
}
