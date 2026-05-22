import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslateModule],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.scss'
})
export class ForgotPassword {
  private translate = inject(TranslateService);
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private toastr = inject(ToastrService);

  isLoading = signal(false);
  isSuccess = signal(false);

  forgotForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  get emailControl() { return this.forgotForm.get('email')!; }

  onSubmit() {
    if (this.forgotForm.invalid || this.isLoading()) return;

    this.isLoading.set(true);
    const email = this.forgotForm.value.email!;

    this.authService.forgotPassword({ email }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.isSuccess.set(true);
      },
      error: (err) => {
        this.isLoading.set(false);
        // Generic success message should mostly be used to prevent enumeration,
        // but if backend fails completely, toast it.
        const msg = err.error?.message || 'Something went wrong. Please try again.';
        this.toastr.error(msg, this.translate.instant('FORGOT.TOAST_ERROR_TITLE'));
      }
    });
  }
}
