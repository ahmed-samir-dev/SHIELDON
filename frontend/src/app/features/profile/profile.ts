import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ProfileService } from '../../core/services/profile.service';
import { ShepherdService } from '../../core/services/shepherd.service';
import { UserProfileResponse } from '../../core/models/profile.model';
import { ToastrService } from 'ngx-toastr';
import { environment } from '../../../environments/environment';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { OtpModalService } from '../../core/services/otp-modal.service';

import { COUNTRY_CODES, CountryCode } from '../../core/constants/country-codes.constant';
import { CountryPickerComponent } from '../../shared/components/country-picker/country-picker';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, CountryPickerComponent],
  templateUrl: './profile.html',
  styleUrl: './profile.scss'
})
export class ProfileComponent implements OnInit {
  private translate = inject(TranslateService);
  private profileService = inject(ProfileService);
  private shepherdService = inject(ShepherdService);
  private otpModalService = inject(OtpModalService);
  private fb = inject(FormBuilder);
  private toastr = inject(ToastrService);

  profileData = signal<UserProfileResponse | null>(null);
  isLoading = signal(true);
  isSaving = signal(false);
  isUploading = signal(false);

  // Tab Navigation for Compact Non-Scrolling Layout
  activeTab = signal<'profile' | 'phone' | 'security' | 'tour'>('profile');

  setActiveTab(tab: 'profile' | 'phone' | 'security' | 'tour'): void {
    this.activeTab.set(tab);
  }


  // Country Codes & Phone Verification
  readonly countryCodes: CountryCode[] = COUNTRY_CODES;
  selectedCountry = signal<CountryCode>(COUNTRY_CODES[0]); // Default Egypt

  isSavingPhone = signal(false);

  phoneForm = this.fb.group({
    countryCode: ['+20'],
    localPhone: ['', [Validators.required, this.phoneValidationRule.bind(this)]]
  });

  onCountrySelected(country: CountryCode): void {
    this.selectedCountry.set(country);
    this.sanitizePhoneInput();
    this.phoneForm.get('localPhone')?.updateValueAndValidity();
  }

  onCountryChange(code: string): void {
    const found = this.countryCodes.find(c => c.code === code) || COUNTRY_CODES[0];
    this.onCountrySelected(found);
  }

  getMaxPhoneLength(): number {
    const code = this.phoneForm?.get('countryCode')?.value || '+20';
    if (code === '+20') {
      const val = (this.phoneForm?.get('localPhone')?.value || '').trim();
      return val.startsWith('0') ? 11 : 10;
    }
    return 12;
  }

  onPhoneInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let val = input.value.replace(/\D/g, ''); // Keep numbers only

