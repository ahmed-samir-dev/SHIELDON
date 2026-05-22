import { Component, OnInit, OnDestroy, inject, signal, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { LucideAngularModule, Search, Download, CheckCircle, XCircle, AlertCircle, ChevronDown, ChevronRight, Edit2, CheckSquare } from 'lucide-angular';
import Swal from 'sweetalert2';
import { GradeService, CourseGradeSummaryResponse, GradeItemResponse } from '../services/grade.service';
import { LanguageService } from '../../../core/services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-course-grades',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule, TranslateModule],
  templateUrl: './course-grades.html',
  styleUrl: './course-grades.scss'
})
export class CourseGrades implements OnInit, OnChanges, OnDestroy {
  private gradeService = inject(GradeService);
  private route = inject(ActivatedRoute);
  private toastr = inject(ToastrService);
  private languageService = inject(LanguageService);
  public translate = inject(TranslateService);
  private langSub!: Subscription;

  // Icons
  readonly Search = Search;
  readonly Download = Download;
  readonly CheckCircle = CheckCircle;
  readonly XCircle = XCircle;
  readonly AlertCircle = AlertCircle;
  readonly ChevronDown = ChevronDown;
  readonly ChevronRight = ChevronRight;
  readonly Edit2 = Edit2;
  readonly CheckSquare = CheckSquare;

  // State
  @Input() courseId!: string;
  isLoading = signal(true);
  summaries = signal<CourseGradeSummaryResponse[]>([]);
  expandedStudentIds = signal<Set<string>>(new Set());
  
  // Pagination & Filtering
  currentPage = signal(1);
  pageSize = signal(20);
  totalPages = signal(1);
  totalCount = signal(0);
  
  currentTypeFilter = signal<'All' | 'Exam' | 'Assignment'>('All');
  currentStatusFilter = signal<'All' | 'Published' | 'Unpublished'>('All');
  searchTerm = signal<string>('');
  private searchTimeout: any;

  ngOnInit() {
    if (this.courseId) {
      this.loadGrades();
    }
    this.langSub = this.languageService.languageChange$.subscribe(() => {
      if (this.courseId) this.loadGrades();
    });
  }

  ngOnDestroy() {
    this.langSub?.unsubscribe();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['courseId'] && !changes['courseId'].firstChange) {
      if (this.courseId) {
        this.loadGrades();
      }
    }
  }

  loadGrades() {
    this.isLoading.set(true);
    
    this.gradeService.getCourseGrades(this.courseId, {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      type: this.currentTypeFilter() === 'All' ? null : this.currentTypeFilter() as 'Exam' | 'Assignment',
      status: this.currentStatusFilter() === 'All' ? null : this.currentStatusFilter() as 'Published' | 'Unpublished',
      searchTerm: this.searchTerm() || null
    }).subscribe({
      next: (res) => {
        this.summaries.set(res.data.items);
        this.totalPages.set(res.data.totalPages);
        this.totalCount.set(res.data.totalCount);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to load grades');
        this.isLoading.set(false);
      }
    });
  }

  onSearch(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
    
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
    this.searchTimeout = setTimeout(() => {
      this.currentPage.set(1);
      this.loadGrades();
    }, 400);
  }

  filterByType(event: Event) {
    const value = (event.target as HTMLSelectElement).value as any;
    this.currentTypeFilter.set(value);
    this.currentPage.set(1);
    this.loadGrades();
  }

  filterByStatus(event: Event) {
    const value = (event.target as HTMLSelectElement).value as any;
    this.currentStatusFilter.set(value);
    this.currentPage.set(1);
    this.loadGrades();
  }

  changePage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadGrades();
    }
  }

  toggleExpand(studentId: string) {
    const expanded = this.expandedStudentIds();
    if (expanded.has(studentId)) {
      expanded.delete(studentId);
    } else {
      expanded.add(studentId);
    }
    this.expandedStudentIds.set(new Set(expanded));
  }

  isExpanded(studentId: string): boolean {
    return this.expandedStudentIds().has(studentId);
  }



  evaluateAssignment(grade: GradeItemResponse) {
    Swal.fire({
      title: this.translate.instant('COURSE_GRADES.SWAL_EVALUATE_TITLE'),
      html: `
        <div class="swal-form-group" style="text-align: left;">
          <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">${this.translate.instant('COURSE_GRADES.SWAL_SCORE_LABEL').replace('{max}', grade.maxScore.toString())}</label>
          <input id="swal-score" class="swal2-input" type="number" step="0.5" value="${grade.score}" max="${grade.maxScore}" min="0" style="margin: 0; width: 100%; box-sizing: border-box;">
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: this.translate.instant('COURSE_GRADES.SWAL_BTN_SAVE'),
      preConfirm: () => {
        const scoreVal = (document.getElementById('swal-score') as HTMLInputElement).value;
        
        return {
          score: scoreVal ? parseFloat(scoreVal) : null,
          notes: null // No feedback per user request
        };
      }
    }).then((result) => {
      if (result.isConfirmed) {
        this.gradeService.updateGrade(grade.id, result.value).subscribe({
          next: () => {
            this.toastr.success(this.translate.instant('COURSE_GRADES.TOAST_EVALUATE_SUCCESS'));
            this.loadGrades();
          },
          error: (err) => this.toastr.error(err.error?.message || this.translate.instant('COURSE_GRADES.TOAST_EVALUATE_FAIL'))
        });
      }
    });
  }

  publishGrade(grade: GradeItemResponse) {
    if (grade.isPublished) return;
    
    this.gradeService.publishGrades(this.courseId, { gradeIds: [grade.id] }).subscribe({
      next: (msg) => {
        this.toastr.success(msg.data || this.translate.instant('COURSE_GRADES.TOAST_PUBLISH_SUCCESS'));
        this.loadGrades();
      },
      error: (err) => this.toastr.error(err.error?.message || this.translate.instant('COURSE_GRADES.TOAST_PUBLISH_FAIL'))
    });
  }

  publishAllUnpublished() {
    Swal.fire({
      title: this.translate.instant('COURSE_GRADES.SWAL_BULK_PUBLISH_TITLE'),
      text: this.translate.instant('COURSE_GRADES.SWAL_BULK_PUBLISH_DESC'),
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#3b82f6',
      cancelButtonColor: '#ef4444',
      confirmButtonText: this.translate.instant('COURSE_GRADES.SWAL_BTN_PUBLISH_ALL')
    }).then((result) => {
      if (result.isConfirmed) {
        this.gradeService.publishGrades(this.courseId, { publishAll: true }).subscribe({
          next: (res) => {
            const msg = res.data || (res as any).message || this.translate.instant('COURSE_GRADES.TOAST_BULK_SUCCESS');
            if (msg.includes('No unpublished')) {
              this.toastr.info(msg);
            } else {
              this.toastr.success(msg);
            }
            this.loadGrades();
          },
          error: (err) => this.toastr.error(err.error?.message || this.translate.instant('COURSE_GRADES.TOAST_BULK_FAIL'))
        });
      }
    });
  }

  exportCsv() {
    this.gradeService.exportGradesCsv(this.courseId);
  }
}
