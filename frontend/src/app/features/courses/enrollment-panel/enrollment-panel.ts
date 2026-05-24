import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CourseService } from '../services/course.service';
import { AuthService } from '../../../core/services/auth.service';
import { EnrollmentResponse, StudentEnrollmentStatusResponse } from '../../../core/models/courses.model';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-enrollment-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './enrollment-panel.html',
  styleUrl: './enrollment-panel.scss'
})
export class EnrollmentPanel implements OnInit {
  private courseService = inject(CourseService);
  public authService = inject(AuthService);
  private toastr = inject(ToastrService);
  private translate = inject(TranslateService);

  pendingRequests = signal<EnrollmentResponse[]>([]);
  approvedStudents = signal<EnrollmentResponse[]>([]);
  studentEnrollments = signal<StudentEnrollmentStatusResponse[]>([]);
  isLoading = signal(true);
  selectedIds = signal<Set<string>>(new Set<string>());
  activeTab = signal<'pending' | 'approved'>('pending');

  // Pagination & Filtering for Pending tab
  pendingPage = signal(1);
  pendingPageSize = 10;
  pendingTotalCount = signal(0);
  pendingSearch = signal('');

  // Pagination & Filtering for Approved tab
  approvedPage = signal(1);
  approvedPageSize = 10;
  approvedTotalCount = signal(0);
  approvedSearch = signal('');
  approvedDateFrom = signal('');
  approvedDateTo = signal('');
  isApprovedLoading = signal(false);

  // Pagination & Filtering for Student tab
  studentPage = signal(1);
  studentPageSize = 10;
  studentTotalCount = signal(0);
  studentSearch = signal('');
  studentStatus = signal('');
  studentDateFrom = signal('');
  studentDateTo = signal('');

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
    this.courseService.getMyEnrollments({
      page: this.studentPage(),
      pageSize: this.studentPageSize,
      searchTerm: this.studentSearch() || null,
      requestedFrom: this.studentDateFrom() || null,
      requestedTo: this.studentDateTo() || null,
      status: this.studentStatus() || null
    }).subscribe({
      next: (res) => {
        this.studentEnrollments.set(res.data.items);
        this.studentTotalCount.set(res.data.totalCount);
        this.isLoading.set(false);
      },
      error: () => this.handleError()
    });
  }

  loadPendingData() {
    this.isLoading.set(true);
    this.courseService.getPendingEnrollments({
      page: this.pendingPage(),
      pageSize: this.pendingPageSize,
      search: this.pendingSearch() || null
    }).subscribe({
      next: (res) => {
        this.pendingRequests.set(res.data.items);
        this.pendingTotalCount.set(res.data.totalCount);
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
      search: this.approvedSearch() || null,
      approvedFrom: this.approvedDateFrom() || null,
      approvedTo: this.approvedDateTo() || null
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

  onApprovedSearch() {
    this.approvedPage.set(1);
    this.loadApprovedData();
  }

  onApprovedPageChange(page: number) {
    this.approvedPage.set(page);
    this.loadApprovedData();
  }

  get approvedTotalPages(): number {
    return Math.ceil(this.approvedTotalCount() / this.approvedPageSize);
  }

  onPendingPageChange(page: number) {
    this.pendingPage.set(page);
    this.loadPendingData();
  }

  get pendingTotalPages(): number {
    return Math.ceil(this.pendingTotalCount() / this.pendingPageSize);
  }

  onStudentSearch() {
    this.studentPage.set(1);
    this.loadStudentData();
  }

  onStudentPageChange(page: number) {
    this.studentPage.set(page);
    this.loadStudentData();
  }

  get studentTotalPages(): number {
    return Math.ceil(this.studentTotalCount() / this.studentPageSize);
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
    this.toastr.error(this.translate.instant('ENROLLMENT_PANEL.TOAST_LOAD_ERR'));
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
        const status = approved ? this.translate.instant('ENROLLMENT_PANEL.STATUS_APPROVED') : this.translate.instant('ENROLLMENT_PANEL.STATUS_REJECTED');
        this.toastr.success(this.translate.instant('ENROLLMENT_PANEL.TOAST_REVIEW_SUCCESS').replace('{status}', status));
        this.loadData();
      },
      error: () => this.toastr.error(this.translate.instant('ENROLLMENT_PANEL.TOAST_REVIEW_ERR'))
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
        this.toastr.success(this.translate.instant('ENROLLMENT_PANEL.TOAST_BULK_SUCCESS').replace('{count}', request.enrollmentIds.length.toString()));
        this.loadData();
      },
      error: () => this.toastr.error(this.translate.instant('ENROLLMENT_PANEL.TOAST_BULK_ERR'))
    });
  }

  private async promptForRejectionReason(): Promise<string | false> {
    const { value: reason, isConfirmed } = await Swal.fire({
      title: this.translate.instant('ENROLLMENT_PANEL.SWAL_REJECT_TITLE'),
      input: 'textarea',
      inputLabel: this.translate.instant('ENROLLMENT_PANEL.SWAL_REJECT_LABEL'),
      inputPlaceholder: this.translate.instant('ENROLLMENT_PANEL.SWAL_REJECT_PLACEHOLDER'),
      showCancelButton: true,
      confirmButtonText: this.translate.instant('ENROLLMENT_PANEL.SWAL_BTN_REJECT'),
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#6b7280'
    });

    return isConfirmed ? (reason || null) : false;
  }
}
