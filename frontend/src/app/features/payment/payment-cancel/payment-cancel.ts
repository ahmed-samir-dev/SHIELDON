import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';

@Component({
  selector: 'app-payment-cancel',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './payment-cancel.html',
  styleUrl: './payment-cancel.scss'
})
export class PaymentCancelComponent {
  constructor(private router: Router) {}

  goToDashboard(): void {
    this.router.navigate(['/courses']);
  }
}
