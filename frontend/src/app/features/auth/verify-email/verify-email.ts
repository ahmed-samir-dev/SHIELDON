import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  templateUrl: './verify-email.html',
  styleUrl: './verify-email.scss'
})
export class VerifyEmail implements OnInit {
  private translate = inject(TranslateService);
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);
  private router = inject(Router);

  email = signal<string>('');
  token = signal<string>('');

  isVerifying = signal(true);
  isSuccess = signal(false);
  errorMessage = signal<string | null>(null);

  isResending = signal(false);
  resendSuccess = signal(false);

  ngOnInit() {
    // Read query strings: ?email=...&token=...
    this.route.queryParams.subscribe(params => {
      const e = params['email'];
      const t = params['token'];

      if (!e || !t) {
        this.isVerifying.set(false);
        this.errorMessage.set(this.translate.instant('VERIFY.INV_LINK_TITLE'));
        return;
      }

      this.email.set(e);
      this.token.set(t);
      this.verifyEmail();
    });
  }

  verifyEmail() {
    this.isVerifying.set(true);
    this.errorMessage.set(null);

    this.authService.verifyEmail({ email: this.email(), token: this.token() }).subscribe({
      next: (res) => {
        this.isVerifying.set(false);
        this.isSuccess.set(true);
      },
      error: (err) => {
        this.isVerifying.set(false);
        const errorMsg = err.error?.message || 'An unexpected error occurred during verification.';
        this.errorMessage.set(errorMsg);
      }
    });
  }

  resendVerification() {
    if (this.isResending()) return;
    
    this.isResending.set(true);
    this.resendSuccess.set(false);
    this.errorMessage.set(null);

    this.authService.resendVerification({ email: this.email() }).subscribe({
      next: (res) => {
        this.isResending.set(false);
        this.resendSuccess.set(true);
      },
      error: (err) => {
        this.isResending.set(false);
        const errorMsg = err.error?.message || 'Failed to resend verification email.';
        this.errorMessage.set(errorMsg);
      }
    });
  }
}
