import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { UserRole } from '../../../core/models/user-role.enum';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { environment } from '../../../../environments/environment';

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
  isCapsLockOn = signal(false);
  errorMessage = signal<string | null>(null);
  showRoleModal = signal(false);
  selectedRole = signal<'Student' | 'Tutor'>('Student');

  checkCapsLock(event: KeyboardEvent): void {
    if (event.getModifierState) {
      this.isCapsLockOn.set(event.getModifierState('CapsLock'));
    }
  }

  onGoogleSignIn(): void {
    this.showRoleModal.set(true);
  }

  selectRole(role: 'Student' | 'Tutor'): void {
    this.selectedRole.set(role);
  }

  closeRoleModal(): void {
    this.showRoleModal.set(false);
  }

  private readonly GOOGLE_CLIENT_ID = environment.googleClientId;

  confirmGoogleSignIn(): void {
    if (!this.selectedRole() || this.isLoading()) return;
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const google = (window as any).google;
    if (!google?.accounts?.id) {
      this.isLoading.set(false);
      this.toastr.error('Google Sign-In is not available. Please check your connection and try again.', 'Error');
      return;
    }

    google.accounts.id.initialize({
      client_id: this.GOOGLE_CLIENT_ID,
      callback: (response: any) => {
        this.executeGoogleAuth(response.credential);
      },
      auto_select: false,
      cancel_on_tap_outside: true
    });

    google.accounts.id.prompt((notification: any) => {
      if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
        // One Tap was blocked/skipped — fall back to the popup flow
        google.accounts.oauth2.initTokenClient({
          client_id: this.GOOGLE_CLIENT_ID,
          scope: 'openid email profile',
          callback: () => {}
        });

        // Use renderButton as fallback trigger via a hidden div
        const tempDiv = document.createElement('div');
        tempDiv.style.display = 'none';
        document.body.appendChild(tempDiv);

        google.accounts.id.renderButton(tempDiv, {
          type: 'standard',
          theme: 'outline',
          size: 'large'
        });

        const btn = tempDiv.querySelector('div[role="button"]') as HTMLElement;
        if (btn) {
          btn.click();
        } else {
          document.body.removeChild(tempDiv);
          this.isLoading.set(false);
          this.toastr.warning('Google Sign-In popup was blocked. Please allow popups for this site.', 'Sign-In');
        }

        setTimeout(() => {
          if (document.body.contains(tempDiv)) {
            document.body.removeChild(tempDiv);
          }
        }, 3000);
      }
    });
  }

  private executeGoogleAuth(idToken: string): void {
    const role = this.selectedRole() ?? 'Student';
    this.authService.googleAuth(idToken, role).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        this.showRoleModal.set(false);
        const user = response.data;
        this.toastr.success(
          `Welcome, ${user.firstName}! Signed in with Google.`,
          'Google Authentication'
        );
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || this._getDashboardRoute(user.role);
        this.router.navigateByUrl(returnUrl);
      },
      error: (err) => {
        this.isLoading.set(false);
        const msg = err.error?.message || 'Google authentication failed. Please try again.';
        this.errorMessage.set(msg);
        this.toastr.error(msg, 'Authentication Error');
      }
    });
  }

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
