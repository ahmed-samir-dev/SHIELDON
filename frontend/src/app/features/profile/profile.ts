import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ProfileService } from '../../core/services/profile.service';
import { ShepherdService } from '../../core/services/shepherd.service';
import { UserProfileResponse } from '../../core/models/profile.model';
import { ToastrService } from 'ngx-toastr';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.scss'
})
export class ProfileComponent implements OnInit {
  private profileService = inject(ProfileService);
  private shepherdService = inject(ShepherdService);
  private fb = inject(FormBuilder);
  private toastr = inject(ToastrService);

  profileData = signal<UserProfileResponse | null>(null);
  isLoading = signal(true);
  isSaving = signal(false);
  isUploading = signal(false);

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
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.toastr.error('Failed to load profile data.');
      }
    });
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
        this.toastr.success('Profile updated successfully.');
      },
      error: () => {
        this.isSaving.set(false);
        this.toastr.error('Failed to update profile.');
      }
    });
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    // Client side validation (matches backend allowed types)
    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      this.toastr.error('Only JPG, PNG, and WebP images are allowed.');
      return;
    }

    if (file.size > 5 * 1024 * 1024) { // 5MB limit
      this.toastr.error('Image size must be less than 5MB.');
      return;
    }

    this.isUploading.set(true);
    this.profileService.uploadProfilePicture(file).subscribe({
      next: (res) => {
        this.profileData.set(res.data);
        this.isUploading.set(false);
        this.toastr.success('Profile picture updated!');
      },
      error: (err) => {
        this.isUploading.set(false);
        this.toastr.error(err.error?.message || 'Failed to upload picture.');
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
    const formValue = this.changePasswordForm.value;
    const request = {
      currentPassword: formValue.currentPassword!,
      newPassword: formValue.newPassword!,
      confirmNewPassword: formValue.confirmNewPassword!
    };

    this.profileService.changePassword(request).subscribe({
      next: () => {
        this.isChangingPassword.set(false);
        this.changePasswordForm.reset();
        this.toastr.success('Password changed successfully.');
      },
      error: (err) => {
        this.isChangingPassword.set(false);
        const msg = err.error?.message || 'Failed to change password.';
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
}
