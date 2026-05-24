import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { ExamResultService, ExamAttemptSummaryDto, ExamResultResponse } from '../services/exam-result';
import { ToastrService } from 'ngx-toastr';
import { LucideAngularModule, ArrowLeft, Search, CheckCircle, Clock, Eye, AlertCircle, Download } from 'lucide-angular';
import Swal from 'sweetalert2';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-tutor-results-panel',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, LucideAngularModule, TranslateModule],
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
  public translate = inject(TranslateService);

  // Icons
  readonly ArrowLeft = ArrowLeft;
  readonly Search = Search;
  readonly CheckCircle = CheckCircle;
  readonly Clock = Clock;
  readonly Eye = Eye;
  readonly AlertCircle = AlertCircle;
  readonly Download = Download;

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
      this.toastr.error(this.translate.instant('TUTOR_RESULTS_PANEL.TOAST_ERR_INVALID_ID'));
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
        this.toastr.error(err.error?.message || this.translate.instant('TUTOR_RESULTS_PANEL.TOAST_ERR_LOAD'));
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

  downloadCsv() {
    this.examResultService.exportResultsCsv(this.examId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `exam_${this.examId}_results.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.toastr.error(this.translate.instant('TUTOR_RESULTS_PANEL.TOAST_CSV_ERR'));
      }
    });
  }

  releaseResults() {
    Swal.fire({
      title: this.translate.instant('TUTOR_RESULTS_PANEL.SWAL_RELEASE_ALL_TITLE'),
      text: this.translate.instant('TUTOR_RESULTS_PANEL.SWAL_RELEASE_ALL_DESC'),
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#215DAE',
      confirmButtonText: this.translate.instant('TUTOR_RESULTS_PANEL.SWAL_BTN_RELEASE_ALL'),
      cancelButtonText: this.translate.instant('TUTOR_RESULTS_PANEL.BTN_CANCEL')
    }).then((res) => {
      if (res.isConfirmed) {
        this.examResultService.releaseResults(this.examId).subscribe({
          next: (response) => {
            this.toastr.success(response.data || this.translate.instant('TUTOR_RESULTS_PANEL.TOAST_RELEASE_ALL_SUCCESS'));
            this.selectedStudentIds.set(new Set());
            this.loadAttempts();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || this.translate.instant('TUTOR_RESULTS_PANEL.TOAST_RELEASE_ALL_ERR'));
          }
        });
      }
    });
  }

  releaseStudentResult(studentId: string) {
    Swal.fire({
      title: this.translate.instant('TUTOR_RESULTS_PANEL.SWAL_RELEASE_ONE_TITLE'),
      text: this.translate.instant('TUTOR_RESULTS_PANEL.SWAL_RELEASE_ONE_DESC'),
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#215DAE',
      confirmButtonText: this.translate.instant('TUTOR_RESULTS_PANEL.SWAL_BTN_RELEASE_ONE'),
      cancelButtonText: this.translate.instant('TUTOR_RESULTS_PANEL.BTN_CANCEL')
    }).then((res) => {
      if (res.isConfirmed) {
        this.examResultService.releaseResults(this.examId, { studentIds: [studentId] }).subscribe({
          next: (response) => {
            this.toastr.success(response.data || this.translate.instant('TUTOR_RESULTS_PANEL.TOAST_RELEASE_ONE_SUCCESS'));
            const current = new Set(this.selectedStudentIds());
            current.delete(studentId);
            this.selectedStudentIds.set(current);
            this.loadAttempts();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || this.translate.instant('TUTOR_RESULTS_PANEL.TOAST_RELEASE_ONE_ERR'));
          }
        });
      }
    });
  }

  releaseSelectedResults() {
    const selected = Array.from(this.selectedStudentIds());
    if (selected.length === 0) return;

    Swal.fire({
      title: this.translate.instant('TUTOR_RESULTS_PANEL.SWAL_RELEASE_SEL_TITLE').replace('{count}', selected.length.toString()),
      text: this.translate.instant('TUTOR_RESULTS_PANEL.SWAL_RELEASE_SEL_DESC'),
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#215DAE',
      confirmButtonText: this.translate.instant('TUTOR_RESULTS_PANEL.SWAL_BTN_RELEASE_SEL'),
      cancelButtonText: this.translate.instant('TUTOR_RESULTS_PANEL.BTN_CANCEL')
    }).then((res) => {
      if (res.isConfirmed) {
        this.examResultService.releaseResults(this.examId, { studentIds: selected }).subscribe({
          next: (response) => {
            this.toastr.success(response.data || this.translate.instant('TUTOR_RESULTS_PANEL.TOAST_RELEASE_SEL_SUCCESS'));
            this.selectedStudentIds.set(new Set());
            this.loadAttempts();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || this.translate.instant('TUTOR_RESULTS_PANEL.TOAST_RELEASE_SEL_ERR'));
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
        this.toastr.error(err.error?.message || this.translate.instant('TUTOR_RESULTS_PANEL.TOAST_ERR_LOAD_DETAILS'));
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
        this.toastr.success(this.translate.instant('TUTOR_RESULTS_PANEL.TOAST_GRADE_SUCCESS'));
        this.closeGradingModal();
        this.loadAttempts();
        this.isSubmittingGrades.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || this.translate.instant('TUTOR_RESULTS_PANEL.TOAST_GRADE_ERR'));
        this.isSubmittingGrades.set(false);
      }
    });
  }
}