    const code = this.phoneForm.get('countryCode')?.value || '+20';
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
    this.phoneForm.get('localPhone')?.setValue(val, { emitEvent: false });
    this.phoneForm.get('localPhone')?.markAsDirty();
    this.phoneForm.get('localPhone')?.updateValueAndValidity();
  }

  private sanitizePhoneInput(): void {
    const control = this.phoneForm?.get('localPhone');
    if (!control || !control.value) return;
    const code = this.phoneForm.get('countryCode')?.value || '+20';
    let val = control.value.toString().replace(/\D/g, '');
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
    if (!val) return null;

    const country = this.phoneForm?.get('countryCode')?.value || '+20';

    if (country === '+20') {
      // Egypt: Allow 010, 011, 012, 015 (11 digits) OR 10, 11, 12, 15 (10 digits)
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

  // Use environment url to prepend to file path
  apiUrl = environment.apiUrl.replace('/api', '');

  profileForm = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    email: [{ value: '', disabled: true }], // Email cannot be changed here
    displayId: [{ value: '', disabled: true }],
    accountStatus: [{ value: '', disabled: true }]
  });

  changePasswordForm = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: [
      '', 
      [
        Validators.required, 
        Validators.minLength(8),
        Validators.pattern(/(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9])/)
      ]
    ],
    confirmNewPassword: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  showCurrentPassword = signal(false);
  showNewPassword = signal(false);
  showConfirmPassword = signal(false);
  isChangingPassword = signal(false);

  get firstNameControl() { return this.profileForm.get('firstName')!; }
  get lastNameControl() { return this.profileForm.get('lastName')!; }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading.set(true);
    this.profileService.getProfile().subscribe({
      next: (res) => {
        this.profileData.set(res.data);
        this.profileForm.patchValue({
          firstName: res.data.firstName,
          lastName: res.data.lastName,
          email: res.data.email,
          displayId: res.data.displayId,
          accountStatus: res.data.accountStatus
        });

        // Parse existing phone number if present
        if (res.data.phoneNumber) {
          const full = res.data.phoneNumber.trim();
          // Match against known country codes list (longest code first to avoid partial prefix match)
          const foundCountry = this.countryCodes
            .slice()
            .sort((a, b) => b.code.length - a.code.length)
            .find(c => full.startsWith(c.code));

          if (foundCountry) {
            const code = foundCountry.code;
            let local = full.substring(code.length);
            if (code === '+20') {
              local = local.replace(/^0+/, '');
            }
            this.phoneForm.patchValue({ countryCode: code, localPhone: local });
            this.onCountrySelected(foundCountry);
          } else {
            let local = full.replace(/^\+\d{1,3}/, '').replace(/^0+/, '');
            this.phoneForm.patchValue({ localPhone: local });
          }
        }

        // Passwordless user handling
        if (res.data.hasPassword === false) {
          this.changePasswordForm.get('currentPassword')?.disable();
          this.changePasswordForm.get('currentPassword')?.clearValidators();
          this.changePasswordForm.get('currentPassword')?.updateValueAndValidity();
        } else {
          this.changePasswordForm.get('currentPassword')?.enable();
          this.changePasswordForm.get('currentPassword')?.setValidators([Validators.required]);
          this.changePasswordForm.get('currentPassword')?.updateValueAndValidity();
        }

        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.toastr.error(this.translate.instant('PROFILE.TOAST_LOAD_ERR'));
      }
    });
  }

  onSavePhone(): void {
    this.phoneForm.markAllAsTouched();
    if (this.phoneForm.invalid || this.isSavingPhone()) return;

    this.isSavingPhone.set(true);
    const countryCode = this.phoneForm.value.countryCode || '+20';
    let localPhone = (this.phoneForm.value.localPhone || '').trim();
    if (countryCode === '+20') {
      localPhone = localPhone.replace(/^0+/, '');
    }
    const fullPhone = `${countryCode}${localPhone}`;

    this.profileService.updatePhone(fullPhone).subscribe({
      next: (res) => {
        this.profileData.set(res.data);
        this.isSavingPhone.set(false);
        this.toastr.success('Phone number saved. Click "Verify Now" to complete verification.', 'Saved');
      },
      error: (err) => {
        this.isSavingPhone.set(false);
        const msg = err.error?.message
          || err.error?.errors?.PhoneNumber?.[0]
          || err.error?.title
          || 'Failed to save phone number. Please try again.';
        this.toastr.error(msg);
      }
    });
  }

  openOtpModal(phoneOverride?: string): void {
    const phone = phoneOverride || this.profileData()?.phoneNumber;
    if (!phone) {
      this.toastr.warning('Please enter and save a phone number first.', 'No Phone Number');
      return;
    }
    this.otpModalService.open(
      phone,
      (updatedProfile: UserProfileResponse) => this.profileData.set(updatedProfile)
    );
  }

  onSubmit(): void {
    if (this.profileForm.invalid) return;

    this.isSaving.set(true);
    const request = {
      firstName: this.profileForm.get('firstName')!.value!,
      lastName: this.profileForm.get('lastName')!.value!
    };

    this.profileService.updateProfile(request).subscribe({
      next: (res) => {
        this.profileData.set(res.data);
        this.isSaving.set(false);
        this.toastr.success(this.translate.instant('PROFILE.TOAST_UPDATE_SUCCESS'));
      },
      error: () => {
        this.isSaving.set(false);
        this.toastr.error(this.translate.instant('PROFILE.TOAST_UPDATE_ERR'));
      }
    });
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    // Client side validation (matches backend allowed types)
    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      this.toastr.error(this.translate.instant('PROFILE.TOAST_PIC_TYPE_ERR'));
      return;
    }

    if (file.size > 5 * 1024 * 1024) { // 5MB limit
      this.toastr.error(this.translate.instant('PROFILE.TOAST_PIC_SIZE_ERR'));
      return;
    }

    this.isUploading.set(true);
    this.profileService.uploadProfilePicture(file).subscribe({
      next: (res) => {
        this.profileData.set(res.data);
        this.isUploading.set(false);
        this.toastr.success(this.translate.instant('PROFILE.TOAST_PIC_SUCCESS'));
      },
      error: (err) => {
        this.isUploading.set(false);
        this.toastr.error(err.error?.message || this.translate.instant('PROFILE.TOAST_PIC_ERR'));
      }
    });
  }

  getAvatarUrl(): string {
    const path = this.profileData()?.profilePictureUrl;
    if (path) {
      return `${this.apiUrl}/${path}`;
    }
    return ''; // Can fallback to initials or a default svg in HTML
  }

  toggleCurrentPassword(): void { this.showCurrentPassword.update(v => !v); }
  toggleNewPassword(): void { this.showNewPassword.update(v => !v); }
  toggleConfirmPassword(): void { this.showConfirmPassword.update(v => !v); }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword')?.value;
    const confirm = control.get('confirmNewPassword')?.value;
    if (newPassword !== confirm && confirm) {
      control.get('confirmNewPassword')?.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    } else {
      if (control.get('confirmNewPassword')?.hasError('passwordMismatch')) {
        control.get('confirmNewPassword')?.setErrors(null);
      }
      return null;
    }
  }

  onChangePasswordSubmit(): void {
    if (this.changePasswordForm.invalid || this.isChangingPassword()) {
      this.changePasswordForm.markAllAsTouched();
      return;
    }

    this.isChangingPassword.set(true);
    const formValue = this.changePasswordForm.getRawValue();
    const request = {
      currentPassword: formValue.currentPassword || '',
      newPassword: formValue.newPassword!,
      confirmNewPassword: formValue.confirmNewPassword!
    };

    this.profileService.changePassword(request).subscribe({
      next: () => {
        this.isChangingPassword.set(false);
        this.changePasswordForm.reset();
        this.toastr.success(this.translate.instant('PROFILE.TOAST_PASS_SUCCESS'));
        this.loadProfile();
      },
      error: (err) => {
        this.isChangingPassword.set(false);
        const msg = err.error?.message || this.translate.instant('PROFILE.TOAST_PASS_ERR');
        this.toastr.error(msg);
      }
    });
  }

  replayTour(): void {
    this.shepherdService.resetTour();
  }

  get currentPasswordControl() { return this.changePasswordForm.get('currentPassword')!; }
  get newPasswordControl() { return this.changePasswordForm.get('newPassword')!; }
  get confirmNewPasswordControl() { return this.changePasswordForm.get('confirmNewPassword')!; }
  get profileLocalPhoneControl() { return this.phoneForm.get('localPhone')!; }
}
