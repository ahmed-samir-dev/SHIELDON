import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { LucideAngularModule, User, ShieldAlert, CheckCircle, RotateCcw, AlertTriangle, Activity, X } from 'lucide-angular';
import Swal from 'sweetalert2';
import { ToastrService } from 'ngx-toastr';
import { SessionTimelineComponent } from '../session-timeline/session-timeline';
import { ViolationTimelineComponent } from '../violation-timeline/violation-timeline';
import { MonitoringService } from '../../../core/services/monitoring.service';
import { ExamResultService } from '../../exams/services/exam-result';

@Component({
  selector: 'app-manual-review',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, SessionTimelineComponent, ViolationTimelineComponent],
  templateUrl: './manual-review.html',
  styleUrls: ['./manual-review.scss']
})
export class ManualReviewComponent implements OnInit {
  @Input() attemptId!: string;

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private monitoring = inject(MonitoringService);
  private resultService = inject(ExamResultService);
  private toastr = inject(ToastrService);

  // Icons
  User = User;
  ShieldAlert = ShieldAlert;
  CheckCircle = CheckCircle;
  RotateCcw = RotateCcw;
  AlertTriangle = AlertTriangle;
  Activity = Activity;
  X = X;

  // State
  loading = signal<boolean>(true);
  error = signal<string>('');
  attemptInfo = signal<any>(null); // from ExamResultService
  
  // Local state flag to see if session is active
  sessionStatus = signal<string>('Unknown');
  hasReviewDecision = signal<boolean>(false);

  ngOnInit() {
    // If not bound via input, get from route
    if (!this.attemptId) {
      this.attemptId = this.route.snapshot.paramMap.get('attemptId') || '';
    }

    if (!this.attemptId) {
      this.error.set('Attempt ID is missing.');
      this.loading.set(false);
      return;
    }

    this.loadAttemptInfo();
  }

  loadAttemptInfo() {
    this.loading.set(true);
    // Use the existing exam result service to get student and attempt basic info
    this.resultService.getAttemptResult(this.attemptId).subscribe({
      next: (res) => {
        this.attemptInfo.set(res.data);
        this.sessionStatus.set(res.data?.status || 'Unknown');
        // Check if there's a review decision already
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load attempt details.');
        this.loading.set(false);
      }
    });
  }

  submitDecision(decision: 'Accepted' | 'MarkedAsCheating' | 'ReAttemptGranted') {
    let title = '';
    let text = '';
    let icon: 'success' | 'warning' | 'error' = 'warning';

    if (decision === 'Accepted') {
      title = 'Accept Attempt?';
      text = 'The score will stand as is, and the integrity warnings will be cleared.';
      icon = 'success';
    } else if (decision === 'MarkedAsCheating') {
      title = 'Mark as Cheating?';
      text = "The student's grade for this exam will be immediately changed to zero (0). This action cannot be undone.";
      icon = 'error';
    } else if (decision === 'ReAttemptGranted') {
      title = 'Grant Re-Attempt?';
      text = 'This will automatically create and approve a re-attempt request for the student.';
      icon = 'warning';
    }

    Swal.fire({
      title,
      text,
      icon,
      input: 'textarea',
      inputPlaceholder: 'Add optional notes for this decision...',
      showCancelButton: true,
      confirmButtonText: 'Yes, confirm decision',
      cancelButtonText: 'Cancel',
      confirmButtonColor: decision === 'MarkedAsCheating' ? '#ef4444' : '#4f46e5'
    }).then((result) => {
      if (result.isConfirmed) {
        const notes = result.value || undefined;
        this.executeDecision(decision, notes);
      }
    });
  }

  private executeDecision(decision: string, notes?: string) {
    this.monitoring.submitReviewDecision(this.attemptId, { decision, notes }).subscribe({
      next: () => {
        this.toastr.success(`Decision '${decision}' saved successfully.`);
        this.hasReviewDecision.set(true);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to submit decision.');
      }
    });
  }

  terminateSession() {
    Swal.fire({
      title: 'Terminate Session?',
      text: "This will immediately force-submit the student's exam. They will not be able to continue.",
      icon: 'warning',
      input: 'text',
      inputPlaceholder: 'Reason for termination (optional)',
      showCancelButton: true,
      confirmButtonText: 'Yes, Terminate',
      confirmButtonColor: '#ef4444'
    }).then((result) => {
      if (result.isConfirmed) {
        this.monitoring.terminateSession(this.attemptId, { reason: result.value || undefined }).subscribe({
          next: () => {
            this.toastr.success('Session terminated successfully.');
            this.sessionStatus.set('ForceSubmitted');
            // Reload components (the timeline handles itself when re-initialized but we can just reload the window or component state)
            this.loadAttemptInfo();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to terminate session.');
          }
        });
      }
    });
  }
}
