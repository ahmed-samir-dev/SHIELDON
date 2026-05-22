import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { UserRole } from '../../../core/models/user-role.enum';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslateModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private translate = inject(TranslateService);
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private toastr = inject(ToastrService);

  isLoading = signal(false);
  showPassword = signal(false);
  errorMessage = signal<string | null>(null);

  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    rememberMe: [false]
  });

  togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  onSubmit(): void {
    if (this.loginForm.invalid || this.isLoading()) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.loginForm.value;

    this.authService.login({ email: email!, password: password! }).subscribe({
      next: (response) => {
        const user = response.data;
        this.toastr.success(
          this.translate.instant('LOGIN.TOAST_SUCCESS', { name: user.firstName }),
          this.translate.instant('LOGIN.TOAST_SUCCESS_TITLE')
        );
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || this._getDashboardRoute(user.role);
        this.router.navigateByUrl(returnUrl);
      },
      error: (err) => {
        this.isLoading.set(false);
        const msg = err.error?.message || 'An unexpected error occurred. Please try again.';
        this.errorMessage.set(msg);
        
        // Premium touch: if account is unverified, toast a helpful message
        if (msg.toLowerCase().includes('verify') || msg.toLowerCase().includes('تحقق')) {
          this.toastr.info(
            this.translate.instant('LOGIN.TOAST_UNVERIFIED'),
            this.translate.instant('LOGIN.TOAST_INFO_TITLE')
          );
        }
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }

  resendVerification(): void {
    const email = this.loginForm.get('email')?.value;
    if (!email) return;

    this.isLoading.set(true);
    this.authService.resendVerification({ email }).subscribe({
      next: (res: any) => {
        this.isLoading.set(false);
        this.toastr.success(
          res.message || this.translate.instant('LOGIN.TOAST_RESEND_SUCCESS'),
          this.translate.instant('LOGIN.TOAST_SUCCESS_TITLE')
        );
      },
      error: (err) => {
        this.isLoading.set(false);
        this.toastr.error(
          err.error?.message || this.translate.instant('LOGIN.TOAST_RESEND_ERROR'),
          this.translate.instant('LOGIN.TOAST_ERROR_TITLE')
        );
      }
    });
  }

  private _getDashboardRoute(role: UserRole): string {
    switch (role) {
      case UserRole.Admin: return '/admin/dashboard';
      case UserRole.Tutor: return '/courses';
      case UserRole.Student: return '/courses';
      default: return '/';
    }
  }

  get emailControl() { return this.loginForm.get('email')!; }
  get passwordControl() { return this.loginForm.get('password')!; }
}
