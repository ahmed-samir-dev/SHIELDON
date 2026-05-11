import { Component, OnInit, inject, signal, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { LucideAngularModule, Search, Download, CheckCircle, XCircle, AlertCircle, ChevronDown, ChevronRight, Edit2, CheckSquare } from 'lucide-angular';
import Swal from 'sweetalert2';
import { GradeService, CourseGradeSummaryResponse, GradeItemResponse } from '../services/grade.service';

@Component({
  selector: 'app-course-grades',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule],
  templateUrl: './course-grades.html',
  styleUrl: './course-grades.scss'
})
export class CourseGrades implements OnInit {
  private gradeService = inject(GradeService);
  private route = inject(ActivatedRoute);
  private toastr = inject(ToastrService);

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
      title: 'Evaluate Assignment',
      html: `
        <div class="swal-form-group" style="text-align: left;">
          <label style="display: block; margin-bottom: 8px; font-weight: 500; color: #374151;">Score (Max: ${grade.maxScore})</label>
          <input id="swal-score" class="swal2-input" type="number" step="0.5" value="${grade.score}" max="${grade.maxScore}" min="0" style="margin: 0; width: 100%; box-sizing: border-box;">
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: 'Save Score',
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
            this.toastr.success('Assignment evaluated successfully');
            this.loadGrades();
          },
          error: (err) => this.toastr.error(err.error?.message || 'Failed to evaluate assignment')
        });
      }
    });
  }

  publishGrade(grade: GradeItemResponse) {
    if (grade.isPublished) return;
    
    this.gradeService.publishGrades(this.courseId, { gradeIds: [grade.id] }).subscribe({
      next: (msg) => {
        this.toastr.success(msg.data || 'Grade published');
        this.loadGrades();
      },
      error: (err) => this.toastr.error(err.error?.message || 'Failed to publish')
    });
  }

  publishAllUnpublished() {
    Swal.fire({
      title: 'Bulk Publish Grades',
      text: 'Are you sure you want to publish all currently unpublished grades matching your current filters in this course?',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#3b82f6',
      cancelButtonColor: '#ef4444',
      confirmButtonText: 'Yes, Publish All'
    }).then((result) => {
      if (result.isConfirmed) {
        this.gradeService.publishGrades(this.courseId, { publishAll: true }).subscribe({
          next: (res) => {
            const msg = res.data || (res as any).message || 'Grades published successfully';
            if (msg.includes('No unpublished')) {
              this.toastr.info(msg);
            } else {
              this.toastr.success(msg);
            }
            this.loadGrades();
          },
          error: (err) => this.toastr.error(err.error?.message || 'Bulk publish failed')
        });
      }
    });
  }

  exportCsv() {
    this.gradeService.exportGradesCsv(this.courseId);
  }
}
