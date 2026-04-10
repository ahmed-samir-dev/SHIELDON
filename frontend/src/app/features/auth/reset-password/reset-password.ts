import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.scss'
})
export class ResetPassword implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  email = signal<string>('');
  token = signal<string>('');
  
  isLoading = signal(false);
  isSuccess = signal(false);
  isInvalidLink = signal(false);
  showPassword = signal(false);

  resetForm = this.fb.group({
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const e = params['email'];
      const t = params['token'];

      if (!e || !t) {
        this.isInvalidLink.set(true);
        return;
      }

      this.email.set(e);
      this.token.set(t);
    });
  }

  passwordMatchValidator(g: any) {
    return g.get('newPassword').value === g.get('confirmPassword').value
      ? null : { mismatch: true };
  }

  get passwordControl() { return this.resetForm.get('newPassword')!; }
  get confirmControl() { return this.resetForm.get('confirmPassword')!; }

  togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  onSubmit() {
    if (this.resetForm.invalid || this.isLoading() || this.isInvalidLink()) return;

    this.isLoading.set(true);

    const payload = {
      email: this.email(),
      token: this.token(),
      newPassword: this.resetForm.value.newPassword!
    };

    this.authService.resetPassword(payload).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.isSuccess.set(true);
        this.toastr.success('Your password has been successfully reset.', 'Password Reset');
      },
      error: (err) => {
        this.isLoading.set(false);
        const msg = err.error?.message || 'Failed to reset password. The link might be expired.';
        this.toastr.error(msg, 'Error');
      }
    });
  }
}
