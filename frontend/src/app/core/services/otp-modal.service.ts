import { Injectable, signal } from '@angular/core';
import { UserProfileResponse } from '../models/profile.model';

export interface OtpModalState {
  isOpen: boolean;
  phoneNumber: string;
  onVerified?: (profile: UserProfileResponse) => void;
  onClose?: () => void;
}

@Injectable({ providedIn: 'root' })
export class OtpModalService {
  private _state = signal<OtpModalState>({ isOpen: false, phoneNumber: '' });

  readonly state = this._state.asReadonly();

  open(phoneNumber: string, onVerified?: (profile: UserProfileResponse) => void, onClose?: () => void): void {
    this._state.set({ isOpen: true, phoneNumber, onVerified, onClose });
  }

  close(): void {
    const current = this._state();
    if (current.onClose) current.onClose();
    this._state.set({ isOpen: false, phoneNumber: '' });
  }

  handleVerified(profile: UserProfileResponse): void {
    const current = this._state();
    if (current.onVerified) current.onVerified(profile);
    this._state.set({ isOpen: false, phoneNumber: '' });
  }
}
