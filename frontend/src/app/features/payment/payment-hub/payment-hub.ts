import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PaymentService } from '../../../core/services/payment.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { PaymentRecordDto, PaymentHistoryQueryParams } from '../../../core/models/payment.model';
import Swal from 'sweetalert2';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-payment-hub',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, TranslateModule],
  templateUrl: './payment-hub.html',
  styleUrl: './payment-hub.scss'
})
export class PaymentHubComponent implements OnInit, OnDestroy {
  private paymentService = inject(PaymentService);
  public authService = inject(AuthService);
  private languageService = inject(LanguageService);
  public translate = inject(TranslateService);
  private langSub!: Subscription;

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

  ngOnInit(): void {
    if (this.isStudent) {
      this.loadPendingPayments();
    } else {
      this.isLoadingPending = false;
    }
    this.loadHistory();
    this.langSub = this.languageService.languageChange$.subscribe(() => {
      if (this.isStudent) this.loadPendingPayments();
      this.loadHistory();
    });
  }

  ngOnDestroy() {
    this.langSub?.unsubscribe();
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
        Swal.fire('Error', err.error?.message || this.translate.instant('PAYMENT_HUB.ERR_INIT'), 'error');
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
