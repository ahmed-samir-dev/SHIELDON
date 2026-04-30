import { Component, OnInit, inject, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ExamResultService, ExamResultResponse, ExamAttemptSummaryDto } from '../services/exam-result';
import { ToastrService } from 'ngx-toastr';
import { LucideAngularModule, CheckCircle, XCircle, ArrowLeft, Clock, FileText, Check, X, AlertCircle } from 'lucide-angular';
import confetti from 'canvas-confetti';

@Component({
  selector: 'app-exam-result-page',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule],
  templateUrl: './exam-result-page.html',
  styleUrl: './exam-result-page.scss'
})
export class ExamResultPage implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private examResultService = inject(ExamResultService);
  private toastr = inject(ToastrService);

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
  
  // Circle animation state
  circumference = 2 * Math.PI * 70; // r=70
  dashOffset = signal(this.circumference);
  
  private animationTimeout: any;

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const attemptId = params.get('attemptId');
      if (!attemptId) {
        this.toastr.error('Invalid attempt ID');
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
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to load exam result');
        this.isLoading.set(false);
      }
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
    // Stage 3.7 implementation goes here
    this.toastr.info('Re-attempt requests will be available in the next update.', 'Coming Soon');
  }
}
