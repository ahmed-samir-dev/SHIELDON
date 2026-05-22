import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-payment-cancel',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  templateUrl: './payment-cancel.html',
  styleUrl: './payment-cancel.scss'
})
export class PaymentCancelComponent {
  constructor(private router: Router) {}

  goToDashboard(): void {
    this.router.navigate(['/courses']);
  }
}
