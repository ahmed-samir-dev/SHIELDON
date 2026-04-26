import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ExamService } from '../services/exam.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { ExamSummaryResponse } from '../../../core/models/exam.model';
import { CourseDetailResponse } from '../../../core/models/courses.model';

@Component({
  selector: 'app-course-exams',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  templateUrl: './course-exams.html',
  styleUrl: './course-exams.scss'
})
export class CourseExamsComponent implements OnInit {
  @Input({ required: true }) course!: CourseDetailResponse;

  private examService = inject(ExamService);
  public authService = inject(AuthService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);

  exams = signal<ExamSummaryResponse[]>([]);
  isLoading = signal(true);
  isSubmitting = signal(false);
  isModalOpen = signal(false);

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
    this.examForm.reset({
      timeLimit: 60,
      maxAttempts: 1,
      passScore: 50,
      resultVisibility: 'Immediate'
    });
    this.isModalOpen.set(true);
  }

  closeModal() {
    this.isModalOpen.set(false);
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

    this.examService.createExam(this.course.id, request).subscribe({
      next: (res) => {
        this.toastr.success('Exam created successfully as Draft');
        this.closeModal();
        this.loadExams();
        this.isSubmitting.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to create exam');
        this.isSubmitting.set(false);
      }
    });
  }

  publishExam(examId: string) {
    if (!confirm('Are you sure you want to publish this exam? Students will be notified and this cannot be undone.')) {
      return;
    }

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

  deleteExam(examId: string) {
    if (!confirm('Are you sure you want to delete this draft exam?')) {
      return;
    }

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

  canManageExams(): boolean {
    const role = this.authService.userRole();
    if (role === 'Admin') return true;
    if (role === 'Tutor' && this.course.assignedTutorId === this.authService.currentUser()?.userId) return true;
    return false;
  }
}
