import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { LucideAngularModule, Search, CheckCircle, XCircle, AlertCircle, Eye, RefreshCw, ChevronLeft, ChevronRight } from 'lucide-angular';
import Swal from 'sweetalert2';
import { ReattemptService, ReattemptRequestResponse } from '../services/reattempt.service';

@Component({
  selector: 'app-reattempt-requests',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule],
  templateUrl: './reattempt-requests.html',
  styleUrl: './reattempt-requests.scss'
})
export class ReattemptRequestsComponent implements OnInit {
  private reattemptService = inject(ReattemptService);
  private toastr = inject(ToastrService);

  // Icons
  readonly Search = Search;
  readonly CheckCircle = CheckCircle;
  readonly XCircle = XCircle;
  readonly AlertCircle = AlertCircle;
  readonly Eye = Eye;
  readonly RefreshCw = RefreshCw;
  readonly ChevronLeft = ChevronLeft;
  readonly ChevronRight = ChevronRight;

  // State
  isLoading = signal(true);
  requests = signal<ReattemptRequestResponse[]>([]);
  
  // Pagination & Filtering
  currentPage = signal(1);
  pageSize = signal(10);
  totalPages = signal(1);
  totalCount = signal(0);
  currentStatusFilter = signal<string>('All');
  searchTerm = signal<string>('');
  private searchTimeout: any;

  statusTabs = ['All', 'Pending', 'Approved', 'Rejected'];

  ngOnInit() {
    this.loadRequests();
  }

  loadRequests() {
    this.isLoading.set(true);
    
    this.reattemptService.getRequests({
      page: this.currentPage(),
      pageSize: this.pageSize(),
      status: this.currentStatusFilter() === 'All' ? null : this.currentStatusFilter(),
      searchTerm: this.searchTerm() || null
    }).subscribe({
      next: (res) => {
        this.requests.set(res.data.items);
        this.totalPages.set(res.data.totalPages);
        this.totalCount.set(res.data.totalCount);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to load requests');
        this.isLoading.set(false);
      }
    });
  }

  filterByStatus(status: string) {
    this.currentStatusFilter.set(status);
    this.currentPage.set(1);
    this.loadRequests();
  }

  onSearch(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
    
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
    this.searchTimeout = setTimeout(() => {
      this.currentPage.set(1);
      this.loadRequests();
    }, 400);
  }

  changePage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadRequests();
    }
  }

  viewJustification(req: ReattemptRequestResponse) {
    Swal.fire({
      title: 'Student Justification',
      text: req.justification,
      icon: 'info',
      confirmButtonColor: '#215DAE'
    });
  }

  approveRequest(id: string, studentName: string) {
    Swal.fire({
      title: 'Approve Request?',
      text: `Are you sure you want to approve ${studentName}'s re-attempt request? They will be able to take the exam again immediately.`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#16A34A',
      cancelButtonColor: '#87949C',
      confirmButtonText: 'Yes, Approve'
    }).then((result) => {
      if (result.isConfirmed) {
        this.reattemptService.reviewRequest(id, { approved: true }).subscribe({
          next: (res) => {
            this.toastr.success(res.message);
            this.loadRequests();
          },
          error: (err) => this.toastr.error(err.error?.message || 'Failed to approve request')
        });
      }
    });
  }

  rejectRequest(id: string, studentName: string) {
    Swal.fire({
      title: 'Reject Request',
      text: `Rejecting ${studentName}'s request. You may provide an optional reason below.`,
      input: 'textarea',
      inputPlaceholder: 'Reason for rejection (optional)...',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#DC2626',
      cancelButtonColor: '#87949C',
      confirmButtonText: 'Reject'
    }).then((result) => {
      if (result.isConfirmed) {
        this.reattemptService.reviewRequest(id, { 
          approved: false, 
          rejectionReason: result.value || undefined 
        }).subscribe({
          next: (res) => {
            this.toastr.success(res.message);
            this.loadRequests();
          },
          error: (err) => this.toastr.error(err.error?.message || 'Failed to reject request')
        });
      }
    });
  }
}
