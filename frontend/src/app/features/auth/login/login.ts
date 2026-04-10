import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { UserRole } from '../../../core/models/user-role.enum';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
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
        this.toastr.success(`Welcome back, ${user.firstName}! 👋`, 'Login Successful');
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || this._getDashboardRoute(user.role);
        this.router.navigateByUrl(returnUrl);
      },
      error: (err) => {
        this.isLoading.set(false);
        const msg = err.error?.message || 'An unexpected error occurred. Please try again.';
        this.errorMessage.set(msg);
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }

  private _getDashboardRoute(role: UserRole): string {
    switch (role) {
      case UserRole.Admin: return '/admin/dashboard';
      case UserRole.Tutor: return '/tutor/dashboard';
      case UserRole.Student: return '/student/dashboard';
      default: return '/';
    }
  }

  get emailControl() { return this.loginForm.get('email')!; }
  get passwordControl() { return this.loginForm.get('password')!; }
}
