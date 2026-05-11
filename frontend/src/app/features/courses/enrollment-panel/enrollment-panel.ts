import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CourseService } from '../services/course.service';
import { AuthService } from '../../../core/services/auth.service';
import { EnrollmentResponse, StudentEnrollmentStatusResponse } from '../../../core/models/courses.model';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-enrollment-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './enrollment-panel.html',
  styleUrl: './enrollment-panel.scss'
})
export class EnrollmentPanel implements OnInit {
  private courseService = inject(CourseService);
  public authService = inject(AuthService);
  private toastr = inject(ToastrService);

  pendingRequests = signal<EnrollmentResponse[]>([]);
  approvedStudents = signal<EnrollmentResponse[]>([]);
  studentRequests = signal<StudentEnrollmentStatusResponse[]>([]);
  isLoading = signal(true);
  selectedIds = signal<Set<string>>(new Set<string>());
  activeTab = signal<'pending' | 'approved'>('pending');

  // Pagination & Filtering for Approved tab
  approvedPage = signal(1);
  approvedPageSize = 10;
  approvedTotalCount = signal(0);
  approvedSearch = signal('');
  isApprovedLoading = signal(false);

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    if (this.authService.isStudent()) {
      this.loadStudentData();
    } else {
      this.loadPendingData();
      this.loadApprovedData();
    }
  }

  loadStudentData() {
    this.isLoading.set(true);
    this.courseService.getMyEnrollments().subscribe({
      next: (res) => {
        this.studentRequests.set(res.data);
        this.isLoading.set(false);
      },
      error: () => this.handleError()
    });
  }

  loadPendingData() {
    this.isLoading.set(true);
    this.courseService.getPendingEnrollments().subscribe({
      next: (res) => {
        this.pendingRequests.set(res.data);
        this.selectedIds.set(new Set<string>());
        this.isLoading.set(false);
      },
      error: () => this.handleError()
    });
  }

  loadApprovedData() {
    this.isApprovedLoading.set(true);
    this.courseService.getApprovedEnrollments({
      page: this.approvedPage(),
      pageSize: this.approvedPageSize,
      search: this.approvedSearch()
    }).subscribe({
      next: (res) => {
        this.approvedStudents.set(res.data.items);
        this.approvedTotalCount.set(res.data.totalCount);
        this.isApprovedLoading.set(false);
      },
      error: () => {
        this.isApprovedLoading.set(false);
        this.handleError();
      }
    });
  }

  onApprovedSearch(term: string) {
    this.approvedSearch.set(term);
    this.approvedPage.set(1);
    this.loadApprovedData();
  }

  onApprovedPageChange(page: number) {
    this.approvedPage.set(page);
    this.loadApprovedData();
  }

  get totalPages(): number {
    return Math.ceil(this.approvedTotalCount() / this.approvedPageSize);
  }

  mathMin(a: number, b: number): number {
    return Math.min(a, b);
  }

  setTab(tab: 'pending' | 'approved') {
    this.activeTab.set(tab);
    this.selectedIds.set(new Set<string>());
  }

  private handleError() {
    this.isLoading.set(false);
    this.toastr.error('Failed to load enrollments.');
  }

  // ── Review Actions (Admin/Tutor) ───────────────────────────────────────

  toggleSelection(id: string) {
    const set = new Set(this.selectedIds());
    if (set.has(id)) set.delete(id);
    else set.add(id);
    this.selectedIds.set(set);
  }

  toggleAll(event: Event) {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) {
      const allIds = this.pendingRequests().map(r => r.id);
      this.selectedIds.set(new Set(allIds));
    } else {
      this.selectedIds.set(new Set());
    }
  }

  isAllSelected(): boolean {
    return this.pendingRequests().length > 0 && this.selectedIds().size === this.pendingRequests().length;
  }

  reviewSingle(id: string, approved: boolean) {
    if (approved) {
      this.executeSingleReview(id, true, null);
    } else {
      this.promptForRejectionReason().then(reason => {
        if (reason !== false) {
          this.executeSingleReview(id, false, reason as string);
        }
      });
    }
  }

  private executeSingleReview(id: string, approved: boolean, reason: string | null) {
    this.courseService.reviewEnrollment(id, { approved, rejectionReason: reason }).subscribe({
      next: () => {
        this.toastr.success(`Enrollment ${approved ? 'approved' : 'rejected'} successfully.`);
        this.loadData();
      },
      error: () => this.toastr.error('Failed to process review.')
    });
  }

  bulkReview(approved: boolean) {
    if (this.selectedIds().size === 0) return;

    if (approved) {
      this.executeBulkReview(true, null);
    } else {
      this.promptForRejectionReason().then(reason => {
        if (reason !== false) {
          this.executeBulkReview(false, reason as string);
        }
      });
    }
  }

  private executeBulkReview(approved: boolean, reason: string | null) {
    const request = {
      enrollmentIds: Array.from(this.selectedIds()),
      approved,
      rejectionReason: reason
    };

    this.courseService.bulkReviewEnrollments(request).subscribe({
      next: () => {
        this.toastr.success(`${request.enrollmentIds.length} enrollment(s) processed.`);
        this.loadData();
      },
      error: () => this.toastr.error('Failed to process bulk review.')
    });
  }

  private async promptForRejectionReason(): Promise<string | false> {
    const { value: reason, isConfirmed } = await Swal.fire({
      title: 'Reject Enrollment',
      input: 'textarea',
      inputLabel: 'Reason for Rejection (Optional but recommended)',
      inputPlaceholder: 'Type your message here...',
      showCancelButton: true,
      confirmButtonText: 'Reject',
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#6b7280'
    });

    return isConfirmed ? (reason || null) : false;
  }
}
