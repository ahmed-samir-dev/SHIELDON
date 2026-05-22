import { Component, OnInit, inject, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ExamResultService, ExamResultResponse, ExamAttemptSummaryDto } from '../services/exam-result';
import { ToastrService } from 'ngx-toastr';
import { LucideAngularModule, CheckCircle, XCircle, ArrowLeft, Clock, FileText, Check, X, AlertCircle } from 'lucide-angular';
import confetti from 'canvas-confetti';
import Swal from 'sweetalert2';
import { ReattemptService, StudentReattemptStatusResponse } from '../services/reattempt.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-exam-result-page',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule, TranslateModule],
  templateUrl: './exam-result-page.html',
  styleUrl: './exam-result-page.scss'
})
export class ExamResultPage implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private examResultService = inject(ExamResultService);
  private reattemptService = inject(ReattemptService);
  private toastr = inject(ToastrService);
  private translate = inject(TranslateService);

  // Icons
  readonly CheckCircle = CheckCircle;
  readonly XCircle = XCircle;
  readonly ArrowLeft = ArrowLeft;
  readonly Clock = Clock;
  readonly FileText = FileText;
  readonly Check = Check;
  readonly X = X;
  readonly AlertCircle = AlertCircle;

  isLoading = signal(true);
  result = signal<ExamResultResponse | null>(null);
  studentAttempts = signal<ExamAttemptSummaryDto[]>([]);
  showAttemptPicker = signal(false);
  
  existingReattemptRequest = signal<StudentReattemptStatusResponse | null>(null);
  
  // Circle animation state
  circumference = 2 * Math.PI * 70; // r=70
  dashOffset = signal(this.circumference);
  
  private animationTimeout: any;

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const attemptId = params.get('attemptId');
      if (!attemptId) {
        this.toastr.error(this.translate.instant('EXAM_RESULT_PAGE.TOAST_ERR_INVALID_ID'));
        this.router.navigate(['/courses']);
        return;
      }
      this.loadResult(attemptId);
    });
  }

  ngOnDestroy() {
    if (this.animationTimeout) clearTimeout(this.animationTimeout);
  }

  loadResult(attemptId: string) {
    this.isLoading.set(true);
    this.dashOffset.set(this.circumference); // Reset circle animation
    
    this.examResultService.getAttemptResult(attemptId).subscribe({
      next: (res) => {
        this.result.set(res.data);
        this.isLoading.set(false);
        
        if (res.data.resultVisible && res.data.score !== null) {
          this.animateScore(res.data.score);
          if (res.data.passed) {
            this.triggerConfetti();
          }
        }
        
        // Fetch all attempts for this student for this exam
        this.examResultService.getStudentAttempts(res.data.examId).subscribe({
          next: (attemptsRes) => this.studentAttempts.set(attemptsRes.data),
          error: () => console.error('Failed to load student attempts')
        });

        // Fetch existing re-attempt requests for this exam
        this.loadExistingReattemptRequest(res.data.examId);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || this.translate.instant('EXAM_RESULT_PAGE.TOAST_ERR_LOAD'));
        this.isLoading.set(false);
      }
    });
  }

  loadExistingReattemptRequest(examId: string) {
    this.reattemptService.getMyRequests().subscribe({
      next: (res) => {
        const req = res.data.find(r => r.examId === examId);
        this.existingReattemptRequest.set(req || null);
      },
      error: () => console.error('Failed to load re-attempt requests')
    });
  }

  animateScore(score: number) {
    // Slight delay to allow DOM to render
    this.animationTimeout = setTimeout(() => {
      const offset = this.circumference - (score / 100) * this.circumference;
      this.dashOffset.set(offset);
    }, 100);
  }

  triggerConfetti() {
    const count = 200;
    const defaults = {
      origin: { y: 0.7 }
    };

    function fire(particleRatio: number, opts: any) {
      confetti({
        ...defaults,
        ...opts,
        particleCount: Math.floor(count * particleRatio)
      });
    }

    fire(0.25, {
      spread: 26,
      startVelocity: 55,
    });
    fire(0.2, {
      spread: 60,
    });
    fire(0.35, {
      spread: 100,
      decay: 0.91,
      scalar: 0.8
    });
    fire(0.1, {
      spread: 120,
      startVelocity: 25,
      decay: 0.92,
      scalar: 1.2
    });
    fire(0.1, {
      spread: 120,
      startVelocity: 45,
    });
  }

  getScoreColorClass(): string {
    const score = this.result()?.score ?? 0;
    const passScore = this.result()?.passScore ?? 50;
    return score >= passScore ? 'score-pass' : 'score-fail';
  }

  goBack() {
    const courseId = this.result()?.courseId; 
    if (courseId) {
      this.router.navigate(['/courses', courseId], { queryParams: { tab: 'exams' } });
    } else {
      this.router.navigate(['/courses']);
    }
  }

  viewAttempt(attemptId: string) {
    this.showAttemptPicker.set(false);
    this.router.navigate(['/exam-results', attemptId]);
  }

  openAttemptPicker() {
    this.showAttemptPicker.set(true);
  }

  closeAttemptPicker() {
    this.showAttemptPicker.set(false);
  }

  requestReattempt() {
    const examId = this.result()?.examId;
    if (!examId) return;

    Swal.fire({
      title: this.translate.instant('EXAM_RESULT_PAGE.SWAL_REQ_TITLE'),
      text: this.translate.instant('EXAM_RESULT_PAGE.SWAL_REQ_DESC'),
      input: 'textarea',
      inputPlaceholder: this.translate.instant('EXAM_RESULT_PAGE.SWAL_PLACEHOLDER'),
      inputAttributes: {
        'aria-label': 'Justification for re-attempt'
      },
      showCancelButton: true,
      confirmButtonText: this.translate.instant('EXAM_RESULT_PAGE.SWAL_BTN_SUBMIT'),
      cancelButtonText: this.translate.instant('EXAM_RESULT_PAGE.SWAL_BTN_CANCEL'),
      confirmButtonColor: '#215DAE',
      cancelButtonColor: '#87949C',
      inputValidator: (value) => {
        if (!value || value.trim().length < 20) {
          return this.translate.instant('EXAM_RESULT_PAGE.SWAL_VAL_ERR');
        }
        return null;
      }
    }).then((swalResult) => {
      if (swalResult.isConfirmed && swalResult.value) {
        this.reattemptService.submitRequest(examId, { justification: swalResult.value }).subscribe({
          next: (res) => {
            this.toastr.success(res.message);
            this.existingReattemptRequest.set(res.data);
          },
          error: (err) => {
            this.toastr.error(err.error?.message || this.translate.instant('EXAM_RESULT_PAGE.TOAST_REQ_ERR'));
          }
        });
      }
    });
  }
}
