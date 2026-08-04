import { Component, Input, Output, EventEmitter, inject, signal, OnDestroy, ElementRef, ViewChildren, QueryList, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProfileService } from '../../../core/services/profile.service';
import { UserProfileResponse } from '../../../core/models/profile.model';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-otp-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './otp-modal.html',
  styleUrl: './otp-modal.scss'
})
export class OtpModalComponent implements OnDestroy, AfterViewInit {
  private profileService = inject(ProfileService);
  private toastr = inject(ToastrService);

  @Input() isOpen = false;
  @Input() phoneNumber = '';
  @Output() closeModal = new EventEmitter<void>();
  @Output() verifiedSuccess = new EventEmitter<UserProfileResponse>();

  @ViewChildren('digitInput') digitInputs!: QueryList<ElementRef<HTMLInputElement>>;

  channel = signal<'whatsapp'>('whatsapp');
  otpDigits = signal<string[]>(['', '', '', '', '', '']);
  isSending = signal(false);
  isVerifying = signal(false);
  isSuccess = signal(false);
  errorMessage = signal<string | null>(null);

  countdown = signal(0);
  private timer: any = null;

  ngAfterViewInit(): void {
    if (this.isOpen) {
      this.focusFirstInput();
    }
  }

  ngOnDestroy(): void {
    this.clearTimer();
  }

  sendOtp(): void {
    if (this.isSending() || this.countdown() > 0) return;

    this.isSending.set(true);
    this.errorMessage.set(null);

    this.profileService.sendPhoneOtp('whatsapp').subscribe({
      next: (res) => {
        this.isSending.set(false);
        this.toastr.success(res.message || 'OTP code sent via WhatsApp!', 'Code Sent');
        this.startCountdown(120); // 2 minutes
        this.focusFirstInput();
      },
      error: (err) => {
        this.isSending.set(false);
        const msg = err.error?.message || 'Failed to send WhatsApp OTP. Please try again.';
        this.errorMessage.set(msg);
      }
    });
  }

  trackByIndex(index: number): number {
    return index;
  }

  onDigitInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    let value = input.value;

    // Keep only the newest typed character if multiple characters are present
    if (value.length > 1) {
      value = value.charAt(value.length - 1);
    }

    const current = [...this.otpDigits()];
    current[index] = value;
    this.otpDigits.set(current);
    input.value = value;

    if (value !== '' && index < 5) {
      setTimeout(() => {
        const nextInput = this.digitInputs?.toArray()[index + 1];
        if (nextInput) {
          nextInput.nativeElement.focus();
          nextInput.nativeElement.select();
        }
      }, 0);
    }

    // Auto verify when all 6 cells are filled
    if (current.every(d => d !== '')) {
      this.verifyOtp();
    }
  }

  onKeyDown(event: KeyboardEvent, index: number): void {
    if (event.key === 'Backspace') {
      const current = [...this.otpDigits()];
      if (current[index] !== '') {
        current[index] = '';
        this.otpDigits.set(current);
      } else if (index > 0) {
        current[index - 1] = '';
        this.otpDigits.set(current);
        setTimeout(() => {
          const prevInput = this.digitInputs?.toArray()[index - 1];
          if (prevInput) {
            prevInput.nativeElement.focus();
            prevInput.nativeElement.select();
          }
        }, 0);
      }
    } else if (event.key === 'ArrowLeft' && index > 0) {
      const prevInput = this.digitInputs?.toArray()[index - 1];
      if (prevInput) prevInput.nativeElement.focus();
    } else if (event.key === 'ArrowRight' && index < 5) {
      const nextInput = this.digitInputs?.toArray()[index + 1];
      if (nextInput) nextInput.nativeElement.focus();
    }
  }

  onPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const pastedText = event.clipboardData?.getData('text') || '';
    if (pastedText) {
      this.handlePaste(pastedText);
    }
  }

  onFocus(event: FocusEvent): void {
    const input = event.target as HTMLInputElement;
    input.select();
  }

  handlePaste(pastedText: string): void {
    const digitsOnly = pastedText.replace(/\D/g, '').slice(0, 6);
    const newDigits = ['', '', '', '', '', ''];
    for (let i = 0; i < digitsOnly.length; i++) {
      newDigits[i] = digitsOnly[i];
    }
    this.otpDigits.set(newDigits);

    if (digitsOnly.length === 6) {
      this.verifyOtp();
    } else if (digitsOnly.length > 0) {
      const targetIndex = Math.min(digitsOnly.length, 5);
      const targetInput = this.digitInputs.toArray()[targetIndex];
      if (targetInput) targetInput.nativeElement.focus();
    }
  }

  verifyOtp(): void {
    const code = this.otpDigits().join('');
    if (code.length !== 6 || this.isVerifying()) return;

    this.isVerifying.set(true);
    this.errorMessage.set(null);

    this.profileService.verifyPhoneOtp(code).subscribe({
      next: (res) => {
        this.isVerifying.set(false);
        this.isSuccess.set(true);
        this.toastr.success('Phone number verified successfully!', 'Verified');
        
        setTimeout(() => {
          this.verifiedSuccess.emit(res.data);
          this.onClose();
        }, 1200);
      },
      error: (err) => {
        this.isVerifying.set(false);
        const msg = err.error?.message || 'Invalid verification code. Please check and try again.';
        this.errorMessage.set(msg);
      }
    });
  }

  onClose(): void {
    this.clearTimer();
    this.otpDigits.set(['', '', '', '', '', '']);
    this.errorMessage.set(null);
    this.isSuccess.set(false);
    this.closeModal.emit();
  }

  private startCountdown(seconds: number): void {
    this.clearTimer();
    this.countdown.set(seconds);
    this.timer = setInterval(() => {
      if (this.countdown() > 0) {
        this.countdown.update(c => c - 1);
      } else {
        this.clearTimer();
      }
    }, 1000);
  }

  private clearTimer(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  private focusFirstInput(): void {
    setTimeout(() => {
      const first = this.digitInputs?.first;
      if (first) first.nativeElement.focus();
    }, 100);
  }

  get formatCountdown(): string {
    const m = Math.floor(this.countdown() / 60);
    const s = this.countdown() % 60;
    return `${m}:${s < 10 ? '0' : ''}${s}`;
  }
}
