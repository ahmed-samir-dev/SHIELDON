import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { ExamResultService, ExamAttemptSummaryDto, ExamResultResponse } from '../services/exam-result';
import { ToastrService } from 'ngx-toastr';
import { LucideAngularModule, ArrowLeft, Search, CheckCircle, Clock, Eye, AlertCircle } from 'lucide-angular';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-tutor-results-panel',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, LucideAngularModule],
  templateUrl: './tutor-results-panel.html',
  styleUrl: './tutor-results-panel.scss',
  providers: [DatePipe]
})
export class TutorResultsPanel implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private examResultService = inject(ExamResultService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);

  // Icons
  readonly ArrowLeft = ArrowLeft;
  readonly Search = Search;
  readonly CheckCircle = CheckCircle;
  readonly Clock = Clock;
  readonly Eye = Eye;
  readonly AlertCircle = AlertCircle;

  examId = '';
  courseId = '';

  attempts = signal<ExamAttemptSummaryDto[]>([]);
  filteredAttempts = signal<ExamAttemptSummaryDto[]>([]);
  isLoading = signal(true);
  searchQuery = signal('');

  selectedStudentIds = signal<Set<string>>(new Set());

  get unreleasedCount(): number {
    return this.attempts().filter(a => a.status === 'Graded' && !a.isGradePublished).length;
  }

  toggleSelection(studentId: string) {
    const current = new Set(this.selectedStudentIds());
    if (current.has(studentId)) {
      current.delete(studentId);
    } else {
      current.add(studentId);
    }
    this.selectedStudentIds.set(current);
  }

  toggleAllUnreleased() {
    const unreleased = this.attempts().filter(a => a.status === 'Graded' && !a.isGradePublished);
    const current = this.selectedStudentIds();
    if (current.size === unreleased.length && unreleased.length > 0) {
      this.selectedStudentIds.set(new Set()); // deselect all
    } else {
      this.selectedStudentIds.set(new Set(unreleased.map(a => a.studentId))); // select all
    }
  }

  isAllUnreleasedSelected(): boolean {
    const unreleased = this.attempts().filter(a => a.status === 'Graded' && !a.isGradePublished);
    return unreleased.length > 0 && this.selectedStudentIds().size === unreleased.length;
  }

  // Grading Modal State
  isGradingModalOpen = signal(false);
  isGradingLoading = signal(false);
  isSubmittingGrades = signal(false);
  gradingResult = signal<ExamResultResponse | null>(null);
  gradingForm!: FormGroup;
  currentAttemptId: string | null = null;

  ngOnInit() {
    this.examId = this.route.snapshot.paramMap.get('examId') || '';
    this.courseId = this.route.snapshot.paramMap.get('courseId') || '';

    if (!this.examId) {
      this.toastr.error('Invalid Exam ID');
      this.goBack();
      return;
    }

    this.loadAttempts();
  }

  loadAttempts() {
    this.isLoading.set(true);
    this.examResultService.getExamAttempts(this.examId).subscribe({
      next: (res) => {
        this.attempts.set(res.data);
        this.filterAttempts(this.searchQuery());
        this.isLoading.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to load attempts');
        this.isLoading.set(false);
      }
    });
  }

  filterAttempts(query: string) {
    this.searchQuery.set(query);
    if (!query.trim()) {
      this.filteredAttempts.set(this.attempts());
      return;
    }
    const q = query.toLowerCase();
    const filtered = this.attempts().filter(a => 
      a.studentName.toLowerCase().includes(q) || 
      a.studentDisplayId.toLowerCase().includes(q)
    );
    this.filteredAttempts.set(filtered);
  }

  goBack() {
    if (this.courseId) {
      this.router.navigate(['/courses', this.courseId], { queryParams: { tab: 'exams' } });
    } else {
      this.router.navigate(['/courses']);
    }
  }

  releaseResults() {
    Swal.fire({
      title: 'Release All Results?',
      text: 'This will publish all graded attempts and notify students. You cannot undo this.',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#215DAE',
      confirmButtonText: 'Yes, release all'
    }).then((res) => {
      if (res.isConfirmed) {
        this.examResultService.releaseResults(this.examId).subscribe({
          next: (response) => {
            this.toastr.success(response.data || 'Results released successfully');
            this.selectedStudentIds.set(new Set());
            this.loadAttempts();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to release results');
          }
        });
      }
    });
  }

  releaseStudentResult(studentId: string) {
    Swal.fire({
      title: 'Release Result?',
      text: 'This will publish this student\'s result and notify them.',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#215DAE',
      confirmButtonText: 'Yes, release'
    }).then((res) => {
      if (res.isConfirmed) {
        this.examResultService.releaseResults(this.examId, { studentIds: [studentId] }).subscribe({
          next: (response) => {
            this.toastr.success(response.data || 'Student result released successfully');
            const current = new Set(this.selectedStudentIds());
            current.delete(studentId);
            this.selectedStudentIds.set(current);
            this.loadAttempts();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to release result');
          }
        });
      }
    });
  }

  releaseSelectedResults() {
    const selected = Array.from(this.selectedStudentIds());
    if (selected.length === 0) return;

    Swal.fire({
      title: `Release ${selected.length} Result(s)?`,
      text: 'This will publish grades for the selected students and notify them.',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#215DAE',
      confirmButtonText: 'Yes, release them'
    }).then((res) => {
      if (res.isConfirmed) {
        this.examResultService.releaseResults(this.examId, { studentIds: selected }).subscribe({
          next: (response) => {
            this.toastr.success(response.data || 'Selected results released successfully');
            this.selectedStudentIds.set(new Set());
            this.loadAttempts();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to release selected results');
          }
        });
      }
    });
  }

  // ── Short Answer Grading ──────────────────────────────────────────────

  get shortAnswerGrades(): FormArray {
    return this.gradingForm.get('grades') as FormArray;
  }

  openGradingModal(attemptId: string) {
    this.currentAttemptId = attemptId;
    this.isGradingModalOpen.set(true);
    this.isGradingLoading.set(true);
    
    this.gradingForm = this.fb.group({
      grades: this.fb.array([])
    });

    this.examResultService.getAttemptResult(attemptId).subscribe({
      next: (res) => {
        this.gradingResult.set(res.data);
        
        // Populate form with short answer questions
        const saQuestions = res.data.questionReviews?.filter(q => q.requiresManualGrading) || [];
        const gradesArray = this.gradingForm.get('grades') as FormArray;
        
        saQuestions.forEach(q => {
          gradesArray.push(this.fb.group({
            questionId: [q.questionId],
            pointsAwarded: [
              q.pointsAwarded !== null ? q.pointsAwarded : 0, 
              [Validators.required, Validators.min(0), Validators.max(q.points)]
            ]
          }));
        });

        this.isGradingLoading.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to load attempt details');
        this.closeGradingModal();
      }
    });
  }

  closeGradingModal() {
    this.isGradingModalOpen.set(false);
    this.currentAttemptId = null;
    this.gradingResult.set(null);
  }

  submitGrades() {
    if (this.gradingForm.invalid || !this.currentAttemptId) {
      this.gradingForm.markAllAsTouched();
      return;
    }

    this.isSubmittingGrades.set(true);
    const request = {
      grades: this.gradingForm.value.grades
    };

    this.examResultService.gradeShortAnswers(this.currentAttemptId, request).subscribe({
      next: (res) => {
        this.toastr.success('Grades saved successfully');
        this.closeGradingModal();
        this.loadAttempts();
        this.isSubmittingGrades.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to save grades');
        this.isSubmittingGrades.set(false);
      }
    });
  }
}
