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
import { COUNTRY_CODES, CountryCode } from '../../../core/constants/country-codes.constant';

import { CountryPickerComponent } from '../../../shared/components/country-picker/country-picker';
import { PasswordStrengthBarComponent } from '../../../shared/components/password-strength-bar/password-strength-bar';
import { PasswordMatchBarComponent } from '../../../shared/components/password-match-bar/password-match-bar';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslateModule, CountryPickerComponent, PasswordStrengthBarComponent, PasswordMatchBarComponent],
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

  readonly countryCodes: CountryCode[] = COUNTRY_CODES;
  selectedCountry = signal<CountryCode>(COUNTRY_CODES[0]); // Default Egypt
  
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
    countryCode: ['+20'],
    localPhone: ['', [this.phoneValidationRule.bind(this)]],
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

  onCountrySelected(country: CountryCode): void {
    this.selectedCountry.set(country);
    this.sanitizePhoneInput();
    this.localPhoneControl.updateValueAndValidity();
  }

  onCountryChange(code: string): void {
    const found = this.countryCodes.find(c => c.code === code) || COUNTRY_CODES[0];
    this.onCountrySelected(found);
  }

  getMaxPhoneLength(): number {
    const code = this.registerForm?.get('countryCode')?.value || '+20';
    const val = (this.registerForm?.get('localPhone')?.value || '').trim();
    if (code === '+20') {
      return val.startsWith('0') ? 11 : 10;
    }
    return 12;
  }

  onPhoneInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let val = input.value.replace(/\D/g, ''); // Digits only

    const code = this.registerForm.get('countryCode')?.value || '+20';
    if (code === '+20') {
      const maxLen = val.startsWith('0') ? 11 : 10;
      if (val.length > maxLen) {
        val = val.substring(0, maxLen);
      }
    } else {
      if (val.length > 12) {
        val = val.substring(0, 12);
      }
    }

    input.value = val;
    this.localPhoneControl.setValue(val, { emitEvent: false });
  }

  private sanitizePhoneInput(): void {
    const control = this.registerForm?.get('localPhone');
    if (!control || !control.value) return;
    let val = control.value.replace(/\D/g, '');
    const code = this.registerForm.get('countryCode')?.value || '+20';
    if (code === '+20') {
      const maxLen = val.startsWith('0') ? 11 : 10;
      if (val.length > maxLen) val = val.substring(0, maxLen);
    } else {
      if (val.length > 12) val = val.substring(0, 12);
    }
    control.setValue(val, { emitEvent: false });
  }

  // Custom Phone Validator supporting Egyptian (+20) strict rules
  phoneValidationRule(control: AbstractControl): ValidationErrors | null {
    const val = (control.value || '').toString().trim();
    if (!val) return null; // Optional

    const country = this.registerForm?.get('countryCode')?.value || '+20';

    if (country === '+20') {
      if (val.startsWith('0')) {
        if (!/^01[0125]\d{8}$/.test(val)) {
          return { invalidEgyptPhoneWithZero: true };
        }
      } else {
        if (!/^1[0125]\d{8}$/.test(val)) {
          return { invalidEgyptPhoneNoZero: true };
        }
      }
    } else {
      if (!/^\d{7,12}$/.test(val)) {
        return { invalidPhone: true };
      }
    }
    return null;
  }

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
    
    // Format optional phone number as E.164
    let fullPhone: string | undefined = undefined;
    if (formValue.localPhone && formValue.localPhone.trim()) {
      const cleanCode = formValue.countryCode || '+20';
      // Automatically strip leading zero for Egypt (e.g. 01012345678 -> 1012345678)
      const cleanLocal = formValue.localPhone.trim().replace(/^0+/, ''); 
      fullPhone = `${cleanCode}${cleanLocal}`;
    }

    const request = {
      firstName: formValue.firstName!,
      lastName: formValue.lastName!,
      email: formValue.email!,
      phoneNumber: fullPhone,
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
  get localPhoneControl() { return this.registerForm.get('localPhone')!; }
  get passwordControl() { return this.registerForm.get('password')!; }
  get confirmPasswordControl() { return this.registerForm.get('confirmPassword')!; }
}
