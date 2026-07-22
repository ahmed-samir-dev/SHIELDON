import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { UserRole } from '../../../core/models/user-role.enum';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslateModule],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class Register {
  private translate = inject(TranslateService);
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  isLoading = signal(false);
  showPassword = signal(false);
  showConfirmPassword = signal(false);
  errorMessage = signal<string | null>(null);
  
  // Available roles for registration
  readonly UserRole = UserRole;
  selectedRole = signal<UserRole>(UserRole.Student); // Default to Student

  private readonly GOOGLE_CLIENT_ID = environment.googleClientId;

  onGoogleSignIn(): void {
    const role = this.selectedRole() === UserRole.Tutor ? 'Tutor' : 'Student';
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
        const idToken = response.credential;
        this.authService.googleAuth(idToken, role).subscribe({
          next: (res) => {
            this.isLoading.set(false);
            const user = res.data;
            this.toastr.success(
              `Welcome, ${user.firstName}! Signed up with Google as ${role}.`,
              'Google Authentication'
            );
            this.router.navigateByUrl(user.role === UserRole.Admin ? '/admin/dashboard' : '/courses');
          },
          error: (err) => {
            this.isLoading.set(false);
            const msg = err.error?.message || 'Google registration failed. Please try again.';
            this.errorMessage.set(msg);
            this.toastr.error(msg, 'Authentication Error');
          }
        });
      },
      auto_select: false,
      cancel_on_tap_outside: true
    });

    google.accounts.id.prompt((notification: any) => {
      if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
        this.isLoading.set(false);
        this.toastr.warning('Google Sign-In popup was blocked. Please allow popups for this site.', 'Sign-In');
      }
    });
  }

  registerForm = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    password: [
      '', 
      [
        Validators.required, 
        Validators.minLength(8),
        Validators.pattern(/(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9])/) // Upper, lower, number, special
      ]
    ],
    confirmPassword: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  toggleConfirmPassword(): void {
    this.showConfirmPassword.update(v => !v);
  }

  selectRole(role: UserRole): void {
    this.selectedRole.set(role);
  }

  onSubmit(): void {
    if (this.registerForm.invalid || this.isLoading()) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const formValue = this.registerForm.value;
    const request = {
      firstName: formValue.firstName!,
      lastName: formValue.lastName!,
      email: formValue.email!,
      password: formValue.password!,
      confirmPassword: formValue.confirmPassword!,
      role: this.selectedRole()
    };

    this.authService.register(request).subscribe({
      next: () => {
        this.isLoading.set(false);
        Swal.fire({
          title: this.translate.instant('REGISTER.TOAST_SUCCESS_TITLE'),
          text: this.translate.instant('REGISTER.TOAST_SUCCESS', { name: formValue.firstName }),
          icon: 'success',
          iconColor: 'var(--color-success-base)',
          confirmButtonText: this.translate.instant('REGISTER.LOG_IN'),
          confirmButtonColor: 'var(--color-primary-base)',
          background: 'var(--color-neutral-white)',
          color: 'var(--color-neutral-900)',
          showClass: {
            popup: 'animate__animated animate__fadeInDown animate__faster'
          },
          hideClass: {
            popup: 'animate__animated animate__fadeOutUp animate__faster'
          },
          customClass: {
            title: 'font-outfit',
            popup: 'premium-swal-popup',
            confirmButton: 'premium-swal-button'
          }
        }).then(() => {
          this.router.navigate(['/login']);
        });
      },
      error: (err) => {
        this.isLoading.set(false);
        const msg = err.error?.message || 'Failed to create an account. Please try again.';
        this.errorMessage.set(msg);
      }
    });
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    if (password !== confirmPassword && confirmPassword) {
      control.get('confirmPassword')?.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    } else {
      if (control.get('confirmPassword')?.hasError('passwordMismatch')) {
        control.get('confirmPassword')?.setErrors(null);
      }
      return null;
    }
  }

  // Getters for template
  get firstNameControl() { return this.registerForm.get('firstName')!; }
  get lastNameControl() { return this.registerForm.get('lastName')!; }
  get emailControl() { return this.registerForm.get('email')!; }
  get passwordControl() { return this.registerForm.get('password')!; }
  get confirmPasswordControl() { return this.registerForm.get('confirmPassword')!; }
}
