import { Component, Input, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ExamService } from '../services/exam.service';
import { QuestionBankService } from '../services/question-bank.service';
import { AuthService } from '../../../core/services/auth.service';
import Swal from 'sweetalert2';
import { ToastrService } from 'ngx-toastr';
import { ExamSummaryResponse } from '../../../core/models/exam.model';
import { CourseDetailResponse } from '../../../core/models/courses.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-course-exams',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './course-exams.html',
  styleUrl: './course-exams.scss'
})
export class CourseExamsComponent implements OnInit, OnDestroy {
  @Input({ required: true }) course!: CourseDetailResponse;

  private examService = inject(ExamService);
  private questionBankService = inject(QuestionBankService);
  public authService = inject(AuthService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  exams = signal<ExamSummaryResponse[]>([]);
  isLoading = signal(true);
  isSubmitting = signal(false);
  isModalOpen = signal(false);
  editingExamId = signal<string | null>(null);
  editingExam = signal<ExamSummaryResponse | null>(null);

  currentTime = signal<Date>(new Date());
  private timeInterval: any;

  examForm: FormGroup;

  constructor() {
    this.examForm = this.fb.group({
      title: ['', Validators.required],
      instructions: [''],
      timeLimit: [60, [Validators.required, Validators.min(1)]],
      maxAttempts: [1, [Validators.required, Validators.min(1)]],
      passScore: [50, [Validators.required, Validators.min(0), Validators.max(100)]],
      resultVisibility: ['Immediate', Validators.required],
      scheduledAt: [''],
      scheduledEndAt: [''],
      mcqCount: [0, [Validators.required, Validators.min(0)]],
      trueFalseCount: [0, [Validators.required, Validators.min(0)]],
      shortAnswerCount: [0, [Validators.required, Validators.min(0)]]
    });
  }

  ngOnInit() {
    this.loadExams();
    // 1-second tick: keeps all badges and buttons in real-time sync
    this.timeInterval = setInterval(() => {
      this.currentTime.set(new Date());
    }, 1000);
  }

  ngOnDestroy() {
    if (this.timeInterval) clearInterval(this.timeInterval);
  }

  loadExams() {
    this.isLoading.set(true);
    this.examService.getExams(this.course.id).subscribe({
      next: (res) => {
        this.exams.set(res.data?.items || []);
        this.isLoading.set(false);
      },
      error: () => {
        this.toastr.error('Failed to load exams');
        this.isLoading.set(false);
      }
    });
  }

  openCreateModal() {
    this.editingExamId.set(null);
    this.examForm.reset({
      timeLimit: 60,
      maxAttempts: 1,
      passScore: 50,
      resultVisibility: 'Immediate',
      mcqCount: 0,
      trueFalseCount: 0,
      shortAnswerCount: 0
    });
    this.isModalOpen.set(true);
  }

  openEditModal(exam: ExamSummaryResponse) {
    this.editingExamId.set(exam.id);
    this.editingExam.set(exam);

    // Convert UTC ISO strings to local datetime-local format (YYYY-MM-DDTHH:mm)
    // We MUST use local time here — <input type="datetime-local"> expects local, not UTC.
    const toLocalInput = (isoStr: string): string => {
      const d = new Date(isoStr);
      const pad = (n: number) => n.toString().padStart(2, '0');
      return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
    };

    const mcqRule = exam.selectionRules?.find(r => r.questionType === 'MCQ');
    const tfRule = exam.selectionRules?.find(r => r.questionType === 'TrueFalse');
    const saRule = exam.selectionRules?.find(r => r.questionType === 'ShortAnswer');

    this.examForm.patchValue({
      title: exam.title,
      instructions: exam.instructions ?? '',
      timeLimit: exam.timeLimit,
      maxAttempts: exam.maxAttempts,
      passScore: exam.passScore,
      resultVisibility: exam.resultVisibility,
      scheduledAt:    exam.scheduledAt    ? toLocalInput(exam.scheduledAt)    : '',
      scheduledEndAt: exam.scheduledEndAt ? toLocalInput(exam.scheduledEndAt) : '',
      mcqCount: mcqRule ? mcqRule.count : 0,
      trueFalseCount: tfRule ? tfRule.count : 0,
      shortAnswerCount: saRule ? saRule.count : 0
    });

    this.isModalOpen.set(true);
  }

  closeModal() {
    this.isModalOpen.set(false);
    this.editingExamId.set(null);
    this.editingExam.set(null);
  }

  onSubmit() {
    if (this.examForm.invalid) {
      this.examForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const formValue = this.examForm.value;

    this.questionBankService.getBankCounts(this.course.id).subscribe({
      next: (res) => {
        const counts = res.data || {};
        const availableMcq = counts['MCQ'] || 0;
        const availableTf = counts['TrueFalse'] || 0;
        const availableSa = counts['ShortAnswer'] || 0;

        if (formValue.mcqCount > availableMcq) {
          this.toastr.warning(`Not enough MCQ questions in the bank. Selected: ${formValue.mcqCount}, Available: ${availableMcq}`);
          this.isSubmitting.set(false);
          return;
        }
        if (formValue.trueFalseCount > availableTf) {
          this.toastr.warning(`Not enough True/False questions in the bank. Selected: ${formValue.trueFalseCount}, Available: ${availableTf}`);
          this.isSubmitting.set(false);
          return;
        }
        if (formValue.shortAnswerCount > availableSa) {
          this.toastr.warning(`Not enough Short Answer questions in the bank. Selected: ${formValue.shortAnswerCount}, Available: ${availableSa}`);
          this.isSubmitting.set(false);
          return;
        }

        const selectionRules = [
          { questionType: 'MCQ', count: formValue.mcqCount },
          { questionType: 'TrueFalse', count: formValue.trueFalseCount },
          { questionType: 'ShortAnswer', count: formValue.shortAnswerCount }
        ].filter(r => r.count > 0);

        const request = {
          ...formValue,
          scheduledAt: formValue.scheduledAt ? new Date(formValue.scheduledAt).toISOString() : null,
          scheduledEndAt: formValue.scheduledEndAt ? new Date(formValue.scheduledEndAt).toISOString() : null,
          selectionRules
        };

        if (this.editingExamId()) {
          this.examService.updateExam(this.editingExamId()!, request).subscribe({
            next: () => {
              this.toastr.success('Exam updated successfully');
              this.finishSubmit();
            },
            error: (err) => {
              this.toastr.error(err.error?.message || 'Failed to update exam');
              this.isSubmitting.set(false);
            }
          });
        } else {
          this.examService.createExam(this.course.id, request).subscribe({
            next: () => {
              this.toastr.success('Exam created successfully as Draft');
              this.finishSubmit();
            },
            error: (err) => {
              this.toastr.error(err.error?.message || 'Failed to create exam');
              this.isSubmitting.set(false);
            }
          });
        }
      },
      error: () => {
        this.toastr.error('Failed to validate question bank counts.');
        this.isSubmitting.set(false);
      }
    });
  }

  private finishSubmit() {
    this.closeModal();
    this.loadExams();
    this.isSubmitting.set(false);
  }

  publishExam(examId: string) {
    Swal.fire({
      title: 'Publish Exam?',
      text: 'Students will be notified and this action cannot be undone.',
      icon: 'info',
      showCancelButton: true,
      confirmButtonColor: '#215DAE', // Matches your primary color
      cancelButtonColor: '#87949C',
      confirmButtonText: 'Yes, publish it'
    }).then((result) => {
      if (result.isConfirmed) {
        this.examService.publishExam(examId).subscribe({
          next: () => {
            this.toastr.success('Exam published successfully! Notifications sent to students.');
            this.loadExams();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to publish exam');
          }
        });
      }
    });
  }

  deleteExam(examId: string) {
    Swal.fire({
      title: 'Delete Exam?',
      text: 'Are you sure you want to delete this draft exam?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444', // Matches your danger color
      cancelButtonColor: '#87949C',
      confirmButtonText: 'Yes, delete it'
    }).then((result) => {
      if (result.isConfirmed) {
        this.examService.deleteExam(examId).subscribe({
          next: () => {
            this.toastr.success('Exam deleted successfully');
            this.loadExams();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to delete exam');
          }
        });
      }
    });
  }

  canManageExams(): boolean {
    const role = this.authService.userRole();
    if (role === 'Admin') return true;
    if (role === 'Tutor' && this.course.assignedTutorId === this.authService.currentUser()?.userId) return true;
    return false;
  }

  isExamScheduled(exam: ExamSummaryResponse): boolean {
    if (exam.status !== 'Published') return false;
    if (!exam.scheduledAt) return false;
    return new Date(exam.scheduledAt) > this.currentTime();
  }

  /**
   * Exam is "Active" when it is Published AND the window [scheduledAt, scheduledEndAt] is
   * currently open (or no window is configured at all).
   */
  isExamActive(exam: ExamSummaryResponse): boolean {
    if (exam.status !== 'Published') return false;
    const now = this.currentTime();
    if (exam.scheduledAt && new Date(exam.scheduledAt) > now) return false; // not started yet
    if (exam.scheduledEndAt && new Date(exam.scheduledEndAt) < now) return false; // already over
    return true;
  }

  isExamExpired(exam: ExamSummaryResponse): boolean {
    if (!exam.scheduledEndAt) return false;
    return new Date(exam.scheduledEndAt) < this.currentTime();
  }

  /** True when editing an exam that is currently expired. */
  isEditingExpiredExam(): boolean {
    const exam = this.editingExam();
    return !!exam && this.isExamExpired(exam);
  }

  takeExam(examId: string) {
    this.router.navigate(['/exam-engine', examId]);
  }

  getExamQuestionCount(exam: ExamSummaryResponse): number {
    if (!exam.selectionRules) return 0;
    return exam.selectionRules.reduce((sum, rule) => sum + rule.count, 0);
  }
}
