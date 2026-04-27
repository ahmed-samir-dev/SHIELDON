import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ExamService } from '../services/exam.service';
import { AuthService } from '../../../core/services/auth.service';
import Swal from 'sweetalert2';
import { ToastrService } from 'ngx-toastr';
import { ExamSummaryResponse } from '../../../core/models/exam.model';
import { CourseDetailResponse } from '../../../core/models/courses.model';
import { ExamQuestionsComponent } from '../exam-questions/exam-questions';
import { Router } from '@angular/router';

@Component({
  selector: 'app-course-exams',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ExamQuestionsComponent],
  templateUrl: './course-exams.html',
  styleUrl: './course-exams.scss'
})
export class CourseExamsComponent implements OnInit {
  @Input({ required: true }) course!: CourseDetailResponse;

  private examService = inject(ExamService);
  public authService = inject(AuthService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  exams = signal<ExamSummaryResponse[]>([]);
  isLoading = signal(true);
  isSubmitting = signal(false);
  isModalOpen = signal(false);
  editingExamId = signal<string | null>(null);

  // When set, the questions panel is shown instead of the exams list
  selectedExamForQuestions = signal<ExamSummaryResponse | null>(null);

  examForm: FormGroup;

  constructor() {
    this.examForm = this.fb.group({
      title: ['', Validators.required],
      instructions: [''],
      timeLimit: [60, [Validators.required, Validators.min(1)]],
      maxAttempts: [1, [Validators.required, Validators.min(1)]],
      passScore: [50, [Validators.required, Validators.min(0), Validators.max(100)]],
      resultVisibility: ['Immediate', Validators.required],
      scheduledAt: ['']
    });
  }

  ngOnInit() {
    this.loadExams();
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
      resultVisibility: 'Immediate'
    });
    this.isModalOpen.set(true);
  }

  openEditModal(exam: ExamSummaryResponse) {
    this.editingExamId.set(exam.id);
    
    // Format date for datetime-local input
    let formattedDate = '';
    if (exam.scheduledAt) {
      const date = new Date(exam.scheduledAt);
      formattedDate = date.toISOString().slice(0, 16); // "YYYY-MM-DDTHH:mm"
    }

    this.examForm.patchValue({
      title: exam.title,
      instructions: exam.instructions,
      timeLimit: exam.timeLimit,
      maxAttempts: exam.maxAttempts,
      passScore: exam.passScore,
      resultVisibility: exam.resultVisibility,
      scheduledAt: formattedDate
    });
    
    this.isModalOpen.set(true);
  }

  closeModal() {
    this.isModalOpen.set(false);
    this.editingExamId.set(null);
  }

  openQuestions(exam: ExamSummaryResponse) {
    this.selectedExamForQuestions.set(exam);
  }

  closeQuestions() {
    this.selectedExamForQuestions.set(null);
    this.loadExams(); // Refresh to get updated question count
  }

  onSubmit() {
    if (this.examForm.invalid) {
      this.examForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const formValue = this.examForm.value;
    
    const request = {
      ...formValue,
      scheduledAt: formValue.scheduledAt ? new Date(formValue.scheduledAt).toISOString() : null
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

  takeExam(examId: string) {
    this.router.navigate(['/exam-engine', examId]);
  }
}
