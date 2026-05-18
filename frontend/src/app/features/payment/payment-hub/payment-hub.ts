import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PaymentService } from '../../../core/services/payment.service';
import { AuthService } from '../../../core/services/auth.service';
import { PaymentRecordDto, PaymentHistoryQueryParams } from '../../../core/models/payment.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-payment-hub',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './payment-hub.html',
  styleUrl: './payment-hub.scss'
})
export class PaymentHubComponent implements OnInit {
  // Pending Payments State
  pendingPayments: PaymentRecordDto[] = [];
  isLoadingPending = true;
  isProcessing = false;

  // History State
  historyRecords: PaymentRecordDto[] = [];
  isLoadingHistory = true;
  
  // Pagination & Filtering
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  searchQuery = '';
  statusFilter = '';

  constructor(
    private paymentService: PaymentService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    if (this.isStudent) {
      this.loadPendingPayments();
    } else {
      this.isLoadingPending = false; // Admin doesn't load pending
    }
    
    this.loadHistory();
  }

  get isStudent(): boolean {
    return this.authService.currentUser()?.role === 'Student';
  }

  get isAdmin(): boolean {
    return this.authService.currentUser()?.role === 'Admin';
  }

  loadPendingPayments(): void {
    this.isLoadingPending = true;
    this.paymentService.getPendingPayments().subscribe({
      next: (payments) => {
        this.pendingPayments = payments;
        this.isLoadingPending = false;
      },
      error: (err) => {
        console.error('Error loading pending payments:', err);
        this.isLoadingPending = false;
      }
    });
  }

  loadHistory(): void {
    this.isLoadingHistory = true;
    const params: PaymentHistoryQueryParams = {
      page: this.currentPage,
      pageSize: this.pageSize
    };

    if (this.searchQuery) params.search = this.searchQuery;
    if (this.statusFilter) params.status = this.statusFilter;

    this.paymentService.getPaymentHistory(params).subscribe({
      next: (response) => {
        this.historyRecords = response.items;
        this.totalCount = response.totalCount;
        this.currentPage = response.pageNumber;
        this.totalPages = response.totalPages;
        this.isLoadingHistory = false;
      },
      error: (err) => {
        console.error('Error loading payment history:', err);
        this.isLoadingHistory = false;
      }
    });
  }

  payCourseFee(payment: PaymentRecordDto): void {
    this.isProcessing = true;
    this.paymentService.createCheckoutSession(payment.id).subscribe({
      next: (response) => {
        // Redirect to Stripe Checkout
        window.location.href = response.checkoutUrl;
      },
      error: (err) => {
        console.error('Error creating checkout session:', err);
        this.isProcessing = false;
        Swal.fire('Error', err.error?.message || 'Failed to initiate payment.', 'error');
      }
    });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadHistory();
  }

  onStatusFilterChange(): void {
    this.currentPage = 1;
    this.loadHistory();
  }

  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadHistory();
    }
  }

  getPagesArray(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }
}
