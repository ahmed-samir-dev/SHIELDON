import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PaymentService } from '../../../core/services/payment.service';
import { PaymentRecordDto } from '../../../core/models/payment.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-payment-hub',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './payment-hub.html',
  styleUrls: ['./payment-hub.css']
})
export class PaymentHubComponent implements OnInit {
  pendingPayments: PaymentRecordDto[] = [];
  isLoading = true;
  isProcessing = false;

  constructor(private paymentService: PaymentService) {}

  ngOnInit(): void {
    this.loadPendingPayments();
  }

  loadPendingPayments(): void {
    this.isLoading = true;
    this.paymentService.getPendingPayments().subscribe({
      next: (payments) => {
        this.pendingPayments = payments;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading payments:', err);
        this.isLoading = false;
        Swal.fire('Error', 'Failed to load pending payments.', 'error');
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
}
