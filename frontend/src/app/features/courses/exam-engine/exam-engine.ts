import { Component, OnDestroy, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Clock, AlertTriangle, CheckCircle, ChevronRight, ChevronLeft, Save } from 'lucide-angular';
import { ToastrService } from 'ngx-toastr';
import { Subject, Subscription, interval } from 'rxjs';
import { takeUntil, debounceTime } from 'rxjs/operators';
import Swal from 'sweetalert2';

import { ExamService } from '../services/exam.service';
import { ExamDetailResponse } from '../../../core/models/exam.model';
import { ExamAttemptService, StartExamResponse, StudentQuestionDto, QuestionType } from '../services/exam-attempt';

type EngineState = 'loading' | 'rules' | 'active' | 'review' | 'submitting' | 'error';

@Component({
  selector: 'app-exam-engine',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './exam-engine.html',
  styleUrls: ['./exam-engine.scss']
})
export class ExamEngine implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  public router = inject(Router);
  private examService = inject(ExamService);
  private attemptService = inject(ExamAttemptService);
  private toastr = inject(ToastrService);

  // Icons
  Clock = Clock;
  AlertTriangle = AlertTriangle;
  CheckCircle = CheckCircle;
  ChevronLeft = ChevronLeft;
  ChevronRight = ChevronRight;
  Save = Save;

  // State
  state = signal<EngineState>('loading');
  examId = signal<string>('');
  examDetails = signal<ExamDetailResponse | null>(null);
  
  // Active Engine State
  attemptData = signal<StartExamResponse | null>(null);
  currentQuestionIndex = signal<number>(0);
  
  // Maps questionId -> answer text or optionId
  answers = signal<Record<string, string | null>>({});
  savingState = signal<Record<string, boolean>>({});

  // Timer
  timeRemainingSeconds = signal<number>(0);
  private timerSub?: Subscription;
  private destroy$ = new Subject<void>();

  // Auto-Save Debouncer
  private answerChange$ = new Subject<{question: StudentQuestionDto, value: string}>();

  // Computed
  currentQuestion = computed(() => {
    const data = this.attemptData();
    if (!data || !data.questions) return null;
    return data.questions[this.currentQuestionIndex()];
  });

  isAnswered = computed(() => {
    const ans = this.answers();
    return (questionId: string) => !!ans[questionId];
  });

  formattedTime = computed(() => {
    const totalSeconds = this.timeRemainingSeconds();
    if (totalSeconds <= 0) return '00:00';
    const m = Math.floor(totalSeconds / 60);
    const s = totalSeconds % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  });

  timerStatus = computed(() => {
    const totalSeconds = this.timeRemainingSeconds();
    if (totalSeconds < 300) return 'danger'; // < 5 mins
    if (totalSeconds < 600) return 'warning'; // < 10 mins
    return 'normal';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('examId');
    if (!id) {
      this.router.navigate(['/']);
      return;
    }
    this.examId.set(id);
    this.loadExamDetails(id);

    // Setup Debouncer
    this.answerChange$.pipe(
      debounceTime(500),
      takeUntil(this.destroy$)
    ).subscribe(({question, value}) => {
      this.executeSaveAnswer(question, value);
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.timerSub?.unsubscribe();
  }

  private loadExamDetails(id: string): void {
    this.examService.getExamById(id).subscribe({
      next: (res) => {
        this.examDetails.set(res.data);
        this.state.set('rules');
      },
      error: () => {
        this.toastr.error('Could not load exam details');
        this.state.set('error');
      }
    });
  }

  startExam(): void {
    this.state.set('loading');
    this.attemptService.startExam(this.examId()).subscribe({
      next: (res) => {
        this.attemptData.set(res.data);
        this.initializeAnswers(res.data);
        this.startTimer(res.data.expiresAt);
        this.state.set('active');
        this.toastr.success('Exam started! Good luck.');
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to start exam');
        this.state.set('rules');
      }
    });
  }

  private initializeAnswers(data: StartExamResponse): void {
    const initialAnswers: Record<string, string | null> = {};
    
    // Default to null
    data.questions.forEach(q => initialAnswers[q.id] = null);
    
    // Override with saved answers from DB
    if (data.savedAnswers && data.savedAnswers.length > 0) {
      data.savedAnswers.forEach(ans => {
        initialAnswers[ans.questionId] = ans.selectedOptionId || ans.textAnswer || null;
      });
    }
    
    this.answers.set(initialAnswers);
  }

  private startTimer(expiresAtIso: string): void {
    const expiresAt = new Date(expiresAtIso).getTime();
    
    // Initial calculation
    this.updateTimeRemaining(expiresAt);

    this.timerSub = interval(1000).pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.updateTimeRemaining(expiresAt);
    });
  }

  private updateTimeRemaining(expiresAt: number): void {
    const now = new Date().getTime();
    const remainingMs = expiresAt - now;
    
    if (remainingMs <= 0) {
      this.timeRemainingSeconds.set(0);
      this.timerSub?.unsubscribe();
      this.forceSubmit();
    } else {
      this.timeRemainingSeconds.set(Math.floor(remainingMs / 1000));
    }
  }

  // ── Navigation ──

  goToQuestion(index: number): void {
    this.currentQuestionIndex.set(index);
  }

  nextQuestion(): void {
    const current = this.currentQuestionIndex();
    const total = this.attemptData()?.questions.length || 0;
    if (current < total - 1) {
      this.currentQuestionIndex.set(current + 1);
    }
  }

  prevQuestion(): void {
    const current = this.currentQuestionIndex();
    if (current > 0) {
      this.currentQuestionIndex.set(current - 1);
    }
  }

  // ── Auto-Save ──

  onAnswerChange(question: StudentQuestionDto, value: string): void {
    // Update local state instantly for UI feedback
    this.answers.update(ans => ({ ...ans, [question.id]: value }));
    this.savingState.update(s => ({ ...s, [question.id]: true }));
    
    // Push to debouncer
    this.answerChange$.next({question, value});
  }

  private executeSaveAnswer(question: StudentQuestionDto, value: string): void {
    const attemptId = this.attemptData()?.attemptId;
    if (!attemptId) return;

    const isOption = question.type !== 'ShortAnswer'; // In our enum: MCQ | TrueFalse
    
    this.attemptService.saveAnswer(attemptId, {
      questionId: question.id,
      selectedOptionId: isOption ? value : null,
      textAnswer: isOption ? null : value
    }).subscribe({
      next: () => {
        this.savingState.update(s => ({ ...s, [question.id]: false }));
      },
      error: () => {
        this.toastr.error('Failed to save answer. Please check connection.');
        this.savingState.update(s => ({ ...s, [question.id]: false }));
      }
    });
  }

  // ── Submit ──

  goToReview(): void {
    this.state.set('review');
  }

  backToExam(): void {
    this.state.set('active');
  }

  submitFinal(): void {
    Swal.fire({
      title: 'Submit Final?',
      text: "You won't be able to change your answers after submitting.",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes, submit exam!',
      cancelButtonText: 'No, return to review'
    }).then((result: any) => {
      if (result.isConfirmed) {
        this.submitExam();
      }
    });
  }

  private submitExam(): void {
    const attemptId = this.attemptData()?.attemptId;
    if (!attemptId) return;

    this.state.set('submitting');
    this.timerSub?.unsubscribe();

    this.attemptService.submitExam(attemptId).subscribe({
      next: (res) => {
        this.toastr.success('Exam submitted successfully!');
        
        // Route conditionally based on resultVisibility and status
        if (res.data?.resultVisibility === 'Immediate' && res.data?.status === 'Graded') {
          this.router.navigate(['/exam-results', attemptId]);
        } else {
          this.router.navigate(['/courses', res.data?.courseId || this.examDetails()?.courseId], { queryParams: { tab: 'exams' } });
        }
      },
      error: (err) => {
        this.toastr.error('Failed to submit exam. Contact support.');
        this.state.set('active');
        this.startTimer(this.attemptData()!.expiresAt); // Restart timer visually
      }
    });
  }

  private forceSubmit(): void {
    const attemptId = this.attemptData()?.attemptId;
    if (!attemptId) return;

    this.state.set('submitting');

    this.attemptService.forceSubmitExam(attemptId).subscribe({
      next: () => {
        this.toastr.success('Exam was auto-submitted due to time expiry.');
        setTimeout(() => {
          this.router.navigate(['/courses', this.examDetails()?.courseId], { queryParams: { tab: 'exams' } });
        }, 1500);
      },
      error: () => {
        setTimeout(() => {
          this.router.navigate(['/courses', this.examDetails()?.courseId]);
        }, 1500);
      }
    });
  }

  getExamQuestionCount(exam: ExamDetailResponse | null): number {
    if (!exam || !exam.selectionRules) return 0;
    return exam.selectionRules.reduce((sum, rule) => sum + rule.count, 0);
  }
}
